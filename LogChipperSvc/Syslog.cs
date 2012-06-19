using System;
using System.Net;
using System.Net.Sockets;

namespace Syslog
{
    public enum Level
    {
        Emergency = 0,
        Alert = 1,
        Critical = 2,
        Error = 3,
        Warning = 4,
        Notice = 5,
        Information = 6,
        Debug = 7,
    }

    public enum Facility
    {
        Kernel = 0,
        User = 1,
        Mail = 2,
        Daemon = 3,
        Auth = 4,
        Syslog = 5,
        Lpr = 6,
        News = 7,
        UUCP = 8,
        Cron = 9,
        Local0 = 10,
        Local1 = 11,
        Local2 = 12,
        Local3 = 13,
        Local4 = 14,
        Local5 = 15,
        Local6 = 16,
        Local7 = 17,
    }

    public class Message
    {
        public int Facility { get; set; }
        public int Level { get; set; }
        public string Text { get; set; }

        public Message() { }
        public Message(int facility, int level, string text)
        {
            Facility = facility;
            Level = level;
            Text = text;
        }
    }

    // Helper class exposes the UdpClient's "Active" propery
    public class Helper : System.Net.Sockets.UdpClient
    {
        public Helper() : base() { }
        public Helper(IPEndPoint ipe) : base(ipe) { }
        ~Helper()
        {
            if (this.Active) this.Close();
        }

        public bool IsActive
        {
            get { return this.Active; }
        }
    }

    public class Client
    {
        private IPHostEntry ipHostInfo;
        private IPAddress ipAddress;
        private IPEndPoint ipLocalEndPoint;
        private Syslog.Helper helper;

        public Client()
        {
            ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHostInfo.AddressList[0];
            ipLocalEndPoint = new IPEndPoint(ipAddress, 0);
            helper = new Syslog.Helper(ipLocalEndPoint);
        }

        public bool IsActive
        {
            get { return helper.IsActive; }
        }

        private string _hostIp = null;
        public string HostIp
        {
            get { return _hostIp; }
            set
            {
                if ((_hostIp == null) && (!IsActive))
                {
                    _hostIp = value;
                    //helper.Connect(_hostIp, _port);
                }
            }
        }

        private int _port = 514;
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        private int _defaultFacility = (int)Facility.Syslog;
        public int DefaultFacility
        {
            get { return _defaultFacility; }
            set { _defaultFacility = DefaultFacility; }
        }

        private int _defaultLevel = (int)Level.Warning;
        public int DefaultLevel
        {
            get { return _defaultLevel; }
            set { _defaultLevel = DefaultLevel; }
        }

        // Send() original method
        public void Send(Syslog.Message message)
        {
            if (!helper.IsActive)
                helper.Connect(_hostIp, _port);
            if (helper.IsActive)
            {
                string msg = System.String.Format("<{0}>{1}", message.Facility * 8 + message.Level, message.Text);
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(msg);
                helper.Send(bytes, bytes.Length);
            }
            else throw new Exception("Syslog client socket is not connected. Please double-check that the host IP and port are set correctly.");
        }

        // Send() simplified overload uses previously set default facility & level
        public void Send(string text)
        {
            Send(new Message(_defaultFacility, _defaultLevel, text));
        }
        
        // destructors
        public void Close()
        {
            if (helper.IsActive) helper.Close();
        }

        public ~ Client()
        {
            Close();
        }
    }

    // TODO: extend & clarify examples
    public class TestClient
    {

        public static void Main(string[] args)
        {

            Syslog.Client c = new Syslog.Client();
            try
            {
                c.HostIp = "127.0.0.1";  // syslogd on local machine
                //c.Port= 1200;
                int facility = (int)Syslog.Facility.Syslog;
                int level = (int)Syslog.Level.Warning;
                string text = (args.Length > 0) ? args[0] : "Hello, Syslog World.";

                c.Send(new Syslog.Message(facility, level, text));
            }
            catch (System.Exception ex1)
            {
                Console.WriteLine("Exception! " + ex1);
            }
            finally
            {
                c.Close();
            }

        }
    }
}
