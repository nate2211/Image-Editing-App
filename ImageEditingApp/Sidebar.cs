using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace ImageEditingApp
{
    public class Sidebar : Panel
    {
        private ImageEditor editor;
        private Button selectButton;
        private bool isSelecting = false;
        private Button floodFillButton;
        private Button magicWandButton;
        public SelectionMode currentSelectionMode = SelectionMode.None;

        public enum SelectionMode
        {
            None,
            FloodFill,
            MagicWand
        }

        public Sidebar(ImageEditor imageEditor)
        {
            this.editor = imageEditor;
            this.Width = 200;
            this.Dock = DockStyle.Right;
            this.BackColor = Color.LightGray;

            selectButton = new Button()
            {
                Text = "Select Tool",
                Location = new Point(20, 20),
                Size = new Size(150, 30)
            };
            selectButton.Click += (s,e) =>  SetSelectionMode(SelectionMode.None);

            floodFillButton = new Button()
            {
                Text = "Flood Fill",
                Location = new Point(20, 60),
                Size = new Size(150, 30)
            };
            floodFillButton.Click += (s, e) => SetSelectionMode(SelectionMode.FloodFill);

            magicWandButton = new Button()
            {
                Text = "Magic Wand",
                Location = new Point(20, 100),
                Size = new Size(150, 30)
            };
            magicWandButton.Click += (s, e) => SetSelectionMode(SelectionMode.MagicWand);

            this.Controls.Add(selectButton);
            this.Controls.Add(floodFillButton);
            this.Controls.Add(magicWandButton);
        }

        private void SetSelectionMode(SelectionMode mode)
        {
            currentSelectionMode = mode;
            editor.SetSelectionMode(mode);
        }

        public HashSet<Point> FloodFillSelection(Bitmap bmp, int x, int y, Color targetColor, int tolerance)
        {
            HashSet<Point> selection = new HashSet<Point>();
            Queue<Point> pixels = new Queue<Point>();
            pixels.Enqueue(new Point(x, y));

            while (pixels.Count > 0)
            {
                Point p = pixels.Dequeue();
                if (p.X < 0 || p.Y < 0 || p.X >= bmp.Width || p.Y >= bmp.Height || selection.Contains(p))
                    continue;

                Color currentColor = bmp.GetPixel(p.X, p.Y);
                if (ColorWithinTolerance(currentColor, targetColor, tolerance))
                {
                    selection.Add(p);
                    pixels.Enqueue(new Point(p.X + 1, p.Y));
                    pixels.Enqueue(new Point(p.X - 1, p.Y));
                    pixels.Enqueue(new Point(p.X, p.Y + 1));
                    pixels.Enqueue(new Point(p.X, p.Y - 1));
                }
            }
            return selection;
        }
        public HashSet<Point> MagicWandSelection(Bitmap bmp, int x, int y, Color targetColor, int tolerance)
        {
            HashSet<Point> selection = new HashSet<Point>();

            // Scan entire image to find all pixels within tolerance
            for (int py = 0; py < bmp.Height; py++)
            {
                for (int px = 0; px < bmp.Width; px++)
                {
                    Color currentColor = bmp.GetPixel(px, py);

                    if (ColorWithinTolerance(currentColor, targetColor, tolerance))
                    {
                        selection.Add(new Point(px, py)); // Add all matching pixels
                    }
                }
            }
            return selection;
        }
        private Rectangle GetSelectionBounds()
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (Point p in editor.selectedPixels)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        public void ConvertSelectionToImageFromCopy()
        {
            if (editor.selectedImage == null || editor.selectedPixels.Count == 0) return;

            Bitmap bmp = new Bitmap(editor.selectedImage.Image);
            Rectangle bounds = GetSelectionBounds();
            Bitmap newImage = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(newImage))
            {
                foreach (Point p in editor.selectedPixels)
                {
                    if (p.X >= 0 && p.Y >= 0 && p.X < bmp.Width && p.Y < bmp.Height)
                    {
                        Color color = bmp.GetPixel(p.X, p.Y);
                        newImage.SetPixel(p.X - bounds.X, p.Y - bounds.Y, color);
                    }
                }
            }

            ImageObject newImgObj = new ImageObject()
            {
                Image = newImage,
                Position = new Point(bounds.X, bounds.Y)
            };

            editor.images.Add(newImgObj);
            editor.selectedPixels.Clear(); // Clear selection after conversion
            editor.canvas.Invalidate();
        }
        public ImageObject ConvertSelectedPixelsToImage()
        {
            if (editor.selectedImage == null || editor.selectedPixels.Count == 0)
                return null;

            Bitmap sourceBitmap = new Bitmap(editor.selectedImage.Image);
            Rectangle bounds = GetSelectionBounds();

            // Create a new bitmap with the size of the selection bounds
            Bitmap selectedImage = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(selectedImage))
            {
                foreach (Point p in editor.selectedPixels)
                {
                    if (p.X >= 0 && p.Y >= 0 && p.X < sourceBitmap.Width && p.Y < sourceBitmap.Height)
                    {
                        Color color = sourceBitmap.GetPixel(p.X, p.Y);
                        selectedImage.SetPixel(p.X - bounds.X, p.Y - bounds.Y, color);
                    }
                }
            }

            ImageObject newImgObj = new ImageObject()
            {
                Image = selectedImage,
                Position = new Point(bounds.X, bounds.Y)
            };
            editor.images.Add(newImgObj);

            return newImgObj;
        }
        public void ConvertSelectionToImageFromPaste()
        {
            if (editor.selectedImage == null || editor.selectedPixels.Count == 0) return;

            Bitmap bmp = new Bitmap(editor.selectedImage.Image);
            Rectangle bounds = GetSelectionBounds();
            Bitmap newImage = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics gNew = Graphics.FromImage(newImage))
            using (Graphics gOriginal = Graphics.FromImage(bmp))
            {
                foreach (Point p in editor.selectedPixels)
                {
                    if (p.X >= 0 && p.Y >= 0 && p.X < bmp.Width && p.Y < bmp.Height)
                    {
                        Color color = bmp.GetPixel(p.X, p.Y);
                        newImage.SetPixel(p.X - bounds.X, p.Y - bounds.Y, color);

                        // Remove selected area from original image (set as transparent)
                        bmp.SetPixel(p.X, p.Y, Color.Transparent);
                    }
                }
            }

            // Save the modified original image
            editor.selectedImage.Image = bmp;

            // Add the cut selection as a new image object
            ImageObject newImgObj = new ImageObject()
            {
                Image = newImage,
                Position = new Point(bounds.X, bounds.Y)
            };

            editor.images.Add(newImgObj);
            editor.selectedPixels.Clear(); // Clear selection after conversion
            editor.canvas.Invalidate();
        }

        private bool ColorWithinTolerance(Color c1, Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) <= tolerance &&
                   Math.Abs(c1.G - c2.G) <= tolerance &&
                   Math.Abs(c1.B - c2.B) <= tolerance;
        }

    }
}
