using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;
using System.Xml;

namespace LogChipperSvc
{
    [RunInstaller(true)]
    public class InstallActions : Installer
    {
        private ServiceController controller;
        private string serviceName = "LogChipper";

        public InstallActions()
            : base()
        {
            this.AfterInstall += new InstallEventHandler(InstallActions_AfterInstall);
            this.BeforeInstall += new InstallEventHandler(InstallActions_BeforeInstall);
            this.BeforeUninstall += new InstallEventHandler(InstallActions_BeforeUninstall);
        }

        void InstallActions_BeforeInstall(object sender, InstallEventArgs e)
        {
            // before installation, create EventLog source if needed
            if (!EventLog.SourceExists("LogChipper"))
            {
                EventLogInstaller eventLogInstaller = new EventLogInstaller();
                eventLogInstaller.Log = "Application"; // TODO: fetch from Properties.Settings.Default.eventLogName;
                eventLogInstaller.Source = "LogChipper"; // TODO: fetch from Properties.Settings.Default.eventLogSource;
                Installers.Add(eventLogInstaller);
            }
        }

        void InstallActions_AfterInstall(object sender, InstallEventArgs e)
        {
            // save install-time user input into config file
            string targetDirectory = Context.Parameters["targetdir"];
            string param1 = Context.Parameters["watchfile"];
            string param2 = Context.Parameters["server"];
            string param3 = Context.Parameters["port"];
            string param4 = Context.Parameters["protocol"];

            string path = System.IO.Path.Combine(targetDirectory, "LogChipperSvc.exe.config");
            XmlDocument xDoc = new XmlDocument();
            
            try
            {
                xDoc.Load(path);

                XmlNode node1 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='logFilePath']/value");
                node1.InnerText = param1;

                XmlNode node2 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogServer']/value");
                node2.InnerText = param2;

                XmlNode node3 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogPort']/value");
                node3.InnerText = param3;

                XmlNode node4 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogProtocol']/value");
                node4.InnerText = param4;

                xDoc.Save(path);
            }
            catch { }

            // automatically start the service after installation
            //SetServiceStatus(true); //BREAKS INSTALL?
        }

        void InstallActions_BeforeUninstall(object sender, InstallEventArgs e)
        {
            // before uninstalling, stop existing service
            SetServiceStatus(false);
        }

        void SetServiceStatus(bool startService)
        {
            ServiceControllerStatus setStatus =
              startService ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;

            try
            {
                controller = new ServiceController(serviceName);
                if (controller != null && controller.Status != setStatus)
                {
                    if (startService)
                        controller.Start();
                    else
                        controller.Stop();
                    controller.WaitForStatus(setStatus, new TimeSpan(0, 0, 30));
                }
            }
            catch { }
        }
    }
}
