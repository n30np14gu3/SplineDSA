using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using SplineDSA.SDK;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;

namespace SplineDSA
{
    public partial class MainForm : Form
    {

        List<PointF> points = new List<PointF>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenImage.FileName = "";
            if (OpenImage.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                MainImage.Image = Image.FromFile(OpenImage.FileName);
            }
            catch
            {
                Application.DoEvents();
            }
        }

        float distance(PointF x1, PointF x2)
        {
            return (float) Math.Sqrt(Math.Pow(x1.X - x2.X, 2) + Math.Pow(x1.Y - x2.Y, 2));
        }

        List<PointF> sort(List<PointF> p)
        {
            List<PointF> result = new List<PointF>();
            PointF first = p[0];
            result.Add(first);
            p.Remove(first);
            while (p.Count != 0)
            {
                PointF pp = p.OrderBy(x => distance(x, first)).First();
                result.Add(pp);
                p.Remove(pp);
                first = pp;
            }

            return result;
        }
        private void drawSplineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainImage.Image == null)
                return;

            float[] ex;
            float[] ey;
            SusanCornersDetector scd = new SusanCornersDetector();
            PointF[] ppp = scd.ProcessImage((Bitmap) MainImage.Image).Select(x => new PointF(x.X, x.Y)).ToArray();
            ppp = sort(ppp.ToList()).ToArray();
            CubicSpline.FitParametric(points.Select(x => x.X).ToArray(), points.Select(x => x.Y).ToArray(), 3000, out ex, out ey);
            using (Graphics g = Graphics.FromImage(MainImage.Image))
            {
                for (int i = 1; i < ex.Length; i++)
                    g.DrawLine(new Pen(Color.Red), ex[i - 1], ey[i - 1], ex[i], ey[i]);
            }
            MainImage.Invalidate();
        }

        private void MainImage_MouseClick(object sender, MouseEventArgs e)
        {
            points.Add(e.Location);
            using (Graphics g = Graphics.FromImage((Bitmap)MainImage.Image))
            {
                g.DrawRectangle(new Pen(Color.Red), e.X, e.Y, 1, 1);
            }
            MainImage.Invalidate();
        }

    }
}
