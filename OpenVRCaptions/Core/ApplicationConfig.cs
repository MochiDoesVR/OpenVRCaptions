using SharpDX;

namespace OpenVRCaptions.Core
{
    public class ApplicationConfig
    {
        public static ApplicationConfig instance;

        public ApplicationConfig()
        {
            instance ??= this;
        }
        
        
        public string VoskModelPath;
        
        public Color textColor { get; set; } = Color.White;
        public Color backgroundColor { get; set; } = new Color(0, 0, 0, 0.5f);
        public string fontFamily = "Segoe UI";
        public float fontSize = 32;
    }
}