using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

string outputDir = @"D:\Project\Gesturing\gesturing\src\Gesturing\Resources";
Directory.CreateDirectory(outputDir);
string iconPath = Path.Combine(outputDir, "app.ico");

var accent = Color.FromArgb(42, 137, 190);  // #2a89be

// 32x32
using var bmp32 = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
using (var g = Graphics.FromImage(bmp32))
{
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    using var pen = new Pen(accent, 2.5f);
    g.DrawEllipse(pen, 3, 3, 26, 26);

    using var brush = new SolidBrush(accent);
    g.FillPolygon(brush, new PointF[] {
        new(16, 7), new(21, 15), new(17.5f, 15), new(17.5f, 25), new(14.5f, 25), new(14.5f, 15), new(11, 15)
    });
}

// 16x16
using var bmp16 = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
using (var g = Graphics.FromImage(bmp16))
{
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.Clear(Color.Transparent);

    using var pen = new Pen(accent, 1.5f);
    g.DrawEllipse(pen, 1.5f, 1.5f, 13, 13);

    using var brush = new SolidBrush(accent);
    g.FillPolygon(brush, new PointF[] {
        new(8, 3), new(11, 7), new(9.5f, 7), new(9.5f, 12), new(6.5f, 12), new(6.5f, 7), new(4, 7)
    });
}

SaveAsIco(new[] { bmp16, bmp32 }, iconPath);
Console.WriteLine($"Icon saved to: {iconPath}");

static void SaveAsIco(Bitmap[] bitmaps, string path)
{
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms);

    writer.Write((short)0);
    writer.Write((short)1);
    writer.Write((short)bitmaps.Length);

    int offset = 6 + 16 * bitmaps.Length;

    var pngDataList = new List<byte[]>();
    foreach (var bmp in bitmaps)
    {
        using var pngMs = new MemoryStream();
        bmp.Save(pngMs, ImageFormat.Png);
        pngDataList.Add(pngMs.ToArray());
    }

    for (int i = 0; i < bitmaps.Length; i++)
    {
        int w = bitmaps[i].Width;
        int h = bitmaps[i].Height;
        writer.Write((byte)(w >= 256 ? 0 : w));
        writer.Write((byte)(h >= 256 ? 0 : h));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write((int)pngDataList[i].Length);
        writer.Write((int)offset);
        offset += pngDataList[i].Length;
    }

    foreach (var data in pngDataList)
    {
        writer.Write(data);
    }

    File.WriteAllBytes(path, ms.ToArray());
}