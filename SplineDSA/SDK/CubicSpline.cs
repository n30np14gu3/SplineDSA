using System;

namespace SplineDSA.SDK
{

    public class CubicSpline
    {
        #region Fields

        private float[] a;
        private float[] b;

        private float[] xOrig;
        private float[] yOrig;

        #endregion

        #region Ctor

        public CubicSpline()
        {
        }

        public CubicSpline(float[] x, float[] y, float startSlope = float.NaN, float endSlope = float.NaN, bool debug = false)
        {
            Fit(x, y, startSlope, endSlope, debug);
        }

        #endregion

        #region Private Methods

        private void CheckAlreadyFitted()
        {
            if (a == null) throw new Exception("Fit must be called before you can evaluate.");
        }

        private int _lastIndex = 0;

        private int GetNextXIndex(float x)
        {
            if (x < xOrig[_lastIndex])
            {
                throw new ArgumentException("The X values to evaluate must be sorted.");
            }

            while ((_lastIndex < xOrig.Length - 2) && (x > xOrig[_lastIndex + 1]))
            {
                _lastIndex++;
            }

            return _lastIndex;
        }


        private float EvalSpline(float x, int j, bool debug = false)
        {
            float dx = xOrig[j + 1] - xOrig[j];
            float t = (x - xOrig[j]) / dx;
            float y = (1 - t) * yOrig[j] + t * yOrig[j + 1] + t * (1 - t) * (a[j] * (1 - t) + b[j] * t); // equation 9
            if (debug) Console.WriteLine("xs = {0}, j = {1}, t = {2}", x, j, t);
            return y;
        }

        #endregion

        #region Fit

        public float[] FitAndEval(float[] x, float[] y, float[] xs, float startSlope = float.NaN, float endSlope = float.NaN, bool debug = false)
        {
            Fit(x, y, startSlope, endSlope, debug);
            return Eval(xs, debug);
        }

        public void Fit(float[] x, float[] y, float startSlope = float.NaN, float endSlope = float.NaN, bool debug = false)
        {
            if (Single.IsInfinity(startSlope) || Single.IsInfinity(endSlope))
            {
                throw new Exception("startSlope and endSlope cannot be infinity.");
            }

            // Save x and y for eval
            this.xOrig = x;
            this.yOrig = y;

            int n = x.Length;
            float[] r = new float[n]; // the right hand side numbers: wikipedia page overloads b

            TriDiagonalMatrixF m = new TriDiagonalMatrixF(n);
            float dx1, dx2, dy1, dy2;

            // First row is different (equation 16 from the article)
            if (float.IsNaN(startSlope))
            {
                dx1 = x[1] - x[0];
                m.C[0] = 1.0f / dx1;
                m.B[0] = 2.0f * m.C[0];
                r[0] = 3 * (y[1] - y[0]) / (dx1 * dx1);
            }
            else
            {
                m.B[0] = 1;
                r[0] = startSlope;
            }

            // Body rows (equation 15 from the article)
            for (int i = 1; i < n - 1; i++)
            {
                dx1 = x[i] - x[i - 1];
                dx2 = x[i + 1] - x[i];

                m.A[i] = 1.0f / dx1;
                m.C[i] = 1.0f / dx2;
                m.B[i] = 2.0f * (m.A[i] + m.C[i]);

                dy1 = y[i] - y[i - 1];
                dy2 = y[i + 1] - y[i];
                r[i] = 3 * (dy1 / (dx1 * dx1) + dy2 / (dx2 * dx2));
            }

            // Last row also different (equation 17 from the article)
            if (float.IsNaN(endSlope))
            {
                dx1 = x[n - 1] - x[n - 2];
                dy1 = y[n - 1] - y[n - 2];
                m.A[n - 1] = 1.0f / dx1;
                m.B[n - 1] = 2.0f * m.A[n - 1];
                r[n - 1] = 3 * (dy1 / (dx1 * dx1));
            }
            else
            {
                m.B[n - 1] = 1;
                r[n - 1] = endSlope;
            }

            if (debug) Console.WriteLine("Tri-diagonal matrix:\n{0}", m.ToDisplayString(":0.0000", "  "));
            if (debug) Console.WriteLine("r: {0}", ArrayUtil.ToString<float>(r));

            // k is the solution to the matrix
            float[] k = m.Solve(r);
            if (debug) Console.WriteLine("k = {0}", ArrayUtil.ToString<float>(k));

            // a and b are each spline's coefficients
            this.a = new float[n - 1];
            this.b = new float[n - 1];

            for (int i = 1; i < n; i++)
            {
                dx1 = x[i] - x[i - 1];
                dy1 = y[i] - y[i - 1];
                a[i - 1] = k[i - 1] * dx1 - dy1; // equation 10 from the article
                b[i - 1] = -k[i] * dx1 + dy1; // equation 11 from the article
            }

            if (debug) Console.WriteLine("a: {0}", ArrayUtil.ToString<float>(a));
            if (debug) Console.WriteLine("b: {0}", ArrayUtil.ToString<float>(b));
        }

