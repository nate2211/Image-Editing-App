using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditingApp
{
    public interface IImageFilter
    {
        string Name { get; }
        Image Apply(Image image);
    }

    public class GrayscaleFilter : IImageFilter
    {
        public string Name => "Grayscale";
        public Image Apply(Image image)
        {
            Bitmap bmp = new Bitmap(image);
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
            return bmp;
        }
    }

}
