using System;
using static System.Single;

namespace SplineDSA.SDK
{

    public class CubicSpline
    {
        #region Fields

        private float[] _a;
        private float[] _b;

        private float[] _xOrig;
        private float[] _yOrig;

        #endregion

        #region Ctor

        public CubicSpline()
        {
        }

        public CubicSpline(float[] x, float[] y, float startSlope = NaN, float endSlope = NaN, bool debug = false)
        {
            Fit(x, y, startSlope, endSlope, debug);
        }

        #endregion

        #region Private Methods

        private void CheckAlreadyFitted()
        {
            if (_a == null) throw new Exception("Fit must be called before you can evaluate.");
        }

        private int _lastIndex;

        private int GetNextXIndex(float x)
        {
            if (x < _xOrig[_lastIndex])
            {
                throw new ArgumentException("The X values to evaluate must be sorted.");
            }

            while ((_lastIndex < _xOrig.Length - 2) && (x > _xOrig[_lastIndex + 1]))
            {
                _lastIndex++;
            }

            return _lastIndex;
        }


        private float EvalSpline(float x, int j, bool debug = false)
        {
            float dx = _xOrig[j + 1] - _xOrig[j];
            float t = (x - _xOrig[j]) / dx;
            float y = (1 - t) * _yOrig[j] + t * _yOrig[j + 1] + t * (1 - t) * (_a[j] * (1 - t) + _b[j] * t);
            if (debug) Console.WriteLine($"xs = {x}, j = {j}, t = {t}");
            return y;
        }

        #endregion

        #region Fit

        public float[] FitAndEval(float[] x, float[] y, float[] xs, float startSlope = NaN, float endSlope = NaN, bool debug = false)
        {
            Fit(x, y, startSlope, endSlope, debug);
            return Eval(xs, debug);
        }

        public void Fit(float[] x, float[] y, float startSlope = NaN, float endSlope = NaN, bool debug = false)
        {
            if (IsInfinity(startSlope) || IsInfinity(endSlope))
            {
                throw new Exception("startSlope and endSlope cannot be infinity.");
            }

            _xOrig = x;
            _yOrig = y;

            int n = x.Length;
            float[] r = new float[n];

            TriDiagonalMatrixF m = new TriDiagonalMatrixF(n);
            float dx1, dx2, dy1, dy2;

            if (IsNaN(startSlope))
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

            if (IsNaN(endSlope))
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

            if (debug) Console.WriteLine($"Tri-diagonal matrix:\n{m.ToDisplayString(":0.0000", "  ")}");
            if (debug) Console.WriteLine($"r: {ArrayUtil.ToString(r)}");

            float[] k = m.Solve(r);
            if (debug) Console.WriteLine($"k = {ArrayUtil.ToString(k)}");

            _a = new float[n - 1];
            _b = new float[n - 1];

            for (int i = 1; i < n; i++)
            {
                dx1 = x[i] - x[i - 1];
                dy1 = y[i] - y[i - 1];
                _a[i - 1] = k[i - 1] * dx1 - dy1;
                _b[i - 1] = -k[i] * dx1 + dy1;
            }

            if (debug) Console.WriteLine($"a: {ArrayUtil.ToString(_a)}");
            if (debug) Console.WriteLine($"b: {ArrayUtil.ToString(_b)}");
        }

        #endregion

        #region Eval*

        public float[] Eval(float[] x, bool debug = false)
        {
            CheckAlreadyFitted();

            int n = x.Length;
            float[] y = new float[n];
            _lastIndex = 0; 

            for (int i = 0; i < n; i++)
            {
                int j = GetNextXIndex(x[i]);

                y[i] = EvalSpline(x[i], j, debug);
            }

            return y;
        }

        public float[] EvalSlope(float[] x, bool debug = false)
        {
            CheckAlreadyFitted();

            int n = x.Length;
            float[] qPrime = new float[n];
            _lastIndex = 0; 

            for (int i = 0; i < n; i++)
            {
                int j = GetNextXIndex(x[i]);

                float dx = _xOrig[j + 1] - _xOrig[j];
                float dy = _yOrig[j + 1] - _yOrig[j];
                float t = (x[i] - _xOrig[j]) / dx;

                qPrime[i] = dy / dx
                    + (1 - 2 * t) * (_a[j] * (1 - t) + _b[j] * t) / dx
                    + t * (1 - t) * (_b[j] - _a[j]) / dx;

                if (debug) Console.WriteLine($"[{i}]: xs = {x[i]}, j = {j}, t = {t}");
            }

            return qPrime;
        }

        #endregion

        #region Static Methods


        public static float[] Compute(float[] x, float[] y, float[] xs, float startSlope = NaN, float endSlope = NaN, bool debug = false)
        {
            CubicSpline spline = new CubicSpline();
            return spline.FitAndEval(x, y, xs, startSlope, endSlope, debug);
        }


        public static void FitParametric(float[] x, float[] y, int nOutputPoints, out float[] xs, out float[] ys,
            float firstDx = NaN, float firstDy = NaN, float lastDx = NaN, float lastDy = NaN)
        {
            int n = x.Length;
            float[] dists = new float[n];
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

            float dt = totalDist / (nOutputPoints - 1);
            float[] times = new float[nOutputPoints];
            times[0] = 0;

            for (int i = 1; i < nOutputPoints; i++)
            {
                times[i] = times[i - 1] + dt;
            }

            NormalizeVector(ref firstDx, ref firstDy);
            NormalizeVector(ref lastDx, ref lastDy);

            CubicSpline xSpline = new CubicSpline();
            xs = xSpline.FitAndEval(dists, x, times, firstDx / dt, lastDx / dt);

            CubicSpline ySpline = new CubicSpline();
            ys = ySpline.FitAndEval(dists, y, times, firstDy / dt, lastDy / dt);
        }

        private static void NormalizeVector(ref float dx, ref float dy)
        {
            if (!IsNaN(dx) && !IsNaN(dy))
            {
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                if (d > Epsilon)
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
                dx = dy = NaN;
            }
        }

        #endregion
    }
}
