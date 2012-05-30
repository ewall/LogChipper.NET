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
            Syslog.Client c = new Syslog.Client();
            c.HostIp = "127.0.0.1";
            int facility = (int)Syslog.Facility.Syslog;
            int level = (int)Syslog.Level.Warning;
            string text = "Hello from LogChipperSvc";
            c.Send(new Syslog.Message(facility, level, text));
            c.Close();
        }

        protected override void OnStop()
        {
        }
    }
}
