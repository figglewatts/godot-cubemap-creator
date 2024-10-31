using System;

namespace cubemapcreator;

public class CubemapLoadImageException : Exception
{
    public CubemapLoadImageException() : base("Failed to load image for cubemap.") { }

    public CubemapLoadImageException(string message) : base(message) { }

    public CubemapLoadImageException(string message, Exception innerException) 
        : base(message, innerException) { }
}