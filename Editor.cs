using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;

namespace cubemapcreator;

public partial class Editor : Control
{
    [ExportGroup("Views")]
    [Export] public Array<CubemapTextureButton> CubemapTextureButtons { get; set; } = new();
    [Export] public Button ExportButton { get; set; }
    [Export] public Button ResetButton { get; set; }
    [Export] public Button FlipXAllButton { get; set; }
    [Export] public Button FlipYAllButton { get; set; }
    [Export] public MeshInstance3D PreviewMeshInstance { get; set; }
    [Export] public FileDialog LoadTextureDialog { get; set; }
    [Export] public FileDialog ExportTextureDialog { get; set; }
    [Export] public AcceptDialog ErrorDialog { get; set; }

    public IEnumerable<Image> CubemapImages => CubemapTextureButtons.Select(btn => btn.CubemapImage);
    public int ImagesCurrentlyLoaded => CubemapTextureButtons.Count(btn => btn.HasImageLoaded);
    
    protected const string SHADER_PATH = "res://shaders/cubemap_reflect.gdshader";

    protected Shader _cubemapPreviewShader;
    protected ShaderMaterial _cubemapPreviewMaterial;
    protected Cubemap _cubemap;
    protected string? _lastTexturePathUsed;
    
    public override void _Ready()
    {
        GD.Print("editor ready");
        foreach (var textureBtn in CubemapTextureButtons)
        {
            textureBtn.OnLoadingTextureStarted += onLoadTexture;
            textureBtn.OnTextureChanged += onCubemapTextureChanged;
        }

        CallDeferred(MethodName.printNumberOfButtons);
        
        ExportButton.Pressed += exportCubemap;
        ExportButton.Disabled = true;

        ResetButton.Pressed += onResetButtonPressed;

        FlipXAllButton.Pressed += () =>
        {
            foreach (var btn in CubemapTextureButtons)
            {
                btn.FlipX();
            }
        };
        FlipYAllButton.Pressed += () =>
        {
            foreach (var btn in CubemapTextureButtons)
            {
                btn.FlipY();
            }
        };

        _cubemapPreviewShader = ResourceLoader.Load<Shader>(SHADER_PATH);
        _cubemapPreviewMaterial = new ShaderMaterial { Shader = _cubemapPreviewShader };
        PreviewMeshInstance.SetSurfaceOverrideMaterial(0, _cubemapPreviewMaterial);
    }

    protected void printNumberOfButtons()
    {
        GD.Print("number of cubemap texture buttons " + CubemapTextureButtons.Count);
    }

    protected void onCubemapTextureChanged()
    {
        regenerateCubemap();
    }

    protected void onLoadTexture(CubemapTextureButton textureButton)
    {
        void loadTexture(string path)
        {
            try
            {
                var image = loadImage(path);
                textureButton.CommitImage(image);
                _lastTexturePathUsed = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
            }
            catch (CubemapLoadImageException e)
            {
                showErrorDialog(e.Message);
            }
            finally
            {
                LoadTextureDialog.FileSelected -= loadTexture;
            }
        }

        GD.Print("onLoadTexture " + textureButton.CubemapImage);
        LoadTextureDialog.FileSelected += loadTexture;
        LoadTextureDialog.CurrentPath = _lastTexturePathUsed;
        LoadTextureDialog.PopupCentered();
    }
    
    protected Image loadImage(string imagePath)
    {
        var loadedImage = Image.LoadFromFile(imagePath);
        if (loadedImage == null)
        {
            throw new CubemapLoadImageException("Loaded file was not an image");
        }
        
        var loadedDimensions = new Vector2I(loadedImage.GetWidth(), loadedImage.GetHeight());
        var loadedFormat = loadedImage.GetFormat();
        if (ImagesCurrentlyLoaded == 0)
        {
            // no images loaded, set the dimensions
            updateImageDimensions(loadedDimensions, loadedFormat);
            ExportButton.Disabled = false;
        }
        else
        {
            // load as normal, check dimensions
            var existingImage = CubemapTextureButtons.First(btn => btn.HasImageLoaded).CubemapImage;
            var existingDimensions = new Vector2I(existingImage.GetWidth(), existingImage.GetHeight());
            var existingFormat = existingImage.GetFormat();
            if (!loadedDimensions.Equals(existingDimensions))
            {
                throw new CubemapLoadImageException(
                    $"Loaded cubemap size ({loadedDimensions}) did not match existing size ({existingDimensions})");
            }
            if (!loadedFormat.Equals(existingFormat))
            {
                throw new CubemapLoadImageException(
                    $"Loaded cubemap format '{loadedFormat}' did not match existing format '{existingFormat}'");
            }
        }

        return loadedImage;
    }

    protected void updateImageDimensions(Vector2I dimensions, Image.Format format, CubemapTextureButton? existingButton = null)
    {
        foreach (var btn in CubemapTextureButtons)
        {
            if (btn == existingButton) continue;
            btn.SetDimensions(dimensions.X, dimensions.Y, format);
        }
    }

    protected void showErrorDialog(string errorMessage)
    {
        ErrorDialog.DialogText = errorMessage;
        ErrorDialog.PopupCentered();
    }

    protected void exportCubemap()
    {
        void exportTexture(string path)
        {
            var cubemapTextureDimension = new Vector2I(_cubemap.GetWidth(), _cubemap.GetHeight());
            var image = Image.CreateEmpty(cubemapTextureDimension.X * 2, cubemapTextureDimension.Y * 3, false,
                _cubemap.GetFormat());
            for (int i = 0; i < 6; i++)
            {
                var cubemapPart = _cubemap.GetLayerData(i);
                int x = i % 2;
                int y = i / 2;
                Vector2I cubemapPos = new Vector2I(x * cubemapTextureDimension.X, y * cubemapTextureDimension.Y);
                image.BlitRect(cubemapPart, new Rect2I(0, 0, cubemapTextureDimension), cubemapPos);
            }
            image.SavePng(path);
            
            ExportTextureDialog.FileSelected -= exportTexture;
        }
        
        ExportTextureDialog.CurrentPath = _lastTexturePathUsed;
        ExportTextureDialog.FileSelected += exportTexture;
        ExportTextureDialog.PopupCentered();
    }

    protected void onResetButtonPressed()
    {
        foreach (var btn in CubemapTextureButtons)
        {
            btn.Reset();
        }
        
        _cubemapPreviewMaterial.SetShaderParameter("cubemap_texture", new Cubemap());
        ExportButton.Disabled = true;
    }

    protected void regenerateCubemap()
    {
        _cubemap = new Cubemap();
        _cubemap.CreateFromImages(new Array<Image>(CubemapImages));
        _cubemapPreviewMaterial.SetShaderParameter("cubemap_texture", _cubemap);
    }
}
