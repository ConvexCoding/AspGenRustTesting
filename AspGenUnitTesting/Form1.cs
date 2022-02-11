using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using gExtensions;
using gClass;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;

namespace AspGenUnitTesting
{
    public partial class Form1 : Form
    {
        static Lens lens = new Lens();
        public Form1()
        {
            InitializeComponent();
            lens.ap = 10;
            lens.BFL = 96.55091;
            lens.WL = 1.07;
            lens.n = 1.44966;
            //lens.Material = "Fused Silica";
            lens.Diameter = 25;
            lens.CT = 5.0;

            lens.Side1.R = 44.966;
            lens.Side1.C = lens.Side1.R.SetCurvature();
            lens.Side1.K = 0; // -0.57922;
            lens.Side1.AD = 0;
            lens.Side1.AE = 0;
            lens.Side1.Type = lens.Side1.SetSurfaceType();

            lens.Side2.R = -1000;
            lens.Side2.C = lens.Side2.R.SetCurvature();
            lens.Side2.K = 0;
            lens.Side2.AD = 0;
            lens.Side2.AE = 0;
            lens.Side2.Type = lens.Side2.SetSurfaceType();

            FirstOrderCalcs(lens);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PrintLens(lens);
            var v1 = new Vector3D(3, 4, 0);
            CalcSlopes(lens, new Vector3D(7, 4, 0));
            CalcSlopes(lens, new Vector3D(3, 8, 0));
            CalcSlopes(lens, new Vector3D(1, 1, 0));

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Random rm = new Random();
            var x = rm.NextDouble() * 8;
            var y = rm.NextDouble() * 8;
            Stopwatch w = new Stopwatch();
            var P0 = new Vector3D(x, y, 0);
            var E0 = new Vector3D(0, 0, 1);
            Side side = lens.Side2;
            w.Reset();
            w.Start();
            var P2 = RTM.TraceRayToSurface(P0, E0, side, 0.0);
            w.Stop();
            rtb.AppendText("Time Tics:   " + w.ElapsedTicks + " (" + w.ElapsedMilliseconds + ")\n");

            w.Reset();
            w.Start();
            var (N2, F) = P2.Slope3D(side);
            w.Stop();
            rtb.AppendText("Time Tics:   " + w.ElapsedTicks + " (" + w.ElapsedMilliseconds + ")\n");

            w.Reset();
            w.Start();
            var E2 = RTM.CalcDirectionSines(E0, N2, 1.0, lens.n);  // after refraction
            w.Stop();
            rtb.AppendText("Time Tics:   " + w.ElapsedTicks + " (" + w.ElapsedMilliseconds + ")\n\ns");

        }

        private void CalcSlopes(Lens lens, Vector3D v1)
        {
            Stopwatch watch = new Stopwatch();

            watch.Start();
            var (n, F) = v1.Slope3D(lens.Side2);
            watch.Stop();
            rtb.AppendText("n: " + n.ToString2() + ",   F:  " + F.ToString() + "\n");
            rtb.AppendText("Time Tics:   " + watch.ElapsedTicks + " (" + watch.ElapsedMilliseconds + ")");
            watch.Reset();
            rtb.AppendText("\n");

            watch.Start();
            (n, F) = v1.Slope3D(lens.Side1);
            watch.Stop();
            rtb.AppendText("n: " + n.ToString2() + ",   F:  " + F.ToString() + "\n");
            rtb.AppendText("Time Tics:   " + watch.ElapsedTicks + " (" + watch.ElapsedMilliseconds + ")");
            watch.Reset();
            rtb.AppendText("\n");
        }

        private void PrintLens(Lens lens)
        {
            string div = ", ";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Matl, CT, WL: " + "No Matl" + div + lens.CT.ToString("f4") + div + lens.WL.ToString("f4"));
            sb.AppendLine("Diameter, CA: " + lens.Diameter.ToString("f3") + div + (lens.ap * 2).ToString("f3"));
            Side s = lens.Side1;
            sb.AppendLine(s.R.ToString("F4") + div + s.C.ToString("F4") + div + s.K.ToString("f4") + div + s.AD.ToString("g4") + div + s.AE.ToString("g4"));
            s = lens.Side2;
            sb.AppendLine(s.R.ToString("F4") + div + s.C.ToString("F4") + div + s.K.ToString("f4") + div + s.AD.ToString("g4") + div + s.AE.ToString("g4"));
            sb.AppendLine();
            rtb.AppendText(sb.ToString() + "\n");
        }

        private Lens FirstOrderCalcs(Lens lensp)
        {
            double Phi = (lensp.n - 1) * (lensp.Side1.C - lensp.Side2.C + (lensp.n - 1) * lensp.CT * lensp.Side1.C * lensp.Side2.C / lensp.n);
            lens.EFL = 1 / Phi;
            lensp.EFL = 1 / Phi;
            double PrincPlane = ((lensp.n - 1) * lensp.Side1.C * lensp.EFL) * (1 / lensp.n) * lensp.CT;
            lens.BFL = lensp.EFL - PrincPlane;
            lensp.BFL = lensp.EFL - PrincPlane;
            return lensp;
        }

        double fiber_radius = 0.1;
        double Refocus = 0.0;

        public static Func<double, double, double, double, double> FermiDirac = (xp, beta, r50p, peak) => (peak / (1 + Math.Exp(beta * (Math.Abs(xp) / r50p - 1.0))));

