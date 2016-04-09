using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace iRacingSettingsServer
{
    public class Server : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            Program.Start();
        }

        protected override void OnStop()
        {
            Program.Stop();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
    }

    public static class Program
    {
        static EventLog Log;
        static HttpListener Listener;
 
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {           
            ServiceBase.Run(new ServiceBase[] { new Server() { CanPauseAndContinue = false }});
        }

        public static void Start()
        {
            try
            {
                Listener = new HttpListener();

                Log = new EventLog();
                Log.Source = "iRacingSettingsServer";

                Log.WriteEntry("Starting Http Listener.");

                Listener.Prefixes.Add("http://localhost:52028/");
                Listener.Start();

                Listener.BeginGetContext(ContextReceived, null);
            }
            catch (Exception e)
            {
                Log.WriteEntry(e.ToString());
                throw;
            }
        }

        public static void Stop()
        {
            Log.WriteEntry("Closing down.");

            Listener.Stop();
            Log.Close();
        }

        private static void ContextReceived(IAsyncResult ar)
        {
            var context = Listener.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;

            var role = request.QueryString["role"];
            if (string.IsNullOrEmpty(role))
            {
                Log.WriteEntry("Request did not specify a role.", EventLogEntryType.Warning);
            }
            else
            {
                UpdateSettings(role);
            }

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Close();

            Listener.BeginGetContext(ContextReceived, null);
        }

        private static void UpdateSettings(string role)
        {
            var helperPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "iRacingSettingsHelper.exe");
            var settingsHelper = new ProcessStarter("iRacingSettingsHelper", helperPath, role);
            settingsHelper.Run();
            if (settingsHelper.WaitForExit() != 0)
            {
                Log.WriteEntry("iRacing Settings Helper failed.", EventLogEntryType.Error);
            }
        }
    }
}
