using System;

using gClass;
using gExtensions;
using System.Data;
using System.Drawing;
using System.Collections.Generic;

namespace AspGenUnitTesting
{
    public static class RTMPlus
    {

        // ray trace methods - 3D 
        public static Vector3D Trace_3D(Vector3D P0, Vector3D E0, Lens lens_in, double Refocus)
        {
            // D or P are position vectors
            // C center points of surfaces 
            // E are ray direction cosine vectors
            // N are surface normals
            // Z is the zero vector used as place holder in data table

            //var P2 = TraceRayToSide1_A(P0, E0, lens);
            var P2 = TraceToSurf(P0, E0, lens_in.Side1, 0.0);
            var (N2, F) = P2.CalcSlope3D(lens_in.Side1);
            //var (E2, aoi2, aor2) = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction
            var E2 = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction


            // Trace to Surface 2 after refraction
            var P3 = TraceToSurf(P2, E2, lens_in.Side2, lens_in.CT);
            var (N3, F3) = new Vector3D(P3.X, P3.Y, P3.Z - lens_in.CT).CalcSlope3D(lens_in.Side2);  // adjust z for CT of lens
            //var (E3, aoi3, aor3) = CalcDirSines(E2, N3, lens_in.n, 1);
            var E3 = CalcDirSines(E2, N3, lens_in.n, 1);


            // transfer ray to image plane
            var P4 = TranslateToFlatSurf(P3, E3, lens_in.CT + lens_in.BFL + Refocus);
            //var E5 = E3;
            //var N5 = new Vector3D(0, 0, 1);
            //var aoi5 = Math.Acos(Vector3D.DotProduct(E3, N5)).RadToDeg();
            return P4;
        }

        public static (Vector3D Vout, Vector3D Cout) Trace_3D_Plus(Vector3D P0, Vector3D E0, Lens lens_in, double Refocus)
        {
            // D or P are position vectors
            // C center points of surfaces 
            // E are ray direction cosine vectors
            // N are surface normals
            // Z is the zero vector used as place holder in data table

            //var P2 = TraceRayToSide1_A(P0, E0, lens);
            var P2 = TraceToSurf(P0, E0, lens_in.Side1, 0.0);
            var (N2, F) = P2.CalcSlope3D(lens_in.Side1);
            //var (E2, aoi2, aor2) = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction
            var E2 = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction


            // Trace to Surface 2 after refraction
            var P3 = TraceToSurf(P2, E2, lens_in.Side2, lens_in.CT);
            var (N3, F3) = new Vector3D(P3.X, P3.Y, P3.Z - lens_in.CT).CalcSlope3D(lens_in.Side2);  // adjust z for CT of lens
            //var (E3, aoi3, aor3) = CalcDirSines(E2, N3, lens_in.n, 1);
            var E3 = CalcDirSines(E2, N3, lens_in.n, 1);


            // transfer ray to image plane
            var P4 = TranslateToFlatSurf(P3, E3, lens_in.CT + lens_in.BFL + Refocus);
            //var E5 = E3;
            //var N5 = new Vector3D(0, 0, 1);
            //var aoi5 = Math.Acos(Vector3D.DotProduct(E3, N5)).RadToDeg();
            return (P4, E3);
        }

        public static Ray Trace_3D_Ray(Vector3D P0, Vector3D E0, Lens lens_in, double Refocus)
        {
            // D or P are position vectors
            // C center points of surfaces 
            // E are ray direction cosine vectors
            // N are surface normals
            // Z is the zero vector used as place holder in data table

            //var P2 = TraceRayToSide1_A(P0, E0, lens);
            var P2 = TraceToSurf(P0, E0, lens_in.Side1, 0.0);
            var (N2, F) = P2.CalcSlope3D(lens_in.Side1);
            //var (E2, aoi2, aor2) = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction
            var E2 = CalcDirSines(E0, N2, 1.0, lens_in.n);  // after refraction


            // Trace to Surface 2 after refraction
            var P3 = TraceToSurf(P2, E2, lens_in.Side2, lens_in.CT);
            var (N3, F3) = new Vector3D(P3.X, P3.Y, P3.Z - lens_in.CT).CalcSlope3D(lens_in.Side2);  // adjust z for CT of lens
            //var (E3, aoi3, aor3) = CalcDirSines(E2, N3, lens_in.n, 1);
            var E3 = CalcDirSines(E2, N3, lens_in.n, 1);


            // transfer ray to image plane
            var P4 = TranslateToFlatSurf(P3, E3, lens_in.CT + lens_in.BFL + Refocus);
            //var E5 = E3;
            //var N5 = new Vector3D(0, 0, 1);
            //var aoi5 = Math.Acos(Vector3D.DotProduct(E3, N5)).RadToDeg();
            return new Ray(P4, E3);
        }


        // Tracing support
        public static Vector3D CalcDirSines(Vector3D E, Vector3D N, double nin, double nout)
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
            //return (Ep, aoi, aor);
        }

        private static Vector3D TraceToSurf(Vector3D D, Vector3D E, Side side, double plane = 0.0)
        {
            if (side.Type == 0)
            {
                return TranslateToFlatSurf(D, E, plane);
            }

            double zest1 = CalcSag3D(D.X, D.Y, side) + plane;
            double u = (zest1 - D.Z) / E.Z;
            var P1 = D;
            var P2 = D + u * E;

            for (int i = 0; i < 10; i++)
            {
                if ((P1 - P2).Length() > 1e-4)
                {
                    P1 = P2;
                    zest1 = CalcSag3D(P1.X, P1.Y, side) + plane;
                    u = (zest1 - D.Z) / E.Z;
                    P2 = D + u * E;
                }
                else
                    break;
            }

            return P2;
        }

        private static Vector3D TranslateToFlatSurf(Vector3D P, Vector3D E, double zplane)
        {
            var u = (zplane - P.Z) / E.Z;
            Vector3D Pp = P + u * E;
            return Pp;
        }

        static public (Vector3D, double) CalcSlope3D(this Vector3D P, Side s)
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

        static public double CalcSag3D(double x, double y, Side side, double RtolforZero = 0.001)
        {
            double C = 0;
            if (Math.Abs(side.R) > RtolforZero)
                C = 1 / side.R;

            double r2 = (x * x + y * y);
            double sqrtvalue = 1 - (1 + side.K) * C * C * r2;

            if (sqrtvalue < 0)
                return 0;
            else
                return (C * r2 / (1 + Math.Sqrt(sqrtvalue))) + side.AD * r2 * r2 + side.AE * r2 * r2 * r2;
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