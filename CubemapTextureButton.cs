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

    public Image CubemapImage
    {
        get => _cubemapImage;
        set
        {
            _cubemapImage = value;
            regenerateTexturePreview();
        }
    }

    public bool HasImageLoaded { get; protected set; } = false;

    public Texture Texture => _generatedTexture;

    protected CubemapTexture _textureKind = CubemapTexture.XPos;
    protected Image _cubemapImage;
    protected Texture2D _generatedTexture;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        Pressed += onPressed;

        FlipXButton.Disabled = true;
        FlipYButton.Disabled = true;
        FlipXButton.Pressed += FlipX;
        FlipYButton.Pressed += FlipY;
    }

    public void SetDimensions(int width, int height)
    {
        _cubemapImage = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
    }

    public void CommitImage(Image image)
    {
        CubemapImage = image;
        regenerateTexturePreview();
        HasImageLoaded = true;
        FlipXButton.Disabled = false;
        FlipYButton.Disabled = false;
    }

    public void FlipX()
    {
        CubemapImage.FlipX();
        regenerateTexturePreview();
    }

    public void FlipY()
    {
        CubemapImage.FlipY();
        regenerateTexturePreview();
    }

    protected void regenerateTexturePreview()
    {
        _generatedTexture = ImageTexture.CreateFromImage(CubemapImage);
        TextureDisplay.Texture = _generatedTexture;
        EmitSignal(SignalName.OnTextureChanged);
    }
    
    protected void onPressed()
    {
        EmitSignal(SignalName.OnLoadingTextureStarted, this);
    }
}
