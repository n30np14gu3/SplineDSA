using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using SplineDSA.SDK;

namespace SplineDSA
{
    public partial class MainForm : Form
    {
        private bool _painted;
        private List<PointF> _points = new List<PointF>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _points.Clear();
            _painted = false;
            Bitmap b = new Bitmap(MainImage.Width, MainImage.Height);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, MainImage.Width, MainImage.Height);
            }

            MainImage.Image = b;
        }

        private void drawSplineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(_points.Count == 0)
                return;

            float[] ex;
            float[] ey;


            CubicSpline.FitParametric(_points.Select(x => x.X).ToArray(), _points.Select(x => x.Y).ToArray(), _points.Count * 100, out ex, out ey);
            using (Graphics g = Graphics.FromImage(MainImage.Image))
            {
                for(int i = 1; i < _points.Count; i++)
                    g.DrawLine(new Pen(Color.Green, 1), _points[i - 1], _points[i]);

                for (int i = 1; i < ex.Length; i++)
                    g.DrawLine(new Pen(Color.Red, 2), ex[i - 1], ey[i - 1], ex[i], ey[i]);

                g.DrawCurve(new Pen(Color.Blue), _points.ToArray());
            }
            MainImage.Invalidate();
            MessageBox.Show("RED: Spline interpolation\r\nBLUE: C# Curve line\r\nGREEN: Line by points", "INFO",
                MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void MainImage_MouseClick(object sender, MouseEventArgs e)
        {
            if(MainImage.Image == null)
                return;

            if (_points.Where(x => (int)x.Y == e.Y && (int)x.X == e.X).ToList().Count == 0)
            {
                _points.Add(e.Location);
                using (Graphics g = Graphics.FromImage((Bitmap)MainImage.Image))
                {
                    g.DrawRectangle(new Pen(Color.Green), e.X, e.Y, 1, 1);
                }
                MainImage.Invalidate();
            }
        }

        private void MainImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (MainImage.Image == null)
                return;

            _painted = true;
        }

        private void MainImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (MainImage.Image == null)
                return;

            _painted = false;
        }

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (MainImage.Image == null)
                return;

            if (_painted)
            {
                int r = new Random().Next(0, 10);
                if(r % 2 == 0)
                    return;

                if (_points.Where(x => (int) x.Y == e.Y && (int) x.X == e.X).ToList().Count == 0)
                {
                    _points.Add(e.Location);
                    using (Graphics g = Graphics.FromImage((Bitmap)MainImage.Image))
                    {
                        g.DrawRectangle(new Pen(Color.Green), e.X, e.Y, 1, 1);
                    }
                    MainImage.Invalidate();
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MainImage.Image == null)
                return;

            MainImage.Image.Save("spline.bmp", ImageFormat.Bmp);
        }
    }
}