        [DllImport(@"rustlib.dll", CallingConvention = CallingConvention.Cdecl)]
        //[DllImport(@"C:\Users\glher\source\repos\RustLGen\target\release\rustlib.dll", CallingConvention = CallingConvention.Cdecl)]
        private unsafe static extern bool gen_trace_rays(GenRays gr, Ray* ptr1, Ray* ptr2, UIntPtr numpts, Lens lens, double refocus);

        int totalrays = 1000000;
        int noofangles = 5;

        private void button3_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            FirstOrderCalcs(lens);
            double ppr = (double)totalrays * noofangles/ (Math.PI * 50 * 50);
            
            var angin = (fiber_radius / lens.EFL);

            Stopwatch w = new Stopwatch();

            w.Reset();
            w.Start();
            var vxyz = lens.ap.GenerateRandomRayVectors(totalrays);
            var Dvector = RTM.AddAnglesToVectors(lens, Refocus, vxyz, angin, noofangles);

            List<Vector3D> vlist = new List<Vector3D>();
            
            foreach (Ray r in Dvector)
            {
                var Pout = RTMPlus.Trace_3D_Ray(r.pvector, r.edir, lens, Refocus);
                vlist.Add(Pout.pvector);
            }

            w.Stop();
            rtb.AppendText("C# Gen & Trace:   " + w.ElapsedMilliseconds + " ms5\n");

            w.Reset();
            w.Start();
            var data = RTM.ProcessVlistData(vlist, lens, Refocus, fiber_radius);
            w.Stop();
            rtb.AppendText("Process Rays:  " + w.ElapsedMilliseconds + " ms\n\n");

            var ydata = data.SliceDataMidPt(border: 1);
            
            for (int i = 0; i < ydata.Length; i++)
                ydata[i] /= ppr;
            
            var xdata = GenerateXArray(ydata, fiber_radius);
            if (ydata != null)
                ChartData(chart1, lens, Refocus, xdata, ydata);
            setAxis(chart1.ChartAreas[0]);

            SendDataToClip(xdata, ydata);

            System.Windows.Forms.Cursor.Current = Cursors.Default;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            FirstOrderCalcs(lens);

            double ppr = (double)totalrays * noofangles / (Math.PI * 50 * 50);

            var angin = (fiber_radius / lens.EFL);

            Stopwatch w = new Stopwatch();

            w.Reset();
            w.Start();

            var npts = (ulong)(totalrays * noofangles);
            Ray[] rrout = new Ray[npts];
            Ray[] rrin = new Ray[npts];

            var gr = new GenRays((uint)totalrays, (uint) noofangles, lens.ap, angin);
            unsafe
            {
                fixed (Ray* ptr = rrin)
                fixed (Ray* ptr2 = rrout)
                {
                    var good = gen_trace_rays(gr, ptr, ptr2, (UIntPtr)rrin.Length, lens, Refocus);
                }
            }

            w.Stop();
            rtb2.AppendText("Rust Gen & Trace:   " + w.ElapsedMilliseconds + " ms\n");

            //Show3DImage simage = new Show3DImage(rrout);
            //simage.ShowDialog();

            w.Reset();
            w.Start();
            var data = RTM.ProcessRayData(rrout, lens, Refocus, fiber_radius);
            w.Stop();
            rtb2.AppendText("Process Rays:  " + w.ElapsedMilliseconds + " ms\n\n");

            var ydata = data.SliceDataMidPt(border: 1);
            for (int i = 0; i < ydata.Length; i++)
                ydata[i] /= ppr;

            List<Ray> rlist = rrout.ToList();
            double maxbin = rlist.Max(point => point.pvector.X);
            double minbin = rlist.Min(point => point.pvector.X);
            maxbin = maxbin.YscaleValue();
            double binsize = (maxbin - minbin) / 200;
            double binsPermm = 1 / binsize;

            var xdata = GenerateXArray(ydata, fiber_radius);

            if (ydata != null)
                ChartData(chart2, lens, Refocus, xdata, ydata);
            setAxis(chart2.ChartAreas[0]);
            SendDataToClip(xdata, ydata);
            System.Windows.Forms.Cursor.Current = Cursors.Default;
        }

        private void SendDataToClip(double[] xdata, double[] ydata)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ydata.Count(); i++)
                sb.AppendLine(xdata[i].ToString() + ", " + ydata[i].ToString());
            Clipboard.SetText(sb.ToString());
        }


        private double[] GenerateXArray(double[] ydata, double fiber_radius)
        {
            int s = ydata.Length;
            double[] x = new double[s];
            var xmin = -2 * fiber_radius;
            var xmax = 2 * fiber_radius;
            double stepsize = (xmax - xmin) / (s - 1);
            for (int i = 0; i < s; i++)
                x[i] = xmin + i * stepsize;

            return x;
        }

        private void ChartData(Chart chart, Lens lens, double Refocus, double[] xs, double[] ys)
        {
            int s = ys.Length;

            chart.Series[0].Points.DataBindXY(xs, ys);

            var color = Color.LightGray;
            chart.ChartAreas[0].AxisX.MajorGrid.LineColor = color;
            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = color;

            chart.ChartAreas[0].AxisX.Title = "Vertical (Y-Axis) Cross Section (mm)";
            chart.ChartAreas[0].AxisY.Title = "Relative Intensity";

            chart.ChartAreas[0].RecalculateAxesScale();
        }

        private void setAxis(ChartArea ca)
        {
            ca.AxisX.Minimum = -0.2;
            ca.AxisX.Maximum = 0.2;
            ca.AxisX.Interval = 0.05;

            //ca.AxisY.Minimum = 0.0;
            //ca.AxisY.Maximum = 1.0;
            //ca.AxisY.Interval = 0.2;

        }


    }
}
