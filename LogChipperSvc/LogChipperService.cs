using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace LogChipperSvc
{
    public partial class LogChipperService : ServiceBase
    {
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

            /* WARNING: this will definately fail to create the source if the account lacks administrator rights.
             * Furthermore, it will probably fail on the first execution due to the lag in creating it. */

            // TODO: solve the above problem by creating this EventLog source during the installation :D

            if (!EventLog.SourceExists(eventLogSource, machineName))
                EventLog.CreateEventSource(eventLogSource, eventLogName, machineName);
            EventLog eventLogger = new EventLog(eventLogName, machineName, eventLogSource);

            eventLogger.WriteEntry("LogChipper.NET service starting up", EventLogEntryType.Information, 0);

            // TODO: prep for posting to syslog
            //Syslog.Client c = new Syslog.Client();
            //c.HostIp = "127.0.0.1";
            //int facility = (int)Syslog.Facility.Syslog;
            //int level = (int)Syslog.Level.Warning;
            //string text = "Hello from LogChipperSvc";
            //c.Send(new Syslog.Message(facility, level, text));
            //c.Close();

            // fetch properties from exe.config, for convenience
            string fileName = Properties.Settings.Default.logFilePath;
            int pauseInMS = Properties.Settings.Default.pauseInMilliseconds;

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
                        System.Threading.Thread.Sleep(pauseInMS);

                        // if the file size has not changed, keep idling
                        if (reader.BaseStream.Length == lastMaxOffset)
                            continue;

                        // seek to the last max offset
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                        // read out of the file until the EOF
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                            Console.WriteLine(line); // TODO: post to syslog

                        // update the last max offset
                        lastMaxOffset = reader.BaseStream.Position;
                    }
                }
            }
            catch (IOException e)
            {
                eventLogger.WriteEntry(e.ToString(), EventLogEntryType.Warning, 2);
                // TODO: pause, then try loading the file again; write to event log
                // Q: should we stop after a certain number of failures?
            }
            catch (Exception e)
            {
                eventLogger.WriteEntry(e.ToString(), EventLogEntryType.Error, 1);
                // Q: do we want to quit or continue here?
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        protected override void OnStop()
        {
            if (reader != null)
                reader.Close();
        }
    }
}
