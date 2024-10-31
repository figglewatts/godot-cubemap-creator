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
        foreach (var textureBtn in CubemapTextureButtons)
        {
            textureBtn.OnLoadingTextureStarted += onLoadTexture;
            textureBtn.OnTextureChanged += onCubemapTextureChanged;
        }

        ExportButton.Pressed += exportCubemap;
        ExportButton.Disabled = true;

        _cubemapPreviewShader = ResourceLoader.Load<Shader>(SHADER_PATH);
        _cubemapPreviewMaterial = new ShaderMaterial { Shader = _cubemapPreviewShader };
        PreviewMeshInstance.SetSurfaceOverrideMaterial(0, _cubemapPreviewMaterial);
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
                _lastTexturePathUsed = Path.GetDirectoryName(path);
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
        if (ImagesCurrentlyLoaded == 0)
        {
            // no images loaded, set the dimensions
            updateImageDimensions(loadedDimensions);
            ExportButton.Disabled = false;
        }
        else
        {
            // load as normal, check dimensions
            var existingImage = CubemapTextureButtons.First(btn => btn.HasImageLoaded).CubemapImage;
            var existingDimensions = new Vector2I(existingImage.GetWidth(), existingImage.GetHeight());
            if (!loadedDimensions.Equals(existingDimensions))
            {
                throw new CubemapLoadImageException("Loaded cubemap size did not match existing size");
            }
        }

        return loadedImage;
    }

    protected void updateImageDimensions(Vector2I dimensions, CubemapTextureButton? existingButton = null)
    {
        foreach (var btn in CubemapTextureButtons)
        {
            if (btn == existingButton) continue;
            btn.SetDimensions(dimensions.X, dimensions.Y);
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
                Image.Format.Rgba8);
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

    protected void regenerateCubemap()
    {
        _cubemap = new Cubemap();
        _cubemap.CreateFromImages(new Array<Image>(CubemapImages));
        _cubemapPreviewMaterial.SetShaderParameter("cubemap_texture", _cubemap);
    }
}
