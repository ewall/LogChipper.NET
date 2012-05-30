using System.ServiceProcess;

namespace LogChipperSvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new LogChipperService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
