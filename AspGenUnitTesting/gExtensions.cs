using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using gClass;
using AspGenUnitTesting;



namespace gExtensions
{
    public static class gExtensions
    {
        static public List<Vector3D> GenerateRandomRayVectors(this double ap, int baseraysct)
        {
            List<Vector3D> Vlist = new List<Vector3D>();
            double x, y;
            Random rd = new Random();
            for (int i = 0; i < baseraysct; i++)
            {
                x = ap * (rd.NextDouble() * 2.0 - 1.0);
                y = ap * (rd.NextDouble() * 2.0 - 1.0);
                while (RTM.Hypo(x, y) > ap)
                {
                    x = ap * (rd.NextDouble() * 2.0 - 1.0);
                    y = ap * (rd.NextDouble() * 2.0 - 1.0);
                }
                Vlist.Add(new Vector3D(x, y, 0));
            }
            return Vlist;
        }

        static public List<Vector3D> GenerateUniformRayVectors(this double ap, int numrays)
        {
            List<Vector3D> vlist = new List<Vector3D>();
            for (double y = -ap; y <= ap * 1.001; y += ap / numrays)
                for (double x = -ap; x <= ap * 1.001; x += ap / numrays)
                {
                    var h = Math.Sqrt(x * x + y * y);
                    if (h <= ap)
                        vlist.Add(new Vector3D(x, y, 0));
                }
            return vlist;
        }

        static public List<Vector3D> GenerateFibonacciRayVectors(this double ap, int baseraysct)
        {
            List<Vector3D> vlist = new List<Vector3D>();

            /* math needed to calc fibonacci positions
            from numpy import arange, pi, sin, cos, arccos
            n = 50
            i = arange(0, n, dtype = float) + 0.5
            phi = arccos(1 - 2 * i / n)
            goldenRatio = (1 + 5 * *0.5) / 2
            theta = 2 pi* i / goldenRatio
            x, y, z = cos(theta) * sin(phi), sin(theta) * sin(phi), cos(phi);
            */

            double goldenratio = (1 + Math.Sqrt(5)) / 2;
            for (int i = 0; i < baseraysct; i++)
            {
                double x = (double)i / goldenratio - Math.Truncate((double)i / goldenratio);
                double y = (double)i / baseraysct;
                List<double> theta = new List<double>();
                List<double> rad = new List<double>();
                if (Math.Sqrt(x * x + y * y) <= 10)
                {
                    theta.Add(2 * Math.PI * x);
                    rad.Add(Math.Sqrt(y));
                    double xs = (ap * 0.1 * 10 * Math.Sqrt(y) * Math.Cos(2 * Math.PI * x));
                    double ys = (ap * 0.1 * 10 * Math.Sqrt(y) * Math.Sin(2 * Math.PI * x));
                    vlist.Add(new Vector3D(xs, ys, 0));
                }
            }
            return vlist;
        }

        static public double VDistance(this Vector3D v0, Vector3D v1)
        {
            return Math.Sqrt( (v0.X - v1.X) * (v0.X - v1.X) +
                              (v0.Y - v1.Y) * (v0.Y - v1.Y) +
                              (v0.Z - v1.Z) * (v0.Z - v1.Z) );
        }

        static public int SetSurfaceType(this Side side)
        {
            if ((Math.Abs(side.R) < 0.01) &&
                 (Math.Abs(side.K) < 1e-8) &&
                 (Math.Abs(side.AD) < 1e-20) &&
                 (Math.Abs(side.AE) < 1e-20))
                return 0;

            if ((Math.Abs(side.AD) < 1e-20) &&
                 (Math.Abs(side.AE) < 1e-20))
                return 1;

            return 2;
        }

        //
        //
        static public double SetCurvature(this double R, double RtolforZero = 0.01)
        {
            if (Math.Abs(R) > RtolforZero)
                return 1 / R;
            else
                return 0;
        }

        static public (DirectionVector Norms, double F) SurfNorm(this Point3D pt, Side s)
        {
            var p = pt.X * pt.X + pt.Y * pt.Y;
            var zpartial = (pt.Z - s.AD * p * p - s.AE * p * p * p);
            var F = -(s.C / 2) * p * (s.K + 1) - (s.C / 2) * (s.K + 1) * zpartial * zpartial + zpartial;
            var t2 = (s.C * (1 - 2 * (s.K + 1) * (2 * s.AD * p + 3 * s.AE * p * p) * zpartial) + (4 * s.AD * p + 6 * s.AE * p * p));

            var norms = new DirectionVector(-pt.X * t2, -pt.Y * t2, 1 - s.C * (s.K + 1) * zpartial).Normalize();
            return (norms, F);
        }

        static public (DirectionVector Norms, double F) SurfNormShort(this Point3D pt, Side s)
        {
            var p = pt.X * pt.X + pt.Y * pt.Y;

            var F = pt.Z - (s.C / 2) * p - (s.C / 2) * (s.K + 1) * pt.Z * pt.Z;

            var dFdz = 1 - s.C * (s.K + 1) * pt.Z;

            var norms = new DirectionVector(-pt.X * s.C, -pt.Y * s.C, 1 - s.C * (s.K + 1) * pt.Z).Normalize();
            return (norms, F);
        }

