    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace ImageEditingApp
    {
        public interface IImageWarp
        {
            string Name { get; }
            Image Apply(Image image, Overhead overhead);
        }
    public class BasicWarp : IImageWarp
    {
        public string Name => "BasicWarp";

        private int Min;
        private int Max;
        private int Batch;
        public BasicWarp(int min = -5, int max = 5, int batch = 10)
        {
            Min = min;
            Max = max;
            Batch = batch;
        }
        public Image Apply(Image image, Overhead overhead)
        {
            Bitmap bmp = new Bitmap(image);
            Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);
            int width = bmp.Width;
            int height = bmp.Height;

            Random rand = new Random();

            // Find the associated ImageObject
            ImageObject currentImageObject = overhead.images.FirstOrDefault(obj => obj.Image == image);
            if (currentImageObject == null)
            {
                Console.WriteLine("No matching ImageObject found in Overhead.");
                return image;
            }

            // Extract features
            var edges = currentImageObject.GetEdges();
            var curvatureData = currentImageObject.GetCurveCurvature();

            // Normalize curvature for processing
            float maxCurvature = curvatureData.Count > 0 ? curvatureData.Max(c => c.Curvature) : 1.0f;

            // Create maps for edges and curvature
            bool[,] edgeMap = new bool[width, height];
            float[,] curvatureMap = new float[width, height];

            foreach (var point in edges)
            {
                if (point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height)
                {
                    edgeMap[point.X, point.Y] = true;
                }
            }

            foreach (var (Point, Curvature) in curvatureData)
            {
                if (Point.X >= 0 && Point.X < width && Point.Y >= 0 && Point.Y < height)
                {
                    curvatureMap[(int)Point.X, (int)Point.Y] = Curvature / maxCurvature;
                }
            }

            // Create a temporary buffer to avoid overwriting pixels during processing
            Bitmap tempBmp = new Bitmap(bmp);

            for (int y = 0; y < height; y += Batch)
            {
                for (int x = 0; x < width; x += Batch)
                {
                    // Determine the average intensity and curvature within the batch
                    float averageCurvature = 0f;
                    float averageColorIntensity = 0f;
                    int pixelsCount = 0;

                    for (int j = 0; j < Batch; j++)
                    {
                        for (int i = 0; i < Batch; i++)
                        {
                            int newX = x + i;
                            int newY = y + j;

                            if (newX < width && newY < height)
                            {
                                // Get pixel color
                                Color pixelColor = bmp.GetPixel(newX, newY);

                                // Calculate color intensity (saturation-based)
                                float colorIntensity = GetColorIntensity(pixelColor);
                                averageColorIntensity += colorIntensity;

                                // Accumulate curvature influence
                                averageCurvature += curvatureMap[newX, newY];
                                pixelsCount++;
                            }
                        }
                    }

                    if (pixelsCount > 0)
                    {
                        averageCurvature /= pixelsCount;
                        averageColorIntensity /= pixelsCount;
                    }

                    // Calculate randomness reduction factor
                    double randomnessFactor = 1.0 - (averageCurvature * 0.5);
                    randomnessFactor *= 1.0 + (averageColorIntensity * 0.8); // Increase distortion for colorful areas

                    // Calculate batch displacement
                    int dx = (int)(rand.Next(Min, Max) * randomnessFactor);
                    int dy = (int)(rand.Next(Min, Max) * randomnessFactor);

                    // Apply distortion to the batch
                    for (int j = 0; j < Batch; j++)
                    {
                        for (int i = 0; i < Batch; i++)
                        {
                            int originalX = x + i;
                            int originalY = y + j;

                            int displacedX = originalX + dx;
                            int displacedY = originalY + dy;

                            if (originalX < width && originalY < height && displacedX < width && displacedY < height &&
                                originalX >= 0 && originalY >= 0 && displacedX >= 0 && displacedY >= 0)
                            {
                                // Get the original and displaced colors
                                Color originalColor = bmp.GetPixel(originalX, originalY);
                                Color displacedColor = tempBmp.GetPixel(displacedX, displacedY);

                                // Blend the colors to prevent gaps
                                Color blendedColor = BlendColors(originalColor, displacedColor);

                                // Set the new pixel on the warped image
                                tempBmp.SetPixel(displacedX, displacedY, blendedColor);
                            }
                        }
                    }
                }
            }

            return tempBmp;
        }

        // Helper function to blend two colors
        // Helper function to calculate color intensity based on saturation
        private float GetColorIntensity(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            float intensity = (max - min); // Higher value means more vibrant color
            return intensity;
        }

        // Helper function to blend two colors
        private Color BlendColors(Color c1, Color c2)
        {
            int r = (c1.R + c2.R) / 2;
            int g = (c1.G + c2.G) / 2;
            int b = (c1.B + c2.B) / 2;
            int a = (c1.A + c2.A) / 2;

            return Color.FromArgb(a, r, g, b);
        }
        public static BasicWarp Configure()
        {
            Form configForm = new Form()
            {
                Text = "Basic Warp Configuration",
                Size = new Size(300, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            Label labelMin = new Label() { Text = "Min Distortion:", Location = new Point(10, 10) };
            TextBox inputBoxMin = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "-5" };

            Label labelMax = new Label() { Text = "Max Distortion:", Location = new Point(10, 40) };
            TextBox inputBoxMax = new TextBox() { Location = new Point(120, 40), Width = 100, Text = "6" };

            Label labelBatch = new Label() { Text = "Batch Size:", Location = new Point(10, 70) };
            TextBox inputBoxBatchSize = new TextBox() { Location = new Point(120, 70), Width = 100, Text = "5" };

            Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 120), Width = 80 };

            BasicWarp configuredWarp = null;

            applyButton.Click += (s, e) =>
            {
                if (int.TryParse(inputBoxMin.Text, out int strengthMin) &&
                    int.TryParse(inputBoxMax.Text, out int strengthMax) &&
                    int.TryParse(inputBoxBatchSize.Text, out int batchSize))
                {
                    configuredWarp = new BasicWarp(strengthMin, strengthMax, batchSize);
                    configForm.Close();
                }
                else
                {
                    MessageBox.Show("Invalid input. Please enter numeric values for all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            configForm.Controls.Add(labelMin);
            configForm.Controls.Add(inputBoxMin);
            configForm.Controls.Add(labelMax);
            configForm.Controls.Add(inputBoxMax);
            configForm.Controls.Add(labelBatch);
            configForm.Controls.Add(inputBoxBatchSize);
            configForm.Controls.Add(applyButton);
            configForm.ShowDialog();

            return configuredWarp;
        }
    }
        public class AverageColorWarp : IImageWarp
        {
            public string Name => "AverageColorWarp";


            private int brightnessD;
            public AverageColorWarp(int brightness = 32)
            {
                brightnessD = brightness;
            }
            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);

                for (int y = 1; y < bmp.Height - 1; y++)
                {
                    for (int x = 1; x < bmp.Width - 1; x++)
                    {
                        // Calculate the average brightness of the surrounding pixels
                        int avgR = 0, avgG = 0, avgB = 0, count = 0;

                        for (int j = -1; j <= 1; j++)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                if (i == 0 && j == 0) continue;
                                Color neighborColor = bmp.GetPixel(x + i, y + j);
                                avgR += neighborColor.R;
                                avgG += neighborColor.G;
                                avgB += neighborColor.B;
                                count++;
                            }
                        }

                        avgR /= count;
                        avgG /= count;
                        avgB /= count;
                        int brightness = (avgR + avgG + avgB) / 3;

                        // Map brightness to displacement
                        int displacement = (brightness / brightnessD) - 4; // Range: -4 to +4
                        int newX = Math.Clamp(x + displacement, 0, bmp.Width - 1);
                        int newY = Math.Clamp(y + displacement, 0, bmp.Height - 1);

                        warpedBmp.SetPixel(newX, newY, bmp.GetPixel(x, y));
                    }
                }

                return warpedBmp;
            }
            public static AverageColorWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Average Color Warp",
                    Size = new Size(300, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label label = new Label() { Text = "Average Color Strength:", Location = new Point(10, 10) };
                TextBox inputBox = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "32" };
                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 50), Width = 80 };

                AverageColorWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (int.TryParse(inputBox.Text, out int strength))
                    {
                        configuredWarp = new AverageColorWarp(strength);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter a numeric value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(label);
                configForm.Controls.Add(inputBox);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class SwirlWarp : IImageWarp
        {
            public string Name => "SwirlWarp";

            private double swirlStrength;

            public SwirlWarp(double strength = 0.05)
            {
                swirlStrength = strength;
            }

            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);
                double centerX = bmp.Width / 2.0;
                double centerY = bmp.Height / 2.0;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        double dx = x - centerX;
                        double dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        double angle = swirlStrength * distance;
                        double sinAngle = Math.Sin(angle);
                        double cosAngle = Math.Cos(angle);

                        int newX = (int)(cosAngle * dx - sinAngle * dy + centerX);
                        int newY = (int)(sinAngle * dx + cosAngle * dy + centerY);

                        if (newX >= 0 && newX < bmp.Width && newY >= 0 && newY < bmp.Height)
                        {
                            warpedBmp.SetPixel(x, y, bmp.GetPixel(newX, newY));
                        }
                    }
                }

                return warpedBmp;
            }


            public static SwirlWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Swirl Warp",
                    Size = new Size(300, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label label = new Label() { Text = "Swirl Strength:", Location = new Point(10, 10) };
                TextBox inputBox = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "0.05" };
                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 50), Width = 80 };

                SwirlWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (double.TryParse(inputBox.Text, out double strength))
                    {
                        configuredWarp = new SwirlWarp(strength);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter a numeric value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(label);
                configForm.Controls.Add(inputBox);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }

        public class WaveWarp : IImageWarp
        {
            public string Name => "WaveWarp";

            private double waveFrequency;
            private double waveAmplitude;

            public WaveWarp(double frequency = 0.1, double amplitude = 10)
            {
                waveFrequency = frequency;
                waveAmplitude = amplitude;
            }

            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int newX = x + (int)(waveAmplitude * Math.Sin(y * waveFrequency));
                        if (newX >= 0 && newX < bmp.Width)
                        {
                            warpedBmp.SetPixel(x, y, bmp.GetPixel(newX, y));
                        }
                    }
                }
                return warpedBmp;
            }

            public static WaveWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Wave Warp",
                    Size = new Size(300, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label freqLabel = new Label() { Text = "Wave Frequency:", Location = new Point(10, 10) };
                TextBox freqInputBox = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "0.1" };

                Label ampLabel = new Label() { Text = "Wave Amplitude:", Location = new Point(10, 40) };
                TextBox ampInputBox = new TextBox() { Location = new Point(120, 40), Width = 100, Text = "10" };

                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 80), Width = 80 };

                WaveWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (double.TryParse(freqInputBox.Text, out double frequency) && double.TryParse(ampInputBox.Text, out double amplitude))
                    {
                        configuredWarp = new WaveWarp(frequency, amplitude);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter numeric values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(freqLabel);
                configForm.Controls.Add(freqInputBox);
                configForm.Controls.Add(ampLabel);
                configForm.Controls.Add(ampInputBox);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class RectangleWarp : IImageWarp
        {
            public string Name => "RectangleWarp";

            private int gridSize;
            private int displacement;

            public RectangleWarp(int gridSize = 20, int displacement = 5)
            {
                this.gridSize = gridSize;
                this.displacement = displacement;
            }

            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);
                Random rand = new Random();

                using (Graphics g = Graphics.FromImage(warpedBmp))
                {
                    g.Clear(Color.Black);

                    for (int y = 0; y < bmp.Height; y += gridSize)
                    {
                        for (int x = 0; x < bmp.Width; x += gridSize)
                        {
                            int dx = rand.Next(-displacement, displacement);
                            int dy = rand.Next(-displacement, displacement);

                            int srcX = Math.Clamp(x + dx, 0, bmp.Width - gridSize);
                            int srcY = Math.Clamp(y + dy, 0, bmp.Height - gridSize);

                            Rectangle srcRect = new Rectangle(srcX, srcY, gridSize, gridSize);
                            Rectangle destRect = new Rectangle(x, y, gridSize, gridSize);

                            g.DrawImage(bmp, destRect, srcRect, GraphicsUnit.Pixel);
                        }
                    }
                }

                return warpedBmp;
            }

            public static RectangleWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Rectangle Warp",
                    Size = new Size(300, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label gridLabel = new Label() { Text = "Grid Size:", Location = new Point(10, 10) };
                TextBox gridInput = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "20" };

                Label displacementLabel = new Label() { Text = "Displacement:", Location = new Point(10, 50) };
                TextBox displacementInput = new TextBox() { Location = new Point(120, 50), Width = 100, Text = "5" };

                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 100), Width = 80 };

                RectangleWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (int.TryParse(gridInput.Text, out int gridSize) && int.TryParse(displacementInput.Text, out int displacement))
                    {
                        configuredWarp = new RectangleWarp(gridSize, displacement);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter numeric values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(gridLabel);
                configForm.Controls.Add(gridInput);
                configForm.Controls.Add(displacementLabel);
                configForm.Controls.Add(displacementInput);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class TriangleWarp : IImageWarp
        {
            public string Name => "TriangleWarp";

            private int triangleSize;
            private int displacement;

            public TriangleWarp(int triangleSize = 20, int displacement = 5)
            {
                this.triangleSize = triangleSize;
                this.displacement = displacement;
            }

            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);
                Random rand = new Random();

                using (Graphics g = Graphics.FromImage(warpedBmp))
                {
                    g.Clear(Color.Black);

                    for (int y = 0; y < bmp.Height; y += triangleSize)
                    {
                        for (int x = 0; x < bmp.Width; x += triangleSize)
                        {
                            // Random displacement but staying in bounds
                            int dx = rand.Next(-displacement, displacement);
                            int dy = rand.Next(-displacement, displacement);

                            int srcX = Math.Clamp(x + dx, 0, bmp.Width - 1);
                            int srcY = Math.Clamp(y + dy, 0, bmp.Height - 1);

                            int rightX = Math.Clamp(srcX + triangleSize, 0, bmp.Width - 1);
                            int bottomY = Math.Clamp(srcY + triangleSize, 0, bmp.Height - 1);
                            int centerX = Math.Clamp(srcX + triangleSize / 2, 0, bmp.Width - 1);
                            int centerY = Math.Clamp(srcY + triangleSize / 2, 0, bmp.Height - 1);

                            // **Fix: Ensure color sampling prevents artifacts**
                            Color colorUpper = GetTriangleColor(bmp, srcX, srcY, rightX, srcY, centerX, centerY);
                            Color colorLower = GetTriangleColor(bmp, srcX, bottomY, rightX, bottomY, centerX, centerY);

                            // **Fix: Make sure triangles share exact edges, no gaps**
                            Point[] upperTriangle = new Point[]
                            {
                                new Point(x, y),
                                new Point(x + triangleSize, y),
                                new Point(x + triangleSize / 2, y + triangleSize)
                            };

                            Point[] lowerTriangle = new Point[]
                            {
                                new Point(x + triangleSize, y + triangleSize),
                                new Point(x + triangleSize, y + triangleSize),
                                new Point(x + triangleSize / 2, y)
                            };

                            using (SolidBrush brush1 = new SolidBrush(colorUpper))
                            {
                                g.FillPolygon(brush1, upperTriangle);
                            }

                            using (SolidBrush brush2 = new SolidBrush(colorLower))
                            {
                                g.FillPolygon(brush2, lowerTriangle);
                            }
                        }
                    }
                }

                return warpedBmp;
            }

            /// <summary>
            /// Improved color sampling to eliminate gaps and artifacts.
            /// Uses multiple pixels inside the triangle to get a good average color.
            /// </summary>
            private Color GetTriangleColor(Bitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3)
            {
                int totalR = 0, totalG = 0, totalB = 0, count = 0;

                // Compute the centroid of the triangle
                int cx = (x1 + x2 + x3) / 3;
                int cy = (y1 + y2 + y3) / 3;

                // Offsets to sample different points inside the triangle
                int[][] offsets =
                {
                    new int[] { 0, 0 },
                    new int[] { 1, 1 }, new int[] { -1, -1 },
                    new int[] { 2, -2 }, new int[] { -2, 2 },
                    new int[] { 3, 0 }, new int[] { 0, 3 },
                    new int[] { -3, 0 }, new int[] { 0, -3 }
                };

                foreach (int[] offset in offsets) // Corrected iteration
                {
                    int px = Math.Clamp(cx + offset[0], 0, bmp.Width - 1);
                    int py = Math.Clamp(cy + offset[1], 0, bmp.Height - 1);
                    Color pixel = bmp.GetPixel(px, py);

                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    count++;
                }

                return Color.FromArgb(totalR / count, totalG / count, totalB / count);
            }



            public static TriangleWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Triangle Warp",
                    Size = new Size(300, 200),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label sizeLabel = new Label() { Text = "Triangle Size:", Location = new Point(10, 10) };
                TextBox sizeInput = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "20" };

                Label displacementLabel = new Label() { Text = "Displacement:", Location = new Point(10, 50) };
                TextBox displacementInput = new TextBox() { Location = new Point(120, 50), Width = 100, Text = "5" };

                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 100), Width = 80 };

                TriangleWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (int.TryParse(sizeInput.Text, out int triangleSize) && int.TryParse(displacementInput.Text, out int displacement))
                    {
                        configuredWarp = new TriangleWarp(triangleSize, displacement);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter numeric values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(sizeLabel);
                configForm.Controls.Add(sizeInput);
                configForm.Controls.Add(displacementLabel);
                configForm.Controls.Add(displacementInput);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class PerspectiveWarp : IImageWarp
        {
            public string Name => "PerspectiveWarp";

            private double perspectiveFactor;
            private double perspectiveAngle;
            private double centerXOffset;
            private double centerYOffset;

            public PerspectiveWarp(double factor = 0.5, double angle = 0.0, double centerX = 0.5, double centerY = 0.5)
            {
                perspectiveFactor = factor;
                perspectiveAngle = angle * Math.PI / 180; // Convert to radians
                centerXOffset = centerX;
                centerYOffset = centerY;
            }

            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap sourceBmp = new Bitmap(image);
                Bitmap destBmp = new Bitmap(sourceBmp.Width, sourceBmp.Height);

                int width = sourceBmp.Width;
                int height = sourceBmp.Height;
                double centerX = width * centerXOffset;
                double centerY = height * centerYOffset;

                using (Graphics g = Graphics.FromImage(destBmp))
                {
                    g.Clear(Color.Transparent); // Keep background fully transparent
                }

                for (int y = 0; y < height; y++)
                {
                    // Compute the perspective scale factor based on vanishing point
                    double yFactor = (y - centerY) / height;
                    double scaleFactor = 1.0 / (1.0 + perspectiveFactor * yFactor);

                    for (int x = 0; x < width; x++)
                    {
                        // Apply transformation with perspective angle
                        double rotatedX = (x - centerX) * scaleFactor + centerX;
                        double rotatedY = y * scaleFactor;

                        // Get interpolated color while preserving transparency
                        Color interpolatedColor = GetBilinearInterpolatedColor(sourceBmp, rotatedX, rotatedY);

                        // Ensure transparency is retained correctly
                        if (interpolatedColor.A > 10) // Ignore nearly transparent pixels
                        {
                            destBmp.SetPixel(x, y, interpolatedColor);
                        }
                    }
                }

                return destBmp;
            }

            private Color GetBilinearInterpolatedColor(Bitmap bmp, double x, double y)
            {
                // Ensure coordinates are within valid range
                if (x < 0 || x >= bmp.Width - 1 || y < 0 || y >= bmp.Height - 1)
                    return Color.Transparent;

                int x1 = (int)Math.Floor(x);
                int x2 = Math.Min(x1 + 1, bmp.Width - 1);
                int y1 = (int)Math.Floor(y);
                int y2 = Math.Min(y1 + 1, bmp.Height - 1);

                Color c11 = bmp.GetPixel(x1, y1);
                Color c12 = bmp.GetPixel(x1, y2);
                Color c21 = bmp.GetPixel(x2, y1);
                Color c22 = bmp.GetPixel(x2, y2);

                double dx = x - x1;
                double dy = y - y1;

                // Alpha blending to avoid transparency issues
                int a = (int)(c11.A * (1 - dx) * (1 - dy) + c21.A * dx * (1 - dy) +
                              c12.A * (1 - dx) * dy + c22.A * dx * dy);

                if (a < 10) return Color.Transparent; // Avoid unwanted transparency blending

                int r = (int)(c11.R * (1 - dx) * (1 - dy) + c21.R * dx * (1 - dy) +
                              c12.R * (1 - dx) * dy + c22.R * dx * dy);

                int g = (int)(c11.G * (1 - dx) * (1 - dy) + c21.G * dx * (1 - dy) +
                              c12.G * (1 - dx) * dy + c22.G * dx * dy);

                int b = (int)(c11.B * (1 - dx) * (1 - dy) + c21.B * dx * (1 - dy) +
                              c12.B * (1 - dx) * dy + c22.B * dx * dy);

                return Color.FromArgb(a, r, g, b);
            }

            public static PerspectiveWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Perspective Warp",
                    Size = new Size(400, 250),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label factorLabel = new Label() { Text = "Perspective Factor:", Location = new Point(10, 10) };
                TextBox factorInputBox = new TextBox() { Location = new Point(180, 10), Width = 100, Text = "0.5" };

                Label angleLabel = new Label() { Text = "Perspective Angle:", Location = new Point(10, 40) };
                TextBox angleInputBox = new TextBox() { Location = new Point(180, 40), Width = 100, Text = "0.0" };

                Label centerXLabel = new Label() { Text = "Center X (0-1):", Location = new Point(10, 70) };
                TextBox centerXInputBox = new TextBox() { Location = new Point(180, 70), Width = 100, Text = "0.5" };

                Label centerYLabel = new Label() { Text = "Center Y (0-1):", Location = new Point(10, 100) };
                TextBox centerYInputBox = new TextBox() { Location = new Point(180, 100), Width = 100, Text = "0.5" };

                Button applyButton = new Button() { Text = "Apply", Location = new Point(150, 140), Width = 80 };

                PerspectiveWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (double.TryParse(factorInputBox.Text, out double factor) &&
                        double.TryParse(angleInputBox.Text, out double angle) &&
                        double.TryParse(centerXInputBox.Text, out double centerX) &&
                        double.TryParse(centerYInputBox.Text, out double centerY))
                    {
                        configuredWarp = new PerspectiveWarp(factor, angle, centerX, centerY);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter numeric values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(factorLabel);
                configForm.Controls.Add(factorInputBox);
                configForm.Controls.Add(angleLabel);
                configForm.Controls.Add(angleInputBox);
                configForm.Controls.Add(centerXLabel);
                configForm.Controls.Add(centerXInputBox);
                configForm.Controls.Add(centerYLabel);
                configForm.Controls.Add(centerYInputBox);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class DistortWarp : IImageWarp
        {
            public string Name => "DistortWarp";

            private double distortStrength;
            private int k = 10;
            private int iterations = 10; 
            public DistortWarp(double strength = 0.05)
            {
                distortStrength = strength;
            }


            public Image Apply(Image image, Overhead overhead)
            {
                Bitmap bmp = new Bitmap(image);
                Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);
                int width = bmp.Width;
                int height = bmp.Height;

                // Initialize random displacement
                Random rand = new Random();
                int maxDisplacement = (int)(distortStrength * Math.Max(width, height));

                // Find the associated ImageObject
                ImageObject currentImageObject = overhead.images.FirstOrDefault(obj => obj.Image == image);
                if (currentImageObject == null)
                {
                    Console.WriteLine("No matching ImageObject found in Overhead.");
                    return image;
                }

                // Extract features
                var edges = currentImageObject.GetEdges();
                var curvatureData = currentImageObject.GetCurveCurvature();

                // Normalize curvature for processing
                float maxCurvature = curvatureData.Count > 0 ? curvatureData.Max(c => c.Curvature) : 1.0f;

                // Pre-calculate curvature influence map for quick access
                float[,] curvatureMap = new float[width, height];
                foreach (var (Point, Curvature) in curvatureData)
                {
                    if (Point.X >= 0 && Point.X < width && Point.Y >= 0 && Point.Y < height)
                    {
                        curvatureMap[(int)Point.X, (int)Point.Y] = Curvature / maxCurvature;
                    }
                }

                // Pre-calculate edge map for quick access
                bool[,] edgeMap = new bool[width, height];
                foreach (var point in edges)
                {
                    if (point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height)
                    {
                        edgeMap[point.X, point.Y] = true;
                    }
                }

                // Calculate pixel density map
                double[,] densityMap = new double[width, height];
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        double intensity = (c.R + c.G + c.B) / 3.0 / 255.0;
                        densityMap[x, y] = intensity;
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Get the curvature influence at this point
                        float curvatureInfluence = curvatureMap[x, y];

                        // Get the intensity-based displacement strength
                        double intensityFactor = densityMap[x, y] + 0.1;

                        // Generate random displacements
                        int dx = (int)((rand.NextDouble() - 0.5) * 2 * maxDisplacement * intensityFactor * (1 - curvatureInfluence));
                        int dy = (int)((rand.NextDouble() - 0.5) * 2 * maxDisplacement * intensityFactor * (1 - curvatureInfluence));

                        // Reduce displacement if near an edge
                        if (edgeMap[x, y])
                        {
                            dx /= 2;
                            dy /= 2;
                        }

                        int newX = x + dx;
                        int newY = y + dy;

                        // Ensure the new coordinates are within bounds
                        if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                        {
                            warpedBmp.SetPixel(newX, newY, bmp.GetPixel(x, y));
                        }
                    }
                }

                return warpedBmp;
            }


            public static DistortWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Distort Warp",
                    Size = new Size(300, 150),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label label = new Label() { Text = "Distort Strength:", Location = new Point(10, 10) };
                TextBox inputBox = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "0.05" };
                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 50), Width = 80 };

                DistortWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (double.TryParse(inputBox.Text, out double strength))
                    {
                        configuredWarp = new DistortWarp(strength);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter a numeric value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(label);
                configForm.Controls.Add(inputBox);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
        public class FaceMorphWarp : IImageWarp
        {
            public string Name => "FaceMorphWarp";

            private double strength;
            private int featureSize;

            public FaceMorphWarp(double strength = 0.5, int featureSize = 20)
            {
                this.strength = strength;
                this.featureSize = featureSize;
            }

        public Image Apply(Image image, Overhead overhead)
        {
            Bitmap bmp = new Bitmap(image);
            Bitmap warpedBmp = new Bitmap(bmp.Width, bmp.Height);

            using (Graphics g = Graphics.FromImage(warpedBmp))
            {
                g.DrawImage(bmp, 0, 0);
            }

            Random rand = new Random();
            int featureSize = 10; // Size of the area to warp
            float strength = 0.5f; // How strong the warp is

            // Find the corresponding ImageObject in Overhead
            ImageObject currentImageObject = overhead.images.FirstOrDefault(obj => obj.Image == image);

            if (currentImageObject == null)
            {
                Console.WriteLine("No matching ImageObject found in Overhead.");
                return image;
            }

            // Extract Features from ImageObject
            var histogram = currentImageObject.GetHistogram();
            var edges = currentImageObject.GetEdges();
            var curvatureData = currentImageObject.GetCurveCurvature();

            // Normalize curvature for decision-making
            float maxCurvature = curvatureData.Max(c => c.Curvature);
            if (maxCurvature == 0) maxCurvature = 1;  // Avoid division by zero

            for (int y = 0; y < bmp.Height; y += featureSize)
            {
                for (int x = 0; x < bmp.Width; x += featureSize)
                {
                    // Check if the current point is near an edge
                    bool isNearEdge = edges.Any(point =>
                        Math.Abs(point.X - x) < featureSize && Math.Abs(point.Y - y) < featureSize);

                    // Calculate curvature influence (0 - low, 1 - high)
                    float curvatureInfluence = 0f;

                    foreach (var (Point, Curvature) in curvatureData)
                    {
                        if (Math.Abs(Point.X - x) < featureSize && Math.Abs(Point.Y - y) < featureSize)
                        {
                            curvatureInfluence = Curvature / maxCurvature;
                            break;
                        }
                    }

                    // Define how much to move based on feature detection
                    int dx = rand.Next((int)(-featureSize * strength * (1 - curvatureInfluence)),
                                       (int)(featureSize * strength * (1 - curvatureInfluence)));

                    int dy = rand.Next((int)(-featureSize * strength * (1 - curvatureInfluence)),
                                       (int)(featureSize * strength * (1 - curvatureInfluence)));

                    if (isNearEdge)
                    {
                        // If it's near an edge, limit the distortion strength
                        dx = (int)(dx * 0.5);
                        dy = (int)(dy * 0.5);
                    }

                    // Apply pixel displacement if within boundaries
                    if (x + dx >= 0 && x + dx < bmp.Width && y + dy >= 0 && y + dy < bmp.Height)
                    {
                        for (int i = 0; i < featureSize; i++)
                        {
                            for (int j = 0; j < featureSize; j++)
                            {
                                if (x + i < bmp.Width && y + j < bmp.Height &&
                                    x + dx + i < bmp.Width && y + dy + j < bmp.Height)
                                {
                                    warpedBmp.SetPixel(x + i, y + j, bmp.GetPixel(x + dx + i, y + dy + j));
                                }
                            }
                        }
                    }
                }
            }

            return warpedBmp;
        }

        public static FaceMorphWarp Configure()
            {
                Form configForm = new Form()
                {
                    Text = "Configure Face Morph Warp",
                    Size = new Size(300, 180),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label strengthLabel = new Label() { Text = "Strength (0-1):", Location = new Point(10, 10) };
                TextBox strengthInput = new TextBox() { Location = new Point(120, 10), Width = 100, Text = "0.5" };

                Label featureLabel = new Label() { Text = "Feature Size:", Location = new Point(10, 50) };
                TextBox featureInput = new TextBox() { Location = new Point(120, 50), Width = 100, Text = "20" };

                Button applyButton = new Button() { Text = "Apply", Location = new Point(100, 100), Width = 80 };

                FaceMorphWarp configuredWarp = null;
                applyButton.Click += (s, e) =>
                {
                    if (double.TryParse(strengthInput.Text, out double strength) && int.TryParse(featureInput.Text, out int size))
                    {
                        configuredWarp = new FaceMorphWarp(strength, size);
                        configForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter numeric values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                configForm.Controls.Add(strengthLabel);
                configForm.Controls.Add(strengthInput);
                configForm.Controls.Add(featureLabel);
                configForm.Controls.Add(featureInput);
                configForm.Controls.Add(applyButton);
                configForm.ShowDialog();

                return configuredWarp;
            }
        }
    }
