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




namespace ImageEditingApp
{
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
    }
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
        public List<ImageObject> images = new List<ImageObject>(); // Store multiple images
        private List<IImageFilter> filters = new List<IImageFilter> { new GrayscaleFilter() };

        public HashSet<Point> selectedPixels = new HashSet<Point>(); // Store selected pixels
        public int selectionTolerance = 50; // Tolerance for Magic Wand

        private Sidebar sidebar;
        private RenderLoop renderLoop;


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
            sidebar = new Sidebar(this);
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
                images.Remove(selectedImage);
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
            warpOptions.Items.AddRange(new string[] { "BasicWarp", "AverageColorWarp", "SwirlWarp", "RectangleWarp", "TriangleWarp", "WaveWarp", "PerspectiveWarp", "DistortWarp"});
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
                }
                if (warpClass != null)
                {
                    if (selectedPixels.Count > 0)
                    {
                        var Image = sidebar.ConvertSelectedPixelsToImage();
                        Image.Image = warpClass.Apply(Image.Image);
                        canvas.Invalidate();
                    }
                    else
                    {
                        selectedImage.Image = warpClass.Apply(selectedImage.Image);
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
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImageObject imgObj = new ImageObject()
                {
                    Image = Image.FromFile(openFileDialog.FileName),
                    Position = new Point(100, 100)
                };
                images.Add(imgObj);
                selectedImage = imgObj; // Select newly added image
                canvas.Invalidate();
            }
        }

        private void ExportImage(object sender, EventArgs e)
        {
            if (images.Count == 0)
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

            foreach (var imgObj in images)
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

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {

            for (int i = images.Count - 1; i >= 0; i--) // Iterate from topmost image
            {
                if (images[i].GetBoundingBox().Contains(e.Location))
                {
                    selectedImage = images[i]; // Select clicked image
                    images.RemoveAt(i);
                    images.Add(selectedImage); // Bring it to the front
                    break;
                }
            }
            if (isSelecting && selectedImage != null)
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
                        selectedPixels = sidebar.FloodFillSelection(bmp, x, y, targetColor, selectionTolerance);
                    }
                    else if (selectionMode == Sidebar.SelectionMode.MagicWand)
                    {
                        selectedPixels = sidebar.MagicWandSelection(bmp, x, y, targetColor, selectionTolerance);
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
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            isScaling = false;
            isRotating = false;
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
