using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace LogChipperSvc
{
    public partial class LogChipperService : ServiceBase
    {
        private EventLog eventLogger;
        private Syslog.Client syslogForwarder;
        private StreamReader reader;

        public LogChipperService()
        {
            InitializeComponent();
        }

        [STAThread]
        protected override void OnStart(string[] args)
        {
            // prep for writing to local event log
            string eventLogName = Properties.Settings.Default.eventLogName;
            string eventLogSource = Properties.Settings.Default.eventLogSource;
            string machineName = ".";
            /* Warning: this will definately fail to create the source if the account lacks administrator/UAC rights.
             * Furthermore, due to the lag in creating the source, the first attempt to write to it might fail.
             * Thus, we should have already done this during the program's installation, this is just in case it was missed. */
            if (!EventLog.SourceExists(eventLogSource, machineName))
                EventLog.CreateEventSource(eventLogSource, eventLogName);
            eventLogger = new EventLog(eventLogName, machineName, eventLogSource);
            eventLogger.WriteEntry("LogChipper syslog forwarding service has started", EventLogEntryType.Information, 0);

            // prep for posting to remote syslog
            syslogForwarder = new Syslog.Client(
                (string)Properties.Settings.Default.syslogServer,
                (int)Properties.Settings.Default.syslogPort,
                (int)Syslog.Facility.Syslog,
                (int)Syslog.Level.Information);
            syslogForwarder.Send("[LogChipper syslog forwarding service has started]");

            // prep to tail the local log file
            string fileName = Properties.Settings.Default.logFilePath;

            if (!File.Exists(fileName))
            {
                bool found = false;

                // re-test every minute for 15 minutes
                for (int i = 0; i < 15; i++)
                {
                    eventLogger.WriteEntry("Target log file not found; please check the configuration.", EventLogEntryType.Warning, 2);
                    System.Threading.Thread.Sleep(60 * 1000);
                    if (File.Exists(fileName))
                    {
                        found = true;
                        break;
                    }
                }
                // TODO: currently we exit if the file is not found within 15 minutes; or should we let it loop perpetually instead?
                if (!found)
                {
                    eventLogger.WriteEntry("Target log file still doesn't exist; exiting service.", EventLogEntryType.Error, 404);
                    Environment.Exit(1); //end it all
                }
            }

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
                    }
                }
            }
            catch (IOException e)
            {
                eventLogger.WriteEntry("IO error: " + e.ToString(), EventLogEntryType.Error, 21);
                // TODO: currently we exit on all IOExceptions; should we try reloading the file first?
                Environment.Exit(1); //end it all
            }
            catch (Exception e)
            {
                eventLogger.WriteEntry("Unexpected error: " + e.ToString(), EventLogEntryType.Error, 1);
                Environment.Exit(1); //end it all
            }
            finally
            {
                if (syslogForwarder != null)
                    syslogForwarder.Close();
                if (reader != null)
                    reader.Close();
            }
        }

        protected override void OnStop()
        {
            if (syslogForwarder != null)
                syslogForwarder.Close(); 
            if (reader != null)
                reader.Close();
        }
    }
}
