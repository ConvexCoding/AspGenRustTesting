using System;

using gClass;
using gExtensions;
using System.Data;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace AspGenUnitTesting
{
    public static class RTM
    {
        public static string formstr(int i, double y, double x, double z, double m, double aoi, double aor, double norm)
        {
            string deform = "f6";
            string line = i.ToString() + ", " +
                          y.ToString(deform) + ", " +
                          x.ToString(deform) + ", " +
                          z.ToString(deform) + ", " +
                          m.ToString(deform) + ", " +
                          aoi.RadToDeg().ToString(deform) + ", " +
                          aor.RadToDeg().ToString(deform) + ", " +
                          norm.ToString(deform) + "\n";
            return line;
        }

        public static Func<double, double, double> Hypo = (x, y) => (Math.Sqrt(x * x + y * y));

        public static Func<double, double, double> Hypo2 = (x, y) => (x * x + y * y);


        // ray trace methods - 3D 

        public static Vector3D Trace_3D(Vector3D P0, Vector3D E0, Lens lens_in, double Refocus)
        {
            // D or P are position vectors
            // C center points of surfaces 
            // E are ray direction cosine vectors
            // N are surface normals
            // Z is the zero vector used as place holder in data table

            //var P2 = TraceRayToSide1_A(P0, E0, lens);
            var P2 = TraceRayToSurface(P0, E0, lens_in.Side1, 0.0);
            var (N2, F) = P2.Slope3D(lens_in.Side1);
            var E2  = CalcDirectionSines(E0, N2, 1.0, lens_in.n);  // after refraction


            // Trace to Surface 2 after refraction
            var P3 = TraceRayToSurface(P2, E2, lens_in.Side2, lens_in.CT);
            var (N3, F3) = new Vector3D(P3.X, P3.Y, P3.Z - lens_in.CT).Slope3D(lens_in.Side2);  // adjust z for CT of lens
            var E3 = CalcDirectionSines(E2, N3, lens_in.n, 1);


            // transfer ray to image plane
            var P4 = TranslateZ_Flat(P3, E3, lens_in.CT + lens_in.BFL + Refocus);
            //var E5 = E3;
            //var N5 = new Vector3D(0, 0, 1);
            //var aoi5 = Math.Acos(Vector3D.DotProduct(E3, N5)).RadToDeg();
            return P4;
        }

        public static List<Ray> AddAnglesToVectors(Lens lens, double Refocus, List<Vector3D> vin, double angin, int noofangles)
        {
            List<Ray> Rlist = new List<Ray>();
            double dirx, diry, dirz;
            Random rd = new Random();
            for (int i = 0; i < vin.Count; i++)
            {
                for (int j = 0; j < noofangles; j++)
                {
                    dirx = angin * (rd.NextDouble() * 2 - 1);
                    diry = angin * (rd.NextDouble() * 2 - 1);
                    while (RTM.Hypo(dirx, diry) > angin)
                    {
                        dirx = angin * (rd.NextDouble() * 2 - 1);
                        diry = angin * (rd.NextDouble() * 2 - 1);
                    }
                    dirz = Math.Sqrt(1 - dirx * dirx - diry * diry);
                    Rlist.Add(new Ray(vin[i], new Vector3D(dirx, diry, dirz)));
                }
            }
            return Rlist;
        }

        public static double[,] ProcessRayData(Ray[] rin, Lens lens, double Refocus, double fiber_radius)
        {
            /*
            double binsize = 0.002;
            double binsPermm = 1 / binsize;
            double minbin = -2 * fiber_radius;
            double maxbin = 2 * fiber_radius;
            */
            List<Ray> rlist = rin.ToList();
            double maxbin = 2 * fiber_radius;
            double minbin = -maxbin;
            double steps = 201;
            double binsize = (maxbin - minbin) / steps;

            var indata = ((int)steps).Gen2DZeroArray();
            int errors = 0;
            foreach (Ray P in rin)
            {
                int row = (int)Math.Round((P.pvector.X - minbin) / binsize);
                int col = (int)Math.Round((P.pvector.Y - minbin) / binsize);
                if ((row >= 0) && (row < steps) && (col >= 0) && (col < steps))
                         indata[row, col] += 1;
                else
                    errors++;
            }
            return indata;
        }

        public static double[,] ProcessVlistData(List<Vector3D> Vlist, Lens lens, double Refocus, double fiber_radius)
        {
            double maxbin = 2 * fiber_radius;
            double minbin = -maxbin;
            double steps = 201;
            double binsize = (maxbin - minbin) / steps;

            var indata = ((int)steps).Gen2DZeroArray();
            int errors = 0;
            foreach (Vector3D P in Vlist)
            {
                int row = (int)Math.Round((P.X - minbin) / binsize);
                int col = (int)Math.Round((P.Y - minbin) / binsize);
                if ((row >= 0) && (row < steps) && (col >= 0) && (col < steps))
                {
                    indata[row, col] += 1;
                }
                else
                    errors++;
            }
            return indata;
        }

        static public (double Y3, double Zend, double LSA, double AOI3) CalcLSA(this double y0, Lens lens_in, double refocus)
        {
            // this methods should be optimized as much as possible since mapping WFE in 2D requires a little HP
            // 
            Vector3D P0 = new Vector3D(0, y0, 0);
            Vector3D E0 = new Vector3D(0, 0, 1);

            // P1 reserved for later expansion by placing entrance pupil before lens to mimic S.L.'s
            // Trace to Side 1 after Refraction
            var P2 = TraceRayToSurface(P0, E0, lens_in.Side1, 0.0);
            var (N2, F) = P2.Slope3D(lens_in.Side1);
            var E2 = CalcDirectionSines(E0, N2, 1.0, lens_in.n);  // after refraction


            // Trace to Surface 2 after refraction
            var P3 = TraceRayToSurface(P2, E2, lens_in.Side2, lens_in.CT);
            var (N3, F3) = new Vector3D(P3.X, P3.Y, P3.Z - lens_in.CT).Slope3D(lens_in.Side2);  // adjust z for CT of lens
            var E3 = CalcDirectionSines(E2, N3, lens_in.n, 1);


            // transfer ray to image plane
            var P4 = TranslateZ_Flat(P3, E3, lens_in.CT + lens_in.BFL + refocus);
            var aoi4 = Math.Acos(Vector3D.DotProduct(E3, new Vector3D(0, 0, 1)));
            double lsa2 = P4.Y / Math.Tan(aoi4);

            return (P4.Y, P4.Z, lsa2, aoi4);  // calls to this function expect radians
        }


        // Tracing support
        public static Vector3D CalcDirectionSines(Vector3D E, Vector3D N, double nin, double nout)
        {
            var alpha = Vector3D.DotProduct(E, N);
            //var aoi = Math.Acos(alpha).RadToDeg();
            //var aor = Math.Asin(Math.Sin(Math.Acos(alpha)) * nin / nout).RadToDeg();

            double a = 1.0;
            double b = 2 * alpha;
            double c = (1 - (nout * nout) / (nin * nin));
            var sol2 = (-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            var Ep = E + sol2 * N;
            Ep /= Ep.Length();
            return Ep;
        }

        public static Vector3D TraceRayToSurface(Vector3D D, Vector3D E, Side side, double plane = 0.0)
        {
            if (side.Type == 0)
            {
                return TranslateZ_Flat(D, E, plane);
            }

            double zest1 = (new PointD(D.X, D.Y)).Sag2D(side) + plane;
            double u = (zest1 - D.Z) / E.Z;
            var P1 = D;
            var P2 = D + u * E;

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs((P1 - P2).Length()) > 1e-4)
                {
                    P1 += P2 - P1;
                    zest1 = (new PointD(P1.X, P1.Y)).Sag2D(side) + plane;
                    u = (zest1 - D.Z) / E.Z;
                    P2 = D + u * E;
                }
                else
                    break;
            }

            return P2;
        }

        private static Vector3D TranslateZ_Sphere(Vector3D D, Vector3D E, Side s)
        {
            Vector3D C = new Vector3D(0, 0, s.R);

            var a = E.LengthSquared();
            var b = Vector3D.DotProduct((2 * E), (D - C));
            var c = (D - C).LengthSquared() - s.R * s.R;

            var temp = b * b - 4 * a * c;

            var sol1 = (-b - Math.Sqrt(temp)) / (2 * a);
            //var sol2 = (-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a);

            Vector3D Dp = D + sol1 * E;
            return Dp;
        }

        public static Vector3D TranslateZ_Flat(Vector3D P, Vector3D E, double zplane)
        {
            var u = (zplane - P.Z) / E.Z;
            Vector3D Pp = P + u * E;
            return Pp;
        }


        /*
         P = P1 + ((zplane - P1.Z) / E1.Z) * E1 = P1 + (zplane/E1.Z - P1.Z/E1.Z)  * E1 = P1 + (zplane * E1)/E1.Z - (P1.Z * E1)/E1.Z


        P1 + (zplane * E1)/E1.Z - (P1.Z * E1)/E1.Z = P2 + (zplane * E2)/E2.Z - (P2.Z * E2)/E2.Z

        (zplane * E1)/E1.Z - (zplane * E2)/E2.Z = (P2 - P1) - (P2.Z * E2)/E2.Z + (P1.Z * E1)/E1.Z
        zplane * (E1/E1.Z - E2/E2.Z) = (P2 - P1) - (P2.Z * E2)/E2.Z + (P1.Z * E1)/E1.Z

        zplane = (P2 - P1 - (P2.Z/E2.Z)*E2 + (P1.Z/E1.Z)*E1) / (E1/E1.Z - E2/E2.Z)

         P = P2 + ((zplane - P2.Z) / E2.Z) * E2 


         */
    }
}