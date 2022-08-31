using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using Newtonsoft.Json;
using OpenVRCaptions.Core;
using OpenVRCaptions.Renderer;
using Valve.VR;
using Vosk;
using Color = System.Drawing.Color;
using ColorConverter = System.Drawing.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using TextBox = System.Windows.Controls.TextBox;

namespace OpenVRCaptions.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ITheme theme = Theme.Create(Theme.Dark, SwatchHelper.Lookup[(MaterialDesignColor)PrimaryColor.DeepPurple], SwatchHelper.Lookup[(MaterialDesignColor)SecondaryColor.DeepPurple]);
            new PaletteHelper().SetTheme(theme);
            Visibility = Visibility.Hidden;
            ShowInTaskbar = false;
            InitializeComponent();

            #region Init OpenVR
            
            // Initiialize a connection to OpenVR, and throw a fatal error if the runtime reports any errors, or if an exception is thrown.
            ulong overlayHandle = 0;
            try
            {
                EVRInitError initError = EVRInitError.None;
                OpenVR.Init(ref initError, EVRApplicationType.VRApplication_Overlay);

                if (initError != EVRInitError.None)
                {
                    DialogWindow.CreateDialog("Fatal Error!", $"Failed to connect to SteamVR! (Error: {initError})", "Quit", (d) => Environment.Exit(-1));
                    return;
                }

                EVROverlayError overlayInitError = EVROverlayError.None;
                OpenVR.Overlay.CreateOverlay("mochidoesvr.openvrcaptionoverlay", "OpenVRCaptions Overlay", ref overlayHandle);
                OpenVR.Overlay.SetOverlayWidthInMeters(overlayHandle, 0.5f);
            }
            catch (Exception e)
            {
                DialogWindow.CreateDialog("Fatal Error!", $"Failed to initialize DirectX 11! (Caught {e.GetType()})", "Quit", (d) => Environment.Exit(-1));
                return;
            }

            #endregion

            #region Init DirectX 11 Renderer
            
            // Initialize the DirectX 11 Renderer, and throw a fatal error if something fails.
            D2DCaptionRenderer captionRenderer = null;
            try
            {
                captionRenderer = new Renderer.D2DCaptionRenderer(overlayHandle) { OverrideRenderSize = true, OverrideWidth = 1024, OverrideHeight = 512 };
                DirectXView.Content = captionRenderer;
            }
            catch (Exception e)
            {
                DialogWindow.CreateDialog("Fatal Error!", $"Failed to initialize DirectX 11! (Caught {e.GetType()})", "Quit", (d) => Environment.Exit(-1));
                return;
            }

            #endregion

            #region Init Vosk
            
            // Initialize Vosk and WASAPI Loopback Capture, and throw a fatal error if something fails.
            var input = new WasapiLoopbackCapture()
            {
                WaveFormat = new WaveFormat(rate: 16000, bits: 16, channels: 1)
            };
            try
            {
                Vosk.Vosk.SetLogLevel(0);
                VoskRecognizer rec = new VoskRecognizer(new Model(ApplicationConfig.instance.VoskModelPath), 16000.0f);
                rec.SetMaxAlternatives(0);
                rec.SetWords(true);

                input.DataAvailable += (sender, args) =>
                {
                    if (rec.AcceptWaveform(args.Buffer, args.BytesRecorded))
                    {
                        string transcribedAudio = JsonConvert.DeserializeObject<TranscriptionResult>(rec.FinalResult()).Text;

                        if (!string.IsNullOrEmpty(transcribedAudio))
                        {
                            captionRenderer.FinalizeCaption();
                        }
                    }
                    else
                    {
                        string transcribedAudio = JsonConvert
                            .DeserializeObject<PartialTranscriptionResult>(rec.PartialResult()).Partial;

                        if (!string.IsNullOrEmpty(transcribedAudio))
                        {
                            captionRenderer.UpdateCaption(transcribedAudio);
                        }
                    }
                };
                
                input.StartRecording();
            }
            catch (Exception e)
            {
                DialogWindow.CreateDialog("Fatal Error!", $"Failed to initialize voice recognition engine! (Caught {e.GetType()})", "Quit", (d) => Environment.Exit(-1));
            }
            
            #endregion

            #region Register UI Callbacks

            try
            {
                // Intentionally writes to OOB memory to trigger an access violation.
                crash.Click += (o, e) => Marshal.StructureToPtr(69, new IntPtr(1), true);
                ViewCreditsButton.Click += (o, e) => new CreditsWindow().Show();

                    FontSize.TextChanged += (o, e) =>
                {
                    FontSize.FilterNonNumericValues();
                    if (float.TryParse(FontSize.Text, out var val))
                    {
                        captionRenderer.fontSize = val;
                    }
                };

                FontFamilySelector.SelectionChanged += (o, e) =>
                    captionRenderer.fontFamily = ((FontFamily)FontFamilySelector.SelectedValue).Source;

                TextColorInput.TextChanged += (o, e) =>
                {
                    var c = TextColorInput.Text.FromHexColor();
                    if (c != null)
                    {
                        TextColorPreview.Fill = new SolidColorBrush(c.Value.ToSystemMedia());
                        captionRenderer.textColor = c.Value.ToSharpDx();
                    }
                };

                BackgroundInput.TextChanged += (o, e) =>
                {
                    var c = BackgroundInput.Text.FromHexColor();
                    if (c != null)
                    {
                        BackgroundPreview.Fill = new SolidColorBrush(c.Value.ToSystemMedia());
                        captionRenderer.backgroundColor = c.Value.ToSharpDx();
                    }
                };

                Closed += (o, e) =>
                {
                    ApplicationConfig.instance.fontSize = float.Parse(FontSize.Text);
                    ApplicationConfig.instance.fontFamily = ((FontFamily)FontFamilySelector.SelectedItem).Source;

                    ApplicationConfig.instance.backgroundColor = captionRenderer.backgroundColor;
                    ApplicationConfig.instance.textColor = captionRenderer.textColor;

                    File.WriteAllText("config.json", JsonConvert.SerializeObject(ApplicationConfig.instance));
                    Environment.Exit(0);
                };
            }
            catch (Exception e)
            {
                DialogWindow.CreateDialog("Fatal Error!", $"Failed To Register UI Events! (Caught {e.GetType()})", "Quit", (d) => Environment.Exit(-1));
                return;
            }

            #endregion
            
            #region Set Initial UI State

            try
            {
                FontSize.Text = ApplicationConfig.instance.fontSize.ToString();
                FontFamilySelector.SelectedItem = Fonts.SystemFontFamilies.First(x => x.Source == ApplicationConfig.instance.fontFamily);
                BackgroundInput.Text = Extensions.ToHexColor(ApplicationConfig.instance.backgroundColor.FromSharpDx());
                TextColorInput.Text = Extensions.ToHexColor(ApplicationConfig.instance.textColor.FromSharpDx());

                BackgroundPreview.Fill = new SolidColorBrush(ApplicationConfig.instance.backgroundColor.FromSharpDx().ToSystemMedia());
                TextColorPreview.Fill = new SolidColorBrush(ApplicationConfig.instance.textColor.FromSharpDx().ToSystemMedia());
                TextColorPreview.Opacity = (float)ApplicationConfig.instance.textColor.A / 255;
                BackgroundPreview.Opacity = (float)ApplicationConfig.instance.backgroundColor.A / 255;

                captionRenderer.textColor = ApplicationConfig.instance.textColor;
                captionRenderer.backgroundColor = ApplicationConfig.instance.backgroundColor;
                captionRenderer.fontSize = ApplicationConfig.instance.fontSize;
                captionRenderer.fontFamily = ApplicationConfig.instance.fontFamily;
            }
            catch (Exception e)
            {
                DialogWindow.CreateDialog("Fatal Error!", $"Failed To Set Initial UI State! (Caught {e.GetType()})", "Quit", (d) => Environment.Exit(-1));
                return;
            }

            #endregion
            
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
        }
    }

    public static class Extensions
    {
        public static void FilterNonNumericValues(this TextBox box)
        {
            box.Text = new string(box.Text.Where(c => char.IsDigit(c)).ToArray());
        }
        
        public static string ToHexColor(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

        public static Color? FromHexColor(this string box)
        {
            try
            {
                var col = (Color)new ColorConverter().ConvertFromString("#"+box[7..9]+box[1..7]);
                return col;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public static System.Windows.Media.Color ToSystemMedia(this Color brush)
        {
            return new System.Windows.Media.Color()
            {
                R = brush.R,
                G = brush.G,
                B = brush.B,
                A = brush.A
            };
        }
        
        public static Color FromSharpDx(this SharpDX.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        public static SharpDX.Color ToSharpDx(this Color color)
        {
            return new SharpDX.Color(color.R, color.G, color.B, color.A);
        }
    }
    
    struct PartialTranscriptionResult
    {
        public string Partial { get; set; }
    }
    struct TranscriptionResult
    {
        public string Text { get; set; }
    }
}