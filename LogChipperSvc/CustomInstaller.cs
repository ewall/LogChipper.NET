using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

namespace LogChipperSvc
{
    [RunInstaller(true)]
    public partial class CustomInstaller : System.Configuration.Install.Installer
    {
        public CustomInstaller()
        {
            InitializeComponent();
        }

        // from http://raquila.com/software/configure-app-config-application-settings-during-msi-install/
        //public override void Install(System.Collections.IDictionary stateSaver)
        //{
        //    base.Install(stateSaver);
        //    string targetDirectory = Context.Parameters["targetdir"];
        //    string param1 = Context.Parameters["param1"];
        //    string param2 = Context.Parameters["param2"];
        //    string param3 = Context.Parameters["param3"];  
        //    //System.Diagnostics.Debugger.Break();
        //    string exePath = string.Format("{0}LogChipperSvc.exe", targetDirectory);
        //    Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
        //    config.AppSettings.Settings["logFilePath"].Value = param1;
        //    config.AppSettings.Settings["syslogServer"].Value = param2;
        //    config.AppSettings.Settings["syslogPort"].Value = param3;
        //    config.Save();
        //}

        // from http://stackoverflow.com/questions/918565/updating-appname-config-file-from-an-custom-installer-class-action
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // save install-time user input into config file
            string targetDirectory = Context.Parameters["targetdir"];
            string param1 = Context.Parameters["param1"];
            string param2 = Context.Parameters["param2"];
            string param3 = Context.Parameters["param3"];

            string path = System.IO.Path.Combine(targetDirectory, "LogChipperSvc.exe.config");
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(path);

            XmlNode node1 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='logFilePath']/value");
            node1.InnerText = param1;

            XmlNode node2 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogServer']/value");
            node2.InnerText = param2;

            XmlNode node3 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogPort']/value");
            node3.InnerText = param3;

            xDoc.Save(path);

            // create the Event Log source if needed
            //   this is best done during install because (a) we should already have admin rights, and (b) there's a lag before you can use it
            string machineName = ".";

            XmlNode node4 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='eventLogName']/value");
            string eventLogName = node4.InnerText;

            XmlNode node5 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='eventLogSource']/value");
            string eventLogSource = node5.InnerText;

            if (!EventLog.SourceExists(eventLogSource, machineName))
                EventLog.CreateEventSource(eventLogSource, eventLogName, machineName);       
        }

    }
}
