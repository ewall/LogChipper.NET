using System;
using System.Net;
using System.Net.Sockets;

namespace Syslog
{
    #region datastructs
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
    #endregion datastructs

    public class Client
    {
        private string        _hostIp;
        private int           _port;
        private bool          _useTCP;
        private UdpClient     _udpSocket;
        private TcpClient     _tcpSocket;
        private NetworkStream _stream;

        #region constructors
        public Client(string server, int port, bool tcp)
        {
            this._hostIp = server;
            this._port = port;
            this._useTCP = tcp;
            if (_useTCP)
            {
                this._tcpSocket = new TcpClient(_hostIp, _port);
                this._stream = _tcpSocket.GetStream();
                this._tcpSocket.LingerState = new LingerOption(true, 30);
            }
            else
            {
                this._udpSocket = new UdpClient(_hostIp, _port);
            }
        }

        public Client(string server, int port, bool tcp, int facility, int level)
            : this(server, port, tcp)
        {
            this._defaultFacility = facility;
            this._defaultLevel = level;
        }
        #endregion constructors

        #region properties
        private int _defaultFacility = (int)Facility.Syslog;
        public int DefaultFacility { get; set; }

        private int _defaultLevel = (int)Level.Warning;
        public int DefaultLevel { get; set; }
        #endregion properties

        #region methods
        // Send() long form with enum
        public void Send(Syslog.Message message)
        {
            string msg = System.String.Format("<{0}>{1}", message.Facility * 8 + message.Level, message.Text);
            byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(msg);

            // TODO: change to use asynchronous sending?

            if (_useTCP)
            {
                bool success = false;
                do
                {
                    try
                    {
                        _stream.Write(sendBytes, 0, sendBytes.Length);
                        _stream.Flush();
                        success = _tcpSocket.Connected;
                    }
                    catch (SocketException e)
                    {
                        // 10035 == WSAEWOULDBLOCK i.e. already connected
                        if (e.NativeErrorCode != 10035)
                        {
                            //_tcpSocket.Connect(_hostIp, _port); // no, need to establish fresh new connection
                            if (_stream != null) _stream.Close();
                            if (_tcpSocket != null) _tcpSocket.Close();
                            _tcpSocket = new TcpClient(_hostIp, _port);
                            _stream = _tcpSocket.GetStream();
                            _tcpSocket.LingerState = new LingerOption(true, 30);
                        }
                    }
                    catch (ObjectDisposedException e)
                    {
                        if (_stream != null) _stream.Close();
                        if (_tcpSocket != null) _tcpSocket.Close();
                        _tcpSocket = new TcpClient(_hostIp, _port);
                        _stream = _tcpSocket.GetStream();
                        _tcpSocket.LingerState = new LingerOption(true, 30);
                    }
                } while (!success);
            }
            else
            {
                _udpSocket.Send(sendBytes, sendBytes.Length);
            }
        }

        // Send() simplified overload uses previously set default facility & level
        public void Send(string text)
        {
            Send(new Message(_defaultFacility, _defaultLevel, text));
        }
        #endregion methods

        #region destructors
        public void Close()
        {
            if (_useTCP)
            {
                _stream.Close();
                _tcpSocket.Close();
            }
            else
            {
                _udpSocket.Close();
            }
        }

        ~Client()
        {
            Close();
        }
        #endregion destuctors

        // TODO: support callback for detailed logging?
    }

    public class DemoClient
    {

        public static void Main(string[] args)
        {

            Syslog.Client c = new Syslog.Client("127.0.0.1", 514, false, (int)Syslog.Facility.Syslog, (int)Syslog.Level.Warning);
            try
            {
                c.Send("This is a test of the syslog client code.");
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                c.Close();
            }
        }
    }
}
