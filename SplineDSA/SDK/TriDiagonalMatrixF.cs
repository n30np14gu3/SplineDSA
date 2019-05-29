using System;
using System.Diagnostics;
using System.Text;

namespace SplineDSA.SDK
{
    public class TriDiagonalMatrixF
    {
        public float[] A;

        public float[] B;

        public float[] C;

        public int N => A?.Length ?? 0;

        public float this[int row, int col]
        {
            get
            {
                int di = row - col;

                if (di == 0)
                {
                    return B[row];
                }

                if (di == -1)
                {
                    Debug.Assert(row < N - 1);
                    return C[row];
                }

                if (di == 1)
                {
                    Debug.Assert(row > 0);
                    return A[row];
                }

                return 0;
            }
            set
            {
                int di = row - col;

                if (di == 0)
                {
                    B[row] = value;
                }
                else if (di == -1)
                {
                    Debug.Assert(row < N - 1);
                    C[row] = value;
                }
                else if (di == 1)
                {
                    Debug.Assert(row > 0);
                    A[row] = value;
                }
                else
                {
                    throw new ArgumentException("Only the main, super, and sub diagonals can be set.");
                }
            }
        }

        public TriDiagonalMatrixF(int n)
        {
            A = new float[n];
            B = new float[n];
            C = new float[n];
        }


        public string ToDisplayString(string fmt = "", string prefix = "")
        {
            if (N > 0)
            {
                var s = new StringBuilder();
                string formatString = "{0" + fmt + "}";

                for (int r = 0; r < N; r++)
                {
                    s.Append(prefix);

                    for (int c = 0; c < N; c++)
                    {
                        s.AppendFormat(formatString, this[r, c]);
                        if (c < N - 1) s.Append(", ");
                    }

                    s.AppendLine();
                }

                return s.ToString();
            }
            else
            {
                return prefix + "0x0 Matrix";
            }
        }


        public float[] Solve(float[] d)
        {
            int n = N;

            if (d.Length != n)
            {
                throw new ArgumentException("The input d is not the same size as this matrix.");
            }

            float[] cPrime = new float[n];
            cPrime[0] = C[0] / B[0];

            for (int i = 1; i < n; i++)
            {
                cPrime[i] = C[i] / (B[i] - cPrime[i - 1] * A[i]);
            }

            float[] dPrime = new float[n];
            dPrime[0] = d[0] / B[0];

            for (int i = 1; i < n; i++)
            {
                dPrime[i] = (d[i] - dPrime[i - 1] * A[i]) / (B[i] - cPrime[i - 1] * A[i]);
            }

            float[] x = new float[n];
            x[n - 1] = dPrime[n - 1];

            for (int i = n - 2; i >= 0; i--)
            {
                x[i] = dPrime[i] - cPrime[i] * x[i + 1];
            }

            return x;
        }
    }
}
