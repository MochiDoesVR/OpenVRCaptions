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
    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {
            ITheme theme = Theme.Create(Theme.Dark, SwatchHelper.Lookup[(MaterialDesignColor)PrimaryColor.DeepPurple], SwatchHelper.Lookup[(MaterialDesignColor)SecondaryColor.DeepPurple]);
            new PaletteHelper().SetTheme(theme);
            InitializeComponent();
        }

        public static DialogWindow CreateDialog(string title, string message,  string acceptButtonText = "", Action<DialogWindow> onAccept = null, string denyButtonText = "", Action<DialogWindow> onDeny = null)
        {
            var dialog = new DialogWindow();
            dialog.Show();

            dialog.Title = title;
            dialog.MessageText.Text = message;

            if (string.IsNullOrEmpty(acceptButtonText))
            {
                dialog.ButtonAccept.Visibility = Visibility.Collapsed;
            }
            else
            {
                dialog.ButtonAccept.Content = acceptButtonText;
                if (onAccept != null)
                {
                    dialog.ButtonAccept.Click += (o, e) => onAccept.Invoke(dialog);
                }
            }

            if (string.IsNullOrEmpty(denyButtonText))
            {
                dialog.ButtonDeny.Visibility = Visibility.Collapsed;
                if (onDeny != null)
                {
                    dialog.ButtonDeny.Click += (o, e) => onDeny.Invoke(dialog);
                }
            }
            else
            {
                dialog.ButtonDeny.Content = denyButtonText;
            }

            return dialog;
        }
    }
}