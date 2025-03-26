using ImageEditingApp.ImageEditingApp;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using static ImageEditingApp.Sidebar;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;
using Image = System.Drawing.Image;
using ImageMagick;



namespace ImageEditingApp
{
    public class ImageEditor : Form
    {
        public PictureBox canvas;
        public ImageObject selectedImage;
        private Point previousMousePos;
        public Sidebar.SelectionMode selectionMode;
        private bool isDragging = false;
        private bool isScaling = false;
        private bool isSelecting = false;
        private bool isRotating = false;
        public Overhead overhead = new Overhead(); // Store multiple images
        private List<IImageFilter> filters = new List<IImageFilter> { new GrayscaleFilter() };

        private Point selectionStart;
        private Point selectionEnd;

        public HashSet<Point> selectedPixels = new HashSet<Point>(); // Store selected pixels
        public int selectionTolerance = 50; // Tolerance for Magic Wand
        private bool shiftHeld = false;
        private Sidebar sidebar;
        private RenderLoop renderLoop;
        private bool isDrawingRectangle = false;
        public ImageEditor()
        {
            this.Text = "Image Editor";
            this.Size = new Size(2200, 2200);
            this.KeyPreview = true; // ✅ Ensures key events are captured at the form level
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem addMenu = new ToolStripMenuItem("Add Image");
            ToolStripMenuItem filterMenu = new ToolStripMenuItem("Add Filter");
            ToolStripMenuItem warpMenu = new ToolStripMenuItem("Add Warp");
            ToolStripMenuItem exportMenu = new ToolStripMenuItem("Export Image");
            // Add Export Selected Object Menu
            ToolStripMenuItem exportSelectedMenu = new ToolStripMenuItem("Export Selected Object");
            exportSelectedMenu.Click += ExportSelectedObject;
            fileMenu.DropDownItems.Add(exportSelectedMenu);
            addMenu.Click += AddImage;
            filterMenu.Click += FilterImage;
            warpMenu.Click += WarpImage;
            exportMenu.Click += ExportImage;
            
            fileMenu.DropDownItems.Add(addMenu);
            fileMenu.DropDownItems.Add(filterMenu);
            fileMenu.DropDownItems.Add(warpMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exportMenu);
            menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);


            // Sidebar Panel
            sidebar = new Sidebar(this, overhead);
            this.Controls.Add(sidebar);

