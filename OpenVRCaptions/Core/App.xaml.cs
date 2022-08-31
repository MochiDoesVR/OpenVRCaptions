using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using OpenVRCaptions.Windows;

namespace OpenVRCaptions.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void PreInit(object sender, StartupEventArgs e)
        {
            if (File.Exists("config.json"))
            {
                JsonConvert.DeserializeObject<ApplicationConfig>(File.ReadAllText("config.json"));
                if (ApplicationConfig.instance != null)
                {
                    if (Directory.Exists(ApplicationConfig.instance.VoskModelPath))
                    {
                        PreInitConfigSuccess();
                    }
                    else
                    {
                        ApplicationConfig.instance = null;
                        PreInitConfigFail();
                    }
                }
                else
                {
                    PreInitConfigFail();
                }
            }
            else
            {
                PreInitConfigFail();
            }
        }

        private void PreInitConfigSuccess()
        {
            Current.MainWindow = new MainWindow();
            Current.MainWindow.Show();
        }
        
        private void PreInitConfigFail()
        {
            Current.MainWindow = new SetupWindow();
            Current.MainWindow.Show();
        }
    }
}