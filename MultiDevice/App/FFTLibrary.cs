using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    class FFTLibrary
    {
        public class MyComplex
        {
            public double re, im;

            public MyComplex()
            {
                re = im = 0.0;
            }

            public MyComplex(double r, double i)
            {
                re = r;
                im = i;
            }

            public MyComplex Add(MyComplex z)
            {
                return new MyComplex(re + z.re, im + z.im);
            }

            public MyComplex Sub(MyComplex z)
            {
                return new MyComplex(re - z.re, im - z.im);
            }

            public MyComplex Mul(MyComplex z)
            {
                double r = re * z.re - im * z.im;
                double i = re * z.im + im * z.re;

                return new MyComplex(r, i);
            }

            public MyComplex Div(MyComplex z)
            {
                double d = z.re * z.re + z.im * z.im;
                double r = (re * z.re + im * z.im) / d;
                double i = (im * z.re - re * z.im) / d;

                return new MyComplex(r, i);
            }

            public static double Abs(MyComplex z)
            {
                return Math.Sqrt(z.re * z.re + z.im * z.im);
            }

            public static MyComplex Exp(MyComplex z)
            {
                double e = Math.Exp(z.re);
                double r = Math.Cos(z.im);
                double i = Math.Sin(z.im);

                return new MyComplex(e * r, e * i);
            }

            public static MyComplex[] RecursiveFFT(MyComplex[] a)
            {
                int n = a.Length;
                int n2 = n / 2;

                if (n == 1)
                    return a;

                MyComplex z = new MyComplex(0.0, 2.0 * Math.PI / n);
                MyComplex omegaN = MyComplex.Exp(z);
                MyComplex omega = new MyComplex(1.0, 0.0);
                MyComplex[] a0 = new MyComplex[n2];
                MyComplex[] a1 = new MyComplex[n2];
                MyComplex[] y0 = new MyComplex[n2];
                MyComplex[] y1 = new MyComplex[n2];
                MyComplex[] y = new MyComplex[n];

                for (int i = 0; i < n2; i++)
                {
                    a0[i] = new MyComplex(0.0, 0.0);
                    a0[i] = a[2 * i];
                    a1[i] = new MyComplex(0.0, 0.0);
                    a1[i] = a[2 * i + 1];
                }

                y0 = RecursiveFFT(a0);
                y1 = RecursiveFFT(a1);

                for (int k = 0; k < n2; k++)
                {
                    y[k] = new MyComplex(0.0, 0.0);
                    y[k] = y0[k].Add(y1[k].Mul(omega));
                    y[k + n2] = new MyComplex(0.0, 0.0);
                    y[k + n2] = y0[k].Sub(y1[k].Mul(omega));
                    omega = omega.Mul(omegaN);
                }

                return y;
            }

            public static MyComplex[] DFT(double[] x)
            {
                double pi2oN = 2.0 * Math.PI / x.Length;
                int k, n;
                MyComplex[] X = new MyComplex[x.Length];

                for (k = 0; k < x.Length; k++)
                {
                    X[k] = new MyComplex(0.0, 0.0);

                    for (n = 0; n < x.Length; n++)
                    {
                        X[k].re += x[n] * Math.Cos(pi2oN * k * n);
                        X[k].im -= x[n] * Math.Sin(pi2oN * k * n);
                    }

                    X[k].re /= x.Length;
                    X[k].im /= x.Length;
                }

                return X;
            }

            public static double[] InverseDFT(MyComplex[] X)
            {
                double[] x = new double[X.Length];
                double imag, pi2oN = 2.0 * Math.PI / X.Length;

                for (int n = 0; n < X.Length; n++)
                {
                    imag = x[n] = 0.0;

                    for (int k = 0; k < X.Length; k++)
                    {
                        x[n] += X[k].re * Math.Cos(pi2oN * k * n)
                              - X[k].im * Math.Sin(pi2oN * k * n);
                        imag += X[k].re * Math.Sin(pi2oN * k * n)
                              + X[k].im * Math.Cos(pi2oN * k * n);
                    }
                }

                return x;
            }

            // This computes an in-place MyComplex-to-MyComplex FFT  
            // x and y are the real and imaginary arrays of 2^m points. 
            // dir =  1 gives forward transform 
            // dir = -1 gives reverse transform  
            // see http://astronomy.swin.edu.au/~pbourke/analysis/dft/ 

            public static void FFT(short dir, int m, double[] x, double[] y)
            {
                int n, i, i1, j, k, i2, l, l1, l2;
                double c1, c2, tx, ty, t1, t2, u1, u2, z;

                // Calculate the number of points 

                n = 1;

                for (i = 0; i < m; i++)
                    n *= 2;

                // Do the bit reversal 

                i2 = n >> 1;
                j = 0;
                for (i = 0; i < n - 1; i++)
                {
                    if (i < j)
                    {
                        tx = x[i];
                        ty = y[i];
                        x[i] = x[j];
                        y[i] = y[j];
                        x[j] = tx;
                        y[j] = ty;
                    }
                    k = i2;

                    while (k <= j)
                    {
                        j -= k;
                        k >>= 1;
                    }

                    j += k;
                }

                // Compute the FFT 

                c1 = -1.0;
                c2 = 0.0;
                l2 = 1;

                for (l = 0; l < m; l++)
                {
                    l1 = l2;
                    l2 <<= 1;
                    u1 = 1.0;
                    u2 = 0.0;

                    for (j = 0; j < l1; j++)
                    {
                        for (i = j; i < n; i += l2)
                        {
                            i1 = i + l1;
                            t1 = u1 * x[i1] - u2 * y[i1];
                            t2 = u1 * y[i1] + u2 * x[i1];
                            x[i1] = x[i] - t1;
                            y[i1] = y[i] - t2;
                            x[i] += t1;
                            y[i] += t2;
                        }

                        z = u1 * c1 - u2 * c2;
                        u2 = u1 * c2 + u2 * c1;
                        u1 = z;
                    }

                    c2 = Math.Sqrt((1.0 - c1) / 2.0);

                    if (dir == 1)
                        c2 = -c2;

                    c1 = Math.Sqrt((1.0 + c1) / 2.0);
                }

                // Scaling for forward transform 

                if (dir == 1)
                {
                    for (i = 0; i < n; i++)
                    {
                        x[i] /= n;
                        y[i] /= n;
                    }
                }
            }
        }
    }
}
