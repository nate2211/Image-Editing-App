using System;
using System.Windows.Forms;
using System.Threading;

namespace ImageEditingApp
{

    namespace ImageEditingApp
    {
        public class RenderLoop
        {
            private Form targetForm;
            private PictureBox targetCanvas;
            private bool isRunning = false;
            private Thread renderThread;

            public event Action OnRender; // Event triggered every frame

            public RenderLoop(Form form, PictureBox canvas)
            {
                this.targetForm = form;
                this.targetCanvas = canvas;
            }

            public void Start()
            {
                if (isRunning) return;
                isRunning = true;

                renderThread = new Thread(RenderLoopThread)
                {
                    IsBackground = true
                };
                renderThread.Start();
            }

            public void Stop()
            {
                isRunning = false;
                renderThread?.Join();
            }

            private void RenderLoopThread()
            {
                while (isRunning)
                {
                    targetForm.Invoke((Action)(() =>
                    {
                        OnRender?.Invoke(); // Invoke custom rendering logic
                        targetCanvas.Invalidate(); // Refresh canvas every frame
                    }));

                    Thread.Sleep(16); // Roughly 60 FPS (1000ms / 60)
                }
            }
        }
    }
}