        #endregion

        #region Eval*

        public float[] Eval(float[] x, bool debug = false)
        {
            CheckAlreadyFitted();

            int n = x.Length;
            float[] y = new float[n];
            _lastIndex = 0; // Reset simultaneous traversal in case there are multiple calls

            for (int i = 0; i < n; i++)
            {
                // Find which spline can be used to compute this x (by simultaneous traverse)
                int j = GetNextXIndex(x[i]);

                // Evaluate using j'th spline
                y[i] = EvalSpline(x[i], j, debug);
            }

            return y;
        }

        public float[] EvalSlope(float[] x, bool debug = false)
        {
            CheckAlreadyFitted();

            int n = x.Length;
            float[] qPrime = new float[n];
            _lastIndex = 0; // Reset simultaneous traversal in case there are multiple calls

            for (int i = 0; i < n; i++)
            {
                // Find which spline can be used to compute this x (by simultaneous traverse)
                int j = GetNextXIndex(x[i]);

                // Evaluate using j'th spline
                float dx = xOrig[j + 1] - xOrig[j];
                float dy = yOrig[j + 1] - yOrig[j];
                float t = (x[i] - xOrig[j]) / dx;

                // From equation 5 we could also compute q' (qp) which is the slope at this x
                qPrime[i] = dy / dx
                    + (1 - 2 * t) * (a[j] * (1 - t) + b[j] * t) / dx
                    + t * (1 - t) * (b[j] - a[j]) / dx;

                if (debug) Console.WriteLine("[{0}]: xs = {1}, j = {2}, t = {3}", i, x[i], j, t);
            }

            return qPrime;
        }

        #endregion

        #region Static Methods


        public static float[] Compute(float[] x, float[] y, float[] xs, float startSlope = float.NaN, float endSlope = float.NaN, bool debug = false)
        {
            CubicSpline spline = new CubicSpline();
            return spline.FitAndEval(x, y, xs, startSlope, endSlope, debug);
        }


        public static void FitParametric(float[] x, float[] y, int nOutputPoints, out float[] xs, out float[] ys,
            float firstDx = Single.NaN, float firstDy = Single.NaN, float lastDx = Single.NaN, float lastDy = Single.NaN)
        {
            // Compute distances
            int n = x.Length;
            float[] dists = new float[n]; // cumulative distance
            dists[0] = 0;
            float totalDist = 0;

            for (int i = 1; i < n; i++)
            {
                float dx = x[i] - x[i - 1];
                float dy = y[i] - y[i - 1];
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                totalDist += dist;
                dists[i] = totalDist;
            }

            // Create 'times' to interpolate to
            float dt = totalDist / (nOutputPoints - 1);
            float[] times = new float[nOutputPoints];
            times[0] = 0;

            for (int i = 1; i < nOutputPoints; i++)
            {
                times[i] = times[i - 1] + dt;
            }

            // Normalize the slopes, if specified
            NormalizeVector(ref firstDx, ref firstDy);
            NormalizeVector(ref lastDx, ref lastDy);

            // Spline fit both x and y to times
            CubicSpline xSpline = new CubicSpline();
            xs = xSpline.FitAndEval(dists, x, times, firstDx / dt, lastDx / dt);

            CubicSpline ySpline = new CubicSpline();
            ys = ySpline.FitAndEval(dists, y, times, firstDy / dt, lastDy / dt);
        }

        private static void NormalizeVector(ref float dx, ref float dy)
        {
            if (!Single.IsNaN(dx) && !Single.IsNaN(dy))
            {
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                if (d > Single.Epsilon) // probably not conservative enough, but catches the (0,0) case at least
                {
                    dx = dx / d;
                    dy = dy / d;
                }
                else
                {
                    throw new ArgumentException("The input vector is too small to be normalized.");
                }
            }
            else
            {
                // In case one is NaN and not the other
                dx = dy = Single.NaN;
            }
        }

        #endregion
    }
}
