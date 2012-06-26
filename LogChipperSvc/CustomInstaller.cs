using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Xml;
using Microsoft.Win32;

namespace LogChipperSvc
{
    [RunInstaller(true)]
    public partial class CustomInstaller : System.Configuration.Install.Installer
    {
        private string serviceName;

        public CustomInstaller()
            : base()
        {
            serviceName = string.IsNullOrEmpty(Context.Parameters["servicename"]) ? "LogChipper" : Context.Parameters["servicename"].ToString();
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            // from http://stackoverflow.com/questions/918565/updating-appname-config-file-from-an-custom-installer-class-action
            base.Install(stateSaver);

            try
            {
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
                    EventLog.CreateEventSource(eventLogSource, eventLogName);
            }
            catch
            {
            }

            // attempt to open permissions for the NetworkService to the Security EventLog key
            //   this is prevent errors in the main app when it searches for the custom event source created above
            string user = "NT SECURITY\\NETWORK SERVICE";
            RegistrySecurity rs = new RegistrySecurity();
            RegistryKey rk = Registry.LocalMachine;
            string subkey = "SYSTEM\\CurrentControlSet\\Services\\EventLog\\Security";
            try
            {
                rs.AddAccessRule(new RegistryAccessRule(user,
                    RegistryRights.ReadKey,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
                rk = rk.OpenSubKey(subkey, false);
                rk.SetAccessControl(rs);
            }
            catch
            {
            }
            finally
            {
                if (rk != null) rk.Close();
            }
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            if (this.IsUpgrade)
                this.StopService();
            this.StartService();
        }

        public override void Uninstall(IDictionary savedState)
        {
            this.StopService();
            base.Uninstall(savedState);
        }

        private bool IsUpgrade
        {
            get
            {
                return !string.IsNullOrEmpty(this.Context.Parameters["OldProductCode"]);
            }
        }

        private void StopService()
        {
            var controller = new ServiceController(serviceName);
            try
            {
                if ((controller.Status != ServiceControllerStatus.Stopped) && (controller.Status != ServiceControllerStatus.StopPending))
                {
                    controller.Stop();
                }
            }
            catch (System.InvalidOperationException)
            {
            }
        }

        private void StartService()
        {
            var controller = new ServiceController(serviceName);
            try
            {
                if ((controller.Status != ServiceControllerStatus.Running) && (controller.Status != ServiceControllerStatus.StartPending))
                {
                    controller.Start();
                }
            }
            catch (System.InvalidOperationException)
            {
            }
        }
    }
}