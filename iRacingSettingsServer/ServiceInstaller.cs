using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Text;

namespace iRacingSettingsServer
{
    [System.ComponentModel.RunInstaller(true)]
    public class ServiceInstaller : Installer
    {
        /// <summary>
        ///    Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components;
        private System.ServiceProcess.ServiceInstaller serviceInstaller;
        private System.ServiceProcess.ServiceProcessInstaller processInstaller;

        public ServiceInstaller()
        {
            serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            processInstaller =
              new System.ServiceProcess.ServiceProcessInstaller();

            serviceInstaller.Description = "iRacing Settings Server";
            serviceInstaller.DisplayName = "iRacing Settings Server";
            serviceInstaller.ServiceName = "iRacingSettingsServer";
            serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
           
            processInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            processInstaller.Password = null;
            processInstaller.Username = null;

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
