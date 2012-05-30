using System.ComponentModel;

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

            string targetDirectory = Context.Parameters["targetdir"];
            string param1 = Context.Parameters["param1"];
            string param2 = Context.Parameters["param2"];
            string param3 = Context.Parameters["param3"];

            string path = System.IO.Path.Combine(targetDirectory, "LogChipperSvc.exe.config");
            System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();
            xDoc.Load(path);

            System.Xml.XmlNode node1 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='logFilePath']/value");
            node1.InnerText = param1;

            System.Xml.XmlNode node2 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogServer']/value");
            node2.InnerText = param2;

            System.Xml.XmlNode node3 = xDoc.SelectSingleNode("/configuration/applicationSettings/LogChipperSvc.Properties.Settings/setting[@name='syslogPort']/value");
            node3.InnerText = param3;

            xDoc.Save(path);
        }

    }
}
