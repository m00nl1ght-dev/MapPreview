/*
 
Modified part of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/MapPreviewGenerator.cs

MIT License

Copyright (c) 2017 UnlimitedHugs, modifications (c) 2022 m00nl1ght <https://github.com/m00nl1ght-dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewResult
{
    public readonly Color[] Pixels;

    public readonly MapPreviewRequest Request;

    public IntVec2 TextureSize => Request.TextureSize;
    public IntVec2 MapSize => Request.MapSize;
    public int MapTile => Request.MapTile;

    public Rect TexCoords => new(0, 0, MapSize.x / (float) TextureSize.x, MapSize.z / (float) TextureSize.z);

    public int InvalidCells { get; internal set; }
    
    public Map Map;

    public MapPreviewResult(MapPreviewRequest request)
    {
        Request = request;
        var buffer = request.ExistingBuffer;
        var textureSize = request.TextureSize;
        if (buffer != null && buffer.Length == textureSize.x * textureSize.z) Pixels = buffer;
        else Pixels = new Color[textureSize.x * textureSize.z];
    }

    public void SetPixel(int x, int z, Color color)
    {
        Pixels[z * TextureSize.x + x] = color;
    }

    public Color GetPixel(int x, int z)
    {
        return Pixels[z * TextureSize.x + x];
    }

    public void CopyToTexture(Texture2D tex)
    {
        tex.SetPixels(Pixels);
    }
}
