using System;
using Godot;

namespace cubemapcreator;

[Tool]
public partial class CubemapTextureButton : Button
{
    [Export] public TextureRect TextureDisplay { get; set; }
    [Export] public Label ButtonLabel { get; set; }
    [Export] public Button FlipXButton { get; set; }
    [Export] public Button FlipYButton { get; set; }
    [Export] public CubemapTexture TextureKind
    {
        get => _textureKind;
        set
        {
            _textureKind = value;
            ButtonLabel.Text = value switch
            {
                CubemapTexture.XPos => "X+",
                CubemapTexture.XNeg => "X-",
                CubemapTexture.YPos => "Y+",
                CubemapTexture.YNeg => "Y-",
                CubemapTexture.ZPos => "Z+",
                CubemapTexture.ZNeg => "Z-",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }

    [Signal] public delegate void OnLoadingTextureStartedEventHandler(CubemapTextureButton textureButton);

    [Signal] public delegate void OnTextureChangedEventHandler();

    public Image? CubemapImage
    {
        get => _cubemapImage;
        set
        {
            _cubemapImage = value;
            regenerateTexturePreview();
        }
    }

    public bool HasImageLoaded { get; protected set; } = false;

    public Texture? Texture => _generatedTexture;

    protected CubemapTexture _textureKind = CubemapTexture.XPos;
    protected Image? _cubemapImage;
    protected Texture2D? _generatedTexture;

    public override void _Ready()
    {
        GD.Print("CubemapTextureButton _Ready");
        if (Engine.IsEditorHint()) return;

        GD.Print("setting pressed behaviour");
        Pressed += onPressed;

        FlipXButton.Disabled = true;
        FlipYButton.Disabled = true;
        FlipXButton.Pressed += FlipX;
        FlipYButton.Pressed += FlipY;
    }

    public void SetDimensions(int width, int height, Image.Format format)
    {
        _cubemapImage = Image.CreateEmpty(width, height, false, format);
    }

    public void CommitImage(Image image)
    {
        CubemapImage = image;
        regenerateTexturePreview();
        HasImageLoaded = true;
        FlipXButton.Disabled = false;
        FlipYButton.Disabled = false;
    }

    public void Reset()
    {
        CubemapImage = null;
        FlipXButton.Disabled = true;
        FlipYButton.Disabled = true;
        HasImageLoaded = false;
        TextureDisplay.Texture = null;
    }

    public void FlipX()
    {
        if (CubemapImage == null) return;
        CubemapImage.FlipX();
        regenerateTexturePreview();
    }

    public void FlipY()
    {
        if (CubemapImage == null) return;
        CubemapImage.FlipY();
        regenerateTexturePreview();
    }

    protected void regenerateTexturePreview()
    {
        if (CubemapImage == null) return;
        _generatedTexture = ImageTexture.CreateFromImage(CubemapImage);
        TextureDisplay.Texture = _generatedTexture;
        EmitSignal(SignalName.OnTextureChanged);
    }
    
    protected void onPressed()
    {
        GD.Print("emitting OnLoadingTextureStarted");
        EmitSignal(SignalName.OnLoadingTextureStarted, this);
    }
}