        //
        //
        static public double[] GenZeroArray(this int size)
        {
            double[] array = new double[size];
            for (int i = 0; i < size; i++)
                array[i] = 0;
            return array;
        }

        static public double[,] Gen2DZeroArray(this int size)
        {
            var array = new double[size, size];
            for (int c = 0; c < size; c++)
                for (int r = 0; r < size; r++)
                    array[r,c] = 0;
            return array;
        }
 
        static public double Sag(this double y, Side side, double RtolforZero = 0.001)
        {

            double sqrtvalue = 1 - (1 + side.K) * side.C * side.C * y * y;
            if (sqrtvalue < 0)
                return 0;
            else
                return (side.C * y * y / (1 + Math.Sqrt(sqrtvalue))) + side.AD * Math.Pow(y, 4) + side.AE * Math.Pow(y, 6);
        }

        static public double SagB(this double y, double R, double k = 0, double ad = 0, double ae = 0, double RtolforZero = 0.001)
        {
            double C = R.SetCurvature();
            double sqrtvalue = 1 - (1 + k) * C * C * y * y;
            if (sqrtvalue < 0)
                return 0;
            else
                return C * y * y / (1 + Math.Sqrt(sqrtvalue)) + ad * Math.Pow(y, 4) + ad * Math.Pow(y, 6);
        }

        static public double Sag2D(this PointD p, Side side, double RtolforZero = 0.001)
        {
            double C = 0;
            if (Math.Abs(side.R) > RtolforZero)
                C = 1 / side.R;

            double r2 = (p.X * p.X + p.Y * p.Y);
            double sqrtvalue = 1 - (1 + side.K) * C * C * r2;

            if (sqrtvalue < 0)
                return 0;
            else
                //return (C * (p.X * p.X + p.Y * p.Y) / (1 + Math.Sqrt(sqrtvalue))) + side.AD * Math.Pow((p.X * p.X + p.Y * p.Y), 2) + side.AE * Math.Pow((p.X * p.X + p.Y * p.Y), 3);
                return (C * r2 / (1 + Math.Sqrt(sqrtvalue))) + side.AD * r2 * r2 + side.AE * r2 * r2 * r2;
        }

        static public (Vector3D, double) Slope3D(this Vector3D P, Side s)
        {
            double p = P.X * P.X + P.Y * P.Y;
            double q0 = (P.Z - s.AD * p * p - s.AE * p * p * p);
            double q1 = (-4 * s.AD * p - 6 * s.AE * p * p);

            double dx = P.X * (-s.C - s.C * (s.K + 1) * q1 * q0 + q1);
            double dy = P.Y * (-s.C - s.C * (s.K + 1) * q1 * q0 + q1);
            double dz = 1 - s.C * (s.K + 1) * q0;

            var N = new Vector3D(dx, dy, dz);
            var n = N / N.Length();
            double F = -(s.C / 2) * p - (s.C / 2) * (s.K + 1) * q0 * q0 + q0;
            return (n, F);
        }

        static public double RadToDeg(this double xrad)
        {
            return (xrad * 180 / Math.PI);
        }

        static public double YscaleValue(this double y)
        {
            var ylog = Math.Log10(y); //-0.25884840114821
            int i = (int)ylog;  // 0
            var yt0 = ylog - i;  //-0.25884840114821
            var yt1 = Math.Pow(10, yt0) * 10;  // 5.51
            var yt2 = Math.Ceiling(yt1);  // 6.0
            var yt3 = yt2 / 10;
            var yt4 = Math.Log10(yt3) + i;
            var ysc = Math.Pow(10, yt4);  // 0.6
            return ysc;
        }


        public static double[] SliceDataMidPt(this double[,] data, int border = 0)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            int midrow = rows / 2;
            double[] odata = new double[cols];

            for (int c = 0; c < cols; c++)
            {
                double sum = 0;
                for (int r = midrow - border; r <= midrow + border; r++)
                    sum += data[r, c];
                odata[c] = sum / (1 + 2 * border);
            }

            return odata;           
        }

        public static (double Min, double Max) FindMinMax(this double[,] data)
        {
            int width = data.GetLength(0);
            int height = data.GetLength(1);

            double Min = 1e20;
            double Max = -1e20;

            for (int w = 0; w < width; w++)
                for (int h = 0; h < height; h++)
                {
                    if (data[w, h] < Min)
                        Min = data[w, h];
                    if (data[w, h] > Max)
                        Max = data[w, h];
                }

            return (Min, Max);
        }

        public static (double Min, double Max) MinMax1D(this double[] data)
        {
            int width = data.Count();

            double Min = 1e20;
            double Max = -1e20;

            for (int w = 0; w < width; w++)
                {
                    if (data[w] < Min)
                        Min = data[w];
                    if (data[w] > Max)
                        Max = data[w];
                }

            return (Min, Max);
        }


    }

}