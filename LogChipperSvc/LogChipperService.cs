using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace LogChipperSvc
{
    public partial class LogChipperService : ServiceBase
    {
        private EventLog eventLogger;
        private Syslog.Client syslogForwarder;
        private string fileName;
        private Thread workerThread = null;
        private static ManualResetEvent pause = new ManualResetEvent(true);
        private StreamReader reader;

        public LogChipperService()
        {
            ServiceName = "LogChipper";
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;

            InitializeComponent();
        }

        //private void InitializeComponent()
        //{
        //    this.ServiceName = "LogChipper"; 
        //    this.CanHandleSessionChangeEvent = false;
        //    this.CanPauseAndContinue = false;
        //    this.CanShutdown = true;
        //    this.CanStop = true;
        //}

        protected override void OnStart(string[] args)
        {
            // prep for writing to local event log
            string eventLogName = Properties.Settings.Default.eventLogName;
            string eventLogSource = Properties.Settings.Default.eventLogSource;
            string machineName = ".";
            // warning: Event Log Source must have already been created during the installation
            eventLogger = new EventLog(eventLogName, machineName, eventLogSource);
            eventLogger.WriteEntry("LogChipper syslog forwarding service has started.", EventLogEntryType.Information, 0);

            // prep for posting to remote syslog
            syslogForwarder = new Syslog.Client(
                (string)Properties.Settings.Default.syslogServer,
                (int)Properties.Settings.Default.syslogPort,
                (int)Syslog.Facility.Syslog,
                (int)Syslog.Level.Information);
            syslogForwarder.Send("[LogChipper syslog forwarding service has started.]");

            // prep to tail the local log file
            fileName = Properties.Settings.Default.logFilePath;
            if (!File.Exists(fileName))
            {
                eventLogger.WriteEntry("Target log file still doesn't exist; exiting service.", EventLogEntryType.Error, 404);
                this.OnStop(); // TODO: correct way to exit?

                //    // TODO: handle missing target file more gracefully
                //    bool found = false;
                //
                //    // re-test every minute for 15 minutes
                //    for (int i = 0; i < 15; i++)
                //    {
                //        eventLogger.WriteEntry("Target log file not found; please check the configuration.", EventLogEntryType.Warning, 2);
                //        System.Threading.Thread.Sleep(60 * 1000);
                //        if (File.Exists(fileName))
                //        {
                //            found = true;
                //            break;
                //        }
                //    }
                //    if (!found)
                //    {
                //        eventLogger.WriteEntry("Target log file still doesn't exist; exiting service.", EventLogEntryType.Error, 404);
                //        Environment.Exit(1); // TODO: correct way to exit?
                //    }
            }

            // spin your thread
            if ((workerThread == null) || ((workerThread.ThreadState & 
                (System.Threading.ThreadState.Unstarted | System.Threading.ThreadState.Stopped)) != 0))
            {
                workerThread = new Thread(new ThreadStart(ServiceWorkerMethod));
                workerThread.Start();
            }
        }

        protected override void OnStop()
        {
            this.RequestAdditionalTime(7000);

            if ((workerThread != null) && (workerThread.IsAlive))
            {
                pause.Reset();
                Thread.Sleep(5000);
                workerThread.Abort();
            }

            eventLogger.WriteEntry("LogChipper syslog forwarding service has been stopped.", EventLogEntryType.Information, 0);
            if (syslogForwarder != null)
            {
                syslogForwarder.Send("[LogChipper syslog forwarding service has been stopped.]");
                syslogForwarder.Close();
            }
            if (reader != null)
                reader.Close();

            this.ExitCode = 0;
        }

        public void ServiceWorkerMethod()
        {
            eventLogger.WriteEntry("LogChipper worker thread starting", EventLogEntryType.Information, 0);

            try
            {
                /* If you try to use the StreamReader's constructor to open a file in use by another process, you will get an exception.
                 * Here we create the FileStream to enable reading even if the file is open for writing, which is the expected use. */
                reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                {
                    //start at the end of the file
                    long lastMaxOffset = reader.BaseStream.Length;

                    while (true)
                    {
                        System.Threading.Thread.Sleep(Properties.Settings.Default.pauseInMilliseconds);

                        // if the file size has not changed, keep idling
                        if (reader.BaseStream.Length == lastMaxOffset)
                            continue;

                        // seek to the last max offset
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                        // read out of the file until the EOF
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                            syslogForwarder.Send(line);

                        // update the last max offset
                        lastMaxOffset = reader.BaseStream.Position;
                        // TODO: handle if the file contents have been cleared

                        // block if the service is paused or is shutting down
                        pause.WaitOne();
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // commonly, the parent service thread has been told to stop
                eventLogger.WriteEntry("LogChipper worker thread exiting", EventLogEntryType.Information, 0);
            }
            catch (IOException e)
            {
                eventLogger.WriteEntry("IO error: " + e.ToString(), EventLogEntryType.Error, 21);
                // TODO: currently we exit on all IOExceptions; should we try reloading the file first?
                this.OnStop(); // TODO: correct way to exit?
            }
            catch (Exception e)
            {
                eventLogger.WriteEntry("Unexpected error: " + e.ToString(), EventLogEntryType.Error, 1);
                this.OnStop(); // TODO: correct way to exit?
            }
            finally
            {
                if (syslogForwarder != null)
                    syslogForwarder.Close();
                if (reader != null)
                    reader.Close();
            }
        }
    }
}
