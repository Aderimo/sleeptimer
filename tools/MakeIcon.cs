using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// SleepTimer simgesi: koyu yuvarlak kare üzerinde amber hilal.
static class MakeIcon
{
    static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Bitmap Draw(int n)
    {
        var b = new Bitmap(n, n, PixelFormat.Format32bppArgb);
        Color bg = Color.FromArgb(32, 33, 36);
        Color amber = Color.FromArgb(239, 159, 39);
        using (var g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            using (var p = Round(new RectangleF(0, 0, n, n), n * 0.22f))
            using (var br = new SolidBrush(bg))
            {
                g.FillPath(br, p);
                g.SetClip(p);
            }
            float d = n * 0.58f, cx = n * 0.47f, cy = n * 0.52f;
            using (var br = new SolidBrush(amber))
                g.FillEllipse(br, cx - d / 2, cy - d / 2, d, d);
            float d2 = d * 0.84f;
            using (var br = new SolidBrush(bg))
                g.FillEllipse(br, cx - d2 / 2 + d * 0.30f, cy - d2 / 2 - d * 0.20f, d2, d2);
        }
        return b;
    }

    static byte[] Dib(Bitmap b)
    {
        int n = b.Width;
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(40); w.Write(n); w.Write(n * 2); w.Write((short)1); w.Write((short)32);
        w.Write(0); w.Write(0); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
        for (int y = n - 1; y >= 0; y--)
            for (int x = 0; x < n; x++)
            {
                Color c = b.GetPixel(x, y);
                w.Write(c.B); w.Write(c.G); w.Write(c.R); w.Write(c.A);
            }
        int maskRow = ((n + 31) / 32) * 4;
        w.Write(new byte[maskRow * n]);
        return ms.ToArray();
    }

    static int Main(string[] args)
    {
        if (args.Length < 1) { Console.WriteLine("kullanim: MakeIcon cikti.ico [onizleme.png]"); return 1; }
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
        var data = new List<byte[]>();
        foreach (int n in sizes)
        {
            using (Bitmap bmp = Draw(n))
            {
                if (n >= 256)
                {
                    var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    data.Add(ms.ToArray());
                }
                else data.Add(Dib(bmp));
            }
        }
        using (var fs = File.Create(args[0]))
        {
            var w = new BinaryWriter(fs);
            w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                w.Write(dim); w.Write(dim); w.Write((byte)0); w.Write((byte)0);
                w.Write((short)1); w.Write((short)32);
                w.Write(data[i].Length); w.Write(offset);
                offset += data[i].Length;
            }
            foreach (byte[] d in data) w.Write(d);
        }
        if (args.Length > 1)
            using (Bitmap big = Draw(256)) big.Save(args[1], ImageFormat.Png);
        return 0;
    }
}
