using System;
using System.ServiceProcess;
using Microsoft.Owin.Hosting;
using NLog;
using Slevyr.DataAccess.Services;

namespace Slevyr.WebAppHost
{
    partial class Service : ServiceBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            int port = Globals.WebAppPort;
            string baseAddress = $"http://localhost:{port}/";

            if (SlevyrService.Start() && Globals.StartWebApi)
            {
                if (Globals.UseLocalHost)
                {
                    WebApp.Start<Startup>(url: baseAddress);
                }
                else
                {
                    WebApp.Start($"http://+:{port}/");
                }
            }
        }

        protected override void OnStop()
        {
            SlevyrService.Stop();
        }

    }
}
