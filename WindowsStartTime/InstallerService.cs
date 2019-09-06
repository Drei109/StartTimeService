using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WindowsStartTime
{
    [RunInstaller(true)]
    public partial class InstallerService : System.Configuration.Install.Installer
    {
        public InstallerService()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            try
            {
                // Retrieve configuration settings

                //string local = Context.Parameters["NOMBRE_COMP"];
                //string baseAddress = Context.Parameters["DIRECCION_SERV"];

                string interval = Context.Parameters["INTERVALO"];
                string tipo = Context.Parameters["TIPO"];

                ExeConfigurationFileMap map = new ExeConfigurationFileMap();
                string configFile = string.Concat(Assembly.GetExecutingAssembly().Location, ".config");
                map.ExeConfigFilename = configFile;
                System.Configuration.Configuration config = System.Configuration.ConfigurationManager.
                OpenMappedExeConfiguration(map, System.Configuration.ConfigurationUserLevel.None);

                //config.AppSettings.Settings["local"].Value = local;
                //config.AppSettings.Settings["baseAddress"].Value = baseAddress;

                config.AppSettings.Settings["tipo"].Value = tipo;
                config.AppSettings.Settings["timeInterval"].Value = interval;
                config.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg = ex.InnerException.ToString();
                }
            }
        }
    }
}