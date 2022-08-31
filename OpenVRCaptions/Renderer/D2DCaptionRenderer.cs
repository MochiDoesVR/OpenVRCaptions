using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using OpenVRCaptions.Windows;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Valve.VR;
using WpfSharpDxControl;
using Color = SharpDX.Color;

namespace OpenVRCaptions.Renderer;

public class D2DCaptionRenderer : Direct2DComponent
{
    // DirectX Stuff
    private SolidColorBrush _textBrush;
    private TextFormat _format;

    // SteamVR Stuff
    private Texture_t _overlayTexture;
    private ulong overlayHandle = 0;

    // Configuration Properties
    public float fontSize 
    {
        get
        {
            return _fontSize;
        }
        set
        {
            _fontSize = value;
            if (_format != null)
            {
                _format.Dispose();
                _format = new TextFormat(FactoryDWrite, _fontFamily, _fontSize) { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };
            }
        }
    }
    public string fontFamily 
    {
        get
        {
            return _fontFamily;
        }
        set
        {
            _fontFamily = value;
            if (_format != null)
            {
                _format.Dispose();
                _format = new TextFormat(FactoryDWrite, _fontFamily, fontSize)
                    { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };
            }
        }
    }

    public Color textColor
    {
        get
        {
            return _textColor;
        }
        set
        {
            _textColor = value;
            if (_format != null)
            {
                _textBrush.Color = _textColor;
            }
        }
    }

    public Color backgroundColor = new Color(0, 0, 0, 0.5f);

    // Backing Fields
    private Color _textColor = Color.White;
    private string _fontFamily = "Segoe UI";
    private float _fontSize = 32;

    public int captionQueueSize = 5;
    
    public D2DCaptionRenderer(ulong handle) => overlayHandle = handle;
    
    protected override void InternalInitialize()
    {
        base.InternalInitialize();
        
        _textBrush = new SolidColorBrush(RenderTarget2D, _textColor);
        
        _format = new TextFormat(FactoryDWrite, _fontFamily, fontSize) { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };
        _overlayTexture = new Texture_t() { eType = ETextureType.DirectX, eColorSpace = EColorSpace.Auto, handle = BackBuffer.NativePointer };
        
        OpenVR.Overlay.ShowOverlay(overlayHandle);
    }

    protected override void InternalUninitialize()
    {
        base.InternalUninitialize();
        
        _textBrush.Dispose();

        _format.Dispose();
    }

    private Queue<string> oldCaptions = new Queue<string>();
    private string currentCaption = "";
    
    protected override void Render()
    {
        RenderTarget2D.Clear(backgroundColor);
        
        StringBuilder sb = new StringBuilder();
        foreach (string oldCaption in oldCaptions)
        {
            sb.Append(oldCaption + '\n');
        }

        var size = MeasureString($"{sb}\n[{currentCaption}]", _format, OverrideWidth-128);
        
        _format.ParagraphAlignment = size.Height > OverrideHeight-128 ? ParagraphAlignment.Far : ParagraphAlignment.Center;
        
        RenderTarget2D.DrawText($"{sb}[{currentCaption}]", _format, new RawRectangleF(64,64, OverrideWidth-64,OverrideHeight-64), _textBrush);
        
        OpenVR.Overlay.SetOverlayTexture(overlayHandle, ref _overlayTexture);

        // Get Overlay Position
        var origin = ETrackingUniverseOrigin.TrackingUniverseStanding;
        var transform = new HmdMatrix34_t();
        OpenVR.Overlay.GetOverlayTransformAbsolute(overlayHandle, ref origin, ref transform);

        // Get HMD Position
        var poses = new TrackedDevicePose_t[1];
        OpenVR.System.GetDeviceToAbsoluteTrackingPose(origin, 0, poses);
        var hmdPosition = poses[0].mDeviceToAbsoluteTracking.ToMatrix4x4().Translation;
        
        // Set Overlay Position
        var newTransformMtx = Matrix4x4.Multiply(Matrix4x4.CreateTranslation(hmdPosition.X, hmdPosition.Y - 1f, hmdPosition.Z), Matrix4x4.CreateRotationX(45f.ToRadians(), hmdPosition)).ToHmdMatrix34_t();
        OpenVR.Overlay.SetOverlayTransformAbsolute(overlayHandle, origin, ref newTransformMtx);
    }
    
    private SizeF MeasureString(string content, TextFormat textFormat, float width)
    {
        var layout = new TextLayout(FactoryDWrite, content, textFormat, width, textFormat.FontSize);
        
        // HACK: We have to create a SizeF and dispose of the TextLayout before continuing, because SharpDX has a lot of problems with memory leaks D:
        var size = new SizeF(layout.Metrics.Width, layout.Metrics.Height);
        layout.Dispose();

        return size;
    }

    public void UpdateCaption(string input)
    {
        currentCaption = $"{input}";
    }

    public void FinalizeCaption()
    {
        oldCaptions.Enqueue(currentCaption);
        currentCaption = "";

        if (oldCaptions.Count > 5)
        {
            oldCaptions.Dequeue();
        }
    }
    
    public void FinalizeCaption(string input)
    {
        oldCaptions.Enqueue(input);
        currentCaption = "";

        if (oldCaptions.Count > 5)
        {
            oldCaptions.Dequeue();
        }
    }
}