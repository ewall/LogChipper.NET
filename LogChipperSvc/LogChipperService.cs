using System;
using System.Collections;
using System.IO;
using System.ServiceProcess;

namespace LogChipperSvc
{
    public partial class LogChipperService : ServiceBase
    {
        public LogChipperService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            // TODO: create event for posting to syslog
            // TODO: create event for writing to local event log

            //Syslog.Client c = new Syslog.Client();
            //c.HostIp = "127.0.0.1";
            //int facility = (int)Syslog.Facility.Syslog;
            //int level = (int)Syslog.Level.Warning;
            //string text = "Hello from LogChipperSvc";
            //c.Send(new Syslog.Message(facility, level, text));
            //c.Close();

            // fetch properties from exe.config, for convenience
            string fileName = Properties.Settings.Default.logFilePath;
            int pauseInMS = Properties.Settings.Default.pauseInMilleseconds;

            try
            {
                /* If you try to use the StreamReader's constructor to open a file in use by another process, you will get an exception.
                 * Here we create the FileStream to enable reading even if the file is open for writing, which is the expected use. */
                StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

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
                // TODO: pause, then try loading the file again; write to event log
                // Q: should we stop after a certain number of failures?
            }
            catch (Exception e)
            {
                // TODO: write to Event Log
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
            //if (reader != null)
            //    reader.Close();
        }
    }
}
