using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditingApp
{

    public class Overhead
    {
        public Overhead() { }
        public List<ImageObject> images = new List<ImageObject>(); // Store multiple images
    }
    public class ImageObject
    {
        public Image Image { get; set; }
        public Point Position { get; set; }
        public float Scale { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;

        public Rectangle GetBoundingBox()
        {
            return new Rectangle(Position.X, Position.Y, (int)(Image.Width * Scale), (int)(Image.Height * Scale));
        }

        // 1. Get Histogram
        public Dictionary<int, int> GetHistogram()
        {
            Bitmap bmp = new Bitmap(Image);
            Dictionary<int, int> histogram = new Dictionary<int, int>();

            for (int i = 0; i <= 255; i++)
                histogram[i] = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    int grayValue = (int)(0.3 * pixelColor.R + 0.59 * pixelColor.G + 0.11 * pixelColor.B);
                    histogram[grayValue]++;
                }
            }

            return histogram;
        }

        // 2. Get Edges (Canny Edge Detection)
        public List<Point> GetEdges()
        {
            Bitmap bmp = new Bitmap(Image);
            List<Point> edges = new List<Point>();

            // Convert to grayscale
            Bitmap grayBmp = new Bitmap(bmp.Width, bmp.Height);
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    int grayValue = (int)(0.3 * pixelColor.R + 0.59 * pixelColor.G + 0.11 * pixelColor.B);
                    grayBmp.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }

            // Simple edge detection using Sobel filter
            int[,] gx = new int[,]
            {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };
            int[,] gy = new int[,]
            {
                { -1, -2, -1 },
                { 0, 0, 0 },
                { 1, 2, 1 }
            };

            for (int x = 1; x < grayBmp.Width - 1; x++)
            {
                for (int y = 1; y < grayBmp.Height - 1; y++)
                {
                    int gradX = 0;
                    int gradY = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            Color pixel = grayBmp.GetPixel(x + i, y + j);
                            int intensity = pixel.R; // Grayscale, so R = G = B
                            gradX += intensity * gx[i + 1, j + 1];
                            gradY += intensity * gy[i + 1, j + 1];
                        }
                    }

                    int gradientMagnitude = (int)Math.Sqrt(gradX * gradX + gradY * gradY);

                    if (gradientMagnitude > 128) // Edge threshold
                    {
                        edges.Add(new Point(x, y));
                    }
                }
            }

            return edges;
        }

        // 3. Get Curve Curvature
        public List<(PointF Point, float Curvature)> GetCurveCurvature()
        {
            List<(PointF, float)> curvaturePoints = new List<(PointF, float)>();

            var edges = GetEdges(); // Get edge points from the previous function

            for (int i = 1; i < edges.Count - 1; i++)
            {
                Point p1 = edges[i - 1];
                Point p2 = edges[i];
                Point p3 = edges[i + 1];

                float dx1 = p2.X - p1.X;
                float dy1 = p2.Y - p1.Y;
                float dx2 = p3.X - p2.X;
                float dy2 = p3.Y - p2.Y;

                float curvature = Math.Abs((dx1 * dy2 - dy1 * dx2)) / (float)Math.Pow(dx1 * dx1 + dy1 * dy1, 1.5f);

                curvaturePoints.Add((new PointF(p2.X, p2.Y), curvature));
            }

            return curvaturePoints;
        }
    }
}

