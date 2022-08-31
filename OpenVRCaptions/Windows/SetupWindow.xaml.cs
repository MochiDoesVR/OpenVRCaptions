using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using Newtonsoft.Json;
using OpenVRCaptions.Core;
using Valve.VR;
using Vosk;
using Path = System.IO.Path;

namespace OpenVRCaptions.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            ITheme theme = Theme.Create(Theme.Dark, SwatchHelper.Lookup[(MaterialDesignColor)PrimaryColor.DeepPurple],
                SwatchHelper.Lookup[(MaterialDesignColor)SecondaryColor.DeepPurple]);
            new PaletteHelper().SetTheme(theme);
            InitializeComponent();

            ButtonQuit.Click += (o,e) => Environment.Exit(0);
            
            ButtonInstall.Click += (o, e) =>
            {
                InstallStateText.Visibility = Visibility.Visible;
                InstallProgressText.Visibility = Visibility.Visible;
                InstallProgressBar.Visibility = Visibility.Visible;
                ButtonQuit.Visibility = Visibility.Hidden;
                ButtonInstall.Visibility = Visibility.Hidden;

                var wc = new WebClient();
                wc.DownloadFileAsync(new Uri("https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip"),
                    "vosk_model.zip");

                InstallStateText.Text = "Downloading Voice Recognition Model...";

                wc.DownloadProgressChanged += (sender, args) =>
                {
                    InstallProgressBar.Value = args.ProgressPercentage;
                    InstallProgressText.Text =
                        $"{args.ProgressPercentage}% Complete ({MathF.Round((float)args.BytesReceived / 1024 / 1024, 2)}MB / {MathF.Round((float)args.TotalBytesToReceive / 1024 / 1024, 2)}MB)";
                };
                wc.DownloadFileCompleted += (sender, args) =>
                {
                    InstallStateText.Text = "Extracting Voice Recognition Model...";
                    InstallProgressText.Text = "N/A";
                    InstallProgressBar.Value = 0;

                    ZipFile.ExtractToDirectory("vosk_model.zip",
                        Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName,
                            "vosk_model"), true);
                    File.Delete("vosk_model.zip");
                    InstallProgressBar.Value = 99;

                    InstallStateText.Text = "Writing Configuration...";
                    InstallProgressText.Text = "N/A";

                    File.WriteAllText("config.json", JsonConvert.SerializeObject(new ApplicationConfig()
                    {
                        VoskModelPath =
                            new DirectoryInfo(Path.Combine(
                                    new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "vosk_model"))
                                .GetDirectories()[0].FullName
                    }));

                    new MainWindow().Show();
                    Close();
                };
            };
        }
    }
}