            canvas = new PictureBox()
            {
                BackColor = Color.White,
                Size = new Size(2000, 2000),
                Location = new Point(10, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            this.Controls.Add(canvas);
            // Initialize RenderLoop
            this.Load += (sender, e) => // Ensure the form is fully created
            {
                renderLoop = new RenderLoop(this, canvas);
                renderLoop.OnRender += UpdateFrame;
                renderLoop.Start();
            };
            this.FormClosing += (sender, e) =>
            {
                if (renderLoop != null)
                {
                    renderLoop.Stop();
                }
            };
        }
        public void SetSelectionMode(Sidebar.SelectionMode mode)
        {
            isSelecting = mode != Sidebar.SelectionMode.None;
            this.Cursor = isSelecting ? Cursors.Cross : Cursors.Default;
            selectionMode = mode;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Delete && selectedImage != null)
            {
                overhead.images.Remove(selectedImage);
                selectedImage = null; // Remove the image

                canvas.Invalidate();  // Refresh the canvas
            }
            if(e.KeyCode == Keys.C && selectedPixels.Count != 0)
            {
                sidebar.ConvertSelectionToImageFromCopy();
            }
            if (e.KeyCode == Keys.P && selectedPixels.Count != 0)
            {
                sidebar.ConvertSelectionToImageFromPaste();
            }
        }
        private void ExportSelectedObject(object sender, EventArgs e)
        {
            if (selectedImage == null)
            {
                MessageBox.Show("No image selected to export!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png";
                saveFileDialog.Title = "Export Selected Image Object";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Create a new Bitmap with the size of the selected image
                    Bitmap exportBmp = new Bitmap((int)(selectedImage.Image.Width * selectedImage.Scale), (int)(selectedImage.Image.Height * selectedImage.Scale));

                    using (Graphics g = Graphics.FromImage(exportBmp))
                    {
                        // Apply scaling and rotation
                        g.TranslateTransform(exportBmp.Width / 2, exportBmp.Height / 2);
                        g.RotateTransform(selectedImage.Rotation);
                        g.ScaleTransform(selectedImage.Scale, selectedImage.Scale);
                        g.TranslateTransform(-selectedImage.Image.Width / 2, -selectedImage.Image.Height / 2);
                        g.DrawImage(selectedImage.Image, new Rectangle(0, 0, selectedImage.Image.Width, selectedImage.Image.Height));
                    }

                    exportBmp.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show("Image exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateFrame()
        {

            if (selectedImage != null)
            {

            }
        }
        private void WarpImage(object sender, EventArgs e)
        {
            Form warpForm = new Form()
            {
                Text = "Select a Warp",
                Size = new Size(300, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
            ComboBox warpOptions = new ComboBox()
            {
                Location = new Point(50, 50),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            warpOptions.Items.AddRange(new string[] { "BasicWarp", "AverageColorWarp", "SwirlWarp", "RectangleWarp", "TriangleWarp", "WaveWarp", "PerspectiveWarp", "DistortWarp", "FaceMorphWarp"});
            Button applyButton = new Button()
            {
                Text = "Apply",
                Location = new Point(100, 100),
                Size = new Size(100, 30)
            };
            applyButton.Click += (s, ev) =>
            {
                ApplyWarp(warpOptions.SelectedItem?.ToString());
                warpForm.Close();
            };

            warpForm.Controls.Add(warpOptions);
            warpForm.Controls.Add(applyButton);
            warpForm.ShowDialog();
        }
        private void FilterImage(object sender, EventArgs e)
        {
            Form filterForm = new Form()
            {
                Text = "Select a Filter",
                Size = new Size(300, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            ComboBox filterOptions = new ComboBox()
            {
                Location = new Point(50, 50),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterOptions.Items.AddRange(new string[] { "Grayscale" });

            Button applyButton = new Button()
            {
                Text = "Apply",
                Location = new Point(100, 100),
                Size = new Size(100, 30)
            };
            applyButton.Click += (s, ev) =>
            {
                ApplyFilter(filterOptions.SelectedItem?.ToString());
                filterForm.Close();
            };

            filterForm.Controls.Add(filterOptions);
            filterForm.Controls.Add(applyButton);
            filterForm.ShowDialog();
        }
        private void ApplyWarp(string warp)
        {
            if (selectedImage == null || string.IsNullOrEmpty(warp)) return;


            if (warp != null)
            {
                IImageWarp warpClass = null;
                switch (warp)
                {
                    case "BasicWarp":
                        warpClass = BasicWarp.Configure();
                        break;
                    case "AverageColorWarp":
                        warpClass = AverageColorWarp.Configure();
                        break;
                    case "SwirlWarp":
                        warpClass = SwirlWarp.Configure();
                        break;
                    case "RectangleWarp":
                        warpClass = RectangleWarp.Configure();
                        break;
                    case "TriangleWarp":
                        warpClass = TriangleWarp.Configure();
                        break;
                    case "WaveWarp":
                        warpClass = WaveWarp.Configure();
                        break;
                    case "PerspectiveWarp":
                        warpClass = PerspectiveWarp.Configure();
                        break;
                    case "DistortWarp":
                        warpClass = DistortWarp.Configure();
                        break;
                    case "FaceMorphWarp":
                        warpClass = FaceMorphWarp.Configure();
                        break;
                }
                if (warpClass != null)
                {
                    if (selectedPixels.Count > 0)
                    {
                        var Image = sidebar.ConvertSelectedPixelsToImage();
                        Image.Image = warpClass.Apply(Image.Image, overhead);
                        canvas.Invalidate();
                    }
                    else
                    {
                        selectedImage.Image = warpClass.Apply(selectedImage.Image, overhead);
                        canvas.Invalidate();
                    }
                }
            }
        }
        private void ApplyFilter(string filter)
        {
            if (selectedImage == null || string.IsNullOrEmpty(filter)) return;


            if (filter != null)
            {
                IImageFilter filterClass = filters.Find(f => f.Name == filter);
                if (filterClass != null)
                {
                    selectedImage.Image = filterClass.Apply(selectedImage.Image);
                    canvas.Invalidate();
                }
            }
        }


        private void AddImage(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Image image = null;
                if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".webp")
                {
                    using (MagickImage magickImage = new MagickImage(openFileDialog.FileName))
                    {
                        byte[] imageBytes = magickImage.ToByteArray(MagickFormat.Png);
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            image = Image.FromStream(ms);
                        }
                    }
                }
                else
                {
                    image = Image.FromFile(openFileDialog.FileName);
                }

                ImageObject imgObj = new ImageObject()
                {
                    Image = image,
                    Position = new Point(100, 100)
                };
                overhead.images.Add(imgObj);
                selectedImage = imgObj; // Select newly added image
                canvas.Invalidate();
            }
        }
        private void ExportImage(object sender, EventArgs e)
        {
            if (overhead.images.Count == 0)
            {
                MessageBox.Show("No images to export!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp|GIF Image|*.gif";
                saveFileDialog.Title = "Export Image";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (Bitmap bmp = new Bitmap(canvas.Width, canvas.Height))
                    {
                        canvas.DrawToBitmap(bmp, new Rectangle(0, 0, canvas.Width, canvas.Height));
                        bmp.Save(saveFileDialog.FileName);
                        MessageBox.Show("Image exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {

            foreach (var imgObj in overhead.images)
            {
                e.Graphics.TranslateTransform(
                    imgObj.Position.X + (imgObj.Image.Width * imgObj.Scale) / 2,
                    imgObj.Position.Y + (imgObj.Image.Height * imgObj.Scale) / 2
                );
                e.Graphics.RotateTransform(imgObj.Rotation);
                e.Graphics.ScaleTransform(imgObj.Scale, imgObj.Scale);
                e.Graphics.TranslateTransform(-(imgObj.Image.Width / 2), -(imgObj.Image.Height / 2));
                e.Graphics.DrawImage(imgObj.Image, new Rectangle(0, 0, imgObj.Image.Width, imgObj.Image.Height));
                e.Graphics.ResetTransform();
            }

            if (selectedImage != null)
            {
                Brush anchorBrush = Brushes.Red;
                int anchorSize = 10;

                // Get transformed anchor positions
                int scaleX = selectedImage.Position.X + (int)(selectedImage.Image.Width * selectedImage.Scale);
                int scaleY = selectedImage.Position.Y + (int)(selectedImage.Image.Height * selectedImage.Scale);

                int rotateX = selectedImage.Position.X + (int)(selectedImage.Image.Width * selectedImage.Scale) / 2;
                int rotateY = selectedImage.Position.Y - 20;

                // Draw scale anchor (bottom-right)
                e.Graphics.FillEllipse(anchorBrush, scaleX - anchorSize / 2, scaleY - anchorSize / 2, anchorSize, anchorSize);

                // Draw rotate anchor (above the image)
                e.Graphics.FillEllipse(anchorBrush, rotateX - anchorSize / 2, rotateY - anchorSize / 2, anchorSize, anchorSize);
            }

            if (selectedPixels.Count > 0 && selectedImage != null && sidebar.currentSelectionMode != Sidebar.SelectionMode.None)
            {

                if (isDrawingRectangle && selectionMode == Sidebar.SelectionMode.Rectangle)
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        e.Graphics.DrawRectangle(pen, GetSelectionRectangle());
                    }
                }
                else
                {
                    using (Pen dottedPen = new Pen(Color.Blue, 1) { DashStyle = DashStyle.Dot })
                    {
                        foreach (Point p in selectedPixels)
                        {
                            // Convert to screen coordinates based on scale
                            int scaledX = (int)(selectedImage.Position.X + p.X * selectedImage.Scale);
                            int scaledY = (int)(selectedImage.Position.Y + p.Y * selectedImage.Scale);
                            e.Graphics.DrawRectangle(dottedPen, scaledX, scaledY, 1, 1);
                        }
                    }
                }
            }

        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {

            for (int i = overhead.images.Count - 1; i >= 0; i--) // Iterate from topmost image
            {
                if (overhead.images[i].GetBoundingBox().Contains(e.Location))
                {
                    selectedImage = overhead.images[i]; // Select clicked image
                    overhead.images.RemoveAt(i);
                    overhead.images.Add(selectedImage); // Bring it to the front
                    break;
                }
            }
            if ((isSelecting && selectedImage != null))
            {
                Bitmap bmp = new Bitmap(selectedImage.Image);

                // Convert mouse position to unscaled image coordinates
                int x = (int)((e.X - selectedImage.Position.X) / selectedImage.Scale);
                int y = (int)((e.Y - selectedImage.Position.Y) / selectedImage.Scale);

                if (x >= 0 && y >= 0 && x < bmp.Width && y < bmp.Height)
                {
                    Color targetColor = bmp.GetPixel(x, y);

                    if (selectionMode == Sidebar.SelectionMode.FloodFill)
                    {
                        var newSelection = sidebar.FloodFillSelection(bmp, x, y, targetColor, selectionTolerance);
                        if (Control.ModifierKeys == Keys.Shift)
                        {
                            selectedPixels.UnionWith(newSelection); // Add new selection to the existing set
                        }
                        else
                        {
                            selectedPixels = newSelection; // Replace selection if Shift is not held down
                        }
                    }
                    else if (selectionMode == Sidebar.SelectionMode.MagicWand)
                    {
                        var newSelection = sidebar.MagicWandSelection(bmp, x, y, targetColor, selectionTolerance);
                        if (Control.ModifierKeys == Keys.Shift)
                        {
                            selectedPixels.UnionWith(newSelection); // Add new selection to the existing set
                        }
                        else
                        {
                            selectedPixels = newSelection; // Replace selection if Shift is not held down
                        }
                    }else if (selectionMode == Sidebar.SelectionMode.Rectangle)
                    {
                        isDrawingRectangle = true;
                        selectionStart = e.Location;
                        selectionEnd = e.Location;
                        Invalidate();
                    }

                    // ✅ Ensure UI updates
                    canvas.Invalidate();
                }
                return;
            }

            if (selectedImage != null && e.Button == MouseButtons.Left)
            {
                Rectangle scaleAnchor = new Rectangle(
                    selectedImage.Position.X + (int)(selectedImage.Image.Width * selectedImage.Scale) - 10,
                    selectedImage.Position.Y + (int)(selectedImage.Image.Height * selectedImage.Scale) - 10,
                    20, 20
                );
                Rectangle rotateAnchor = new Rectangle(
                    selectedImage.Position.X + (int)(selectedImage.Image.Width * selectedImage.Scale) / 2 - 10,
                    selectedImage.Position.Y - 30,
                    20, 20
                );

                if (scaleAnchor.Contains(e.Location))
                {
                    isScaling = true;
                }
                else if (rotateAnchor.Contains(e.Location))
                {
                    isRotating = true;
                }
                else
                {
                    isDragging = true;
                }

                previousMousePos = e.Location;
            }

        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedImage != null)
            {
                int dx = e.X - previousMousePos.X;
                int dy = e.Y - previousMousePos.Y;
                selectedImage.Position = new Point(selectedImage.Position.X + dx, selectedImage.Position.Y + dy);
                previousMousePos = e.Location;
                canvas.Invalidate();
            }
            else if (isScaling && selectedImage != null)
            {
                int dx = e.X - previousMousePos.X;
                selectedImage.Scale = Math.Max(0.1f, selectedImage.Scale + dx * 0.01f);
                previousMousePos = e.Location;
                canvas.Invalidate();
            }
            else if (isRotating && selectedImage != null)
            {
                int dy = e.Y - previousMousePos.Y;
                selectedImage.Rotation += dy * 0.5f;
                previousMousePos = e.Location;
                canvas.Invalidate();
            }else if (isDrawingRectangle && selectionMode == Sidebar.SelectionMode.Rectangle)
            {
                selectionEnd = e.Location;
                Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            isScaling = false;
            isRotating = false;
            if (isDrawingRectangle && selectionMode == Sidebar.SelectionMode.Rectangle)
            {
                isDrawingRectangle = false;
                SelectPixelsInRectangle();
                Invalidate();
            }
        }

        private void SelectPixelsInRectangle()
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            selectedPixels.Clear();
            Bitmap bmp = new Bitmap(selectedImage.Image);

            Rectangle selectionRect = GetSelectionRectangle();

            // Convert selection rectangle from screen space to image space
            float scaleX = (float)bmp.Width / this.Width;
            float scaleY = (float)bmp.Height / this.Height;

            int startX = Math.Max(0, (int)((selectionRect.Left - selectedImage.Position.X) / selectedImage.Scale));
            int startY = Math.Max(0, (int)((selectionRect.Top - selectedImage.Position.Y) / selectedImage.Scale));
            int endX = Math.Min(bmp.Width - 1, (int)((selectionRect.Right - selectedImage.Position.X) / selectedImage.Scale));
            int endY = Math.Min(bmp.Height - 1, (int)((selectionRect.Bottom - selectedImage.Position.Y) / selectedImage.Scale));

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    selectedPixels.Add(new Point(x, y));
                }
            }
        }


        private Rectangle GetSelectionRectangle()
        {
            int x = Math.Min(selectionStart.X, selectionEnd.X);
            int y = Math.Min(selectionStart.Y, selectionEnd.Y);
            int width = Math.Abs(selectionStart.X - selectionEnd.X);
            int height = Math.Abs(selectionStart.Y - selectionEnd.Y);
            return new Rectangle(x, y, width, height);
        }


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ImageEditor());
        }
    }
}
