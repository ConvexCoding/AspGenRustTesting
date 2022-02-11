using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using gClass;
using gExtensions;
using gGraphExt;


namespace AspGenUnitTesting
{
    public partial class Show3DImage : Form
    {
         public Show3DImage(Ray[] Rin)
        {
            InitializeComponent();
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

            var cp = Properties.Resources.rainbow.LoadColorPallete2();
            cp[cp.Length - 1] = cp[cp.Length - 2];  // get rid of white pixels

            // setup binning by first calculating max extent of data and setting number of bins
            List<Ray> rlist = Rin.ToList();
            double maxbin = rlist.Max(point => point.pvector.X);
            maxbin = maxbin.YscaleValue();
            int numbins = 201;

            var indata = ProcessRays(Rin, numbins, maxbin);

            UpdatePixBox(indata, cp, numbins, maxbin);

        }

        private void UpdatePixBox(double[,] data, Color[] cp, int sbins, double maxbin)
        {
            Bitmap b = gGraphExt.gGraphExt.DoubleToBitmap(data, cp);

            using (var e = Graphics.FromImage(b))
            {
                Pen pen = new Pen(Color.White, 3);
                pen.StartCap = LineCap.ArrowAnchor;
                pen.EndCap = LineCap.ArrowAnchor;
                e.DrawLine(pen, new Point(0, 20), new Point(b.Width - 2, 20));

                string dimens = (2 * maxbin).ToString("f3") + " mm";
                Rectangle rect = new Rectangle(0, 0, b.Width, 40);
                StringFormat format = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                };

                e.SmoothingMode = SmoothingMode.AntiAlias;
                e.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.DrawString(dimens, new Font("Tahoma", 12), Brushes.White, rect, format);
            }

            pb.Image = b as Image;
        }

        public static double[,] ProcessRays(Ray[] Rin, int sbins, double maxbin)
        {
            double binsize = 2.0 * maxbin / (double)(sbins - 1);
            double binsPermm = 1 / binsize;

            var indata = sbins.Gen2DZeroArray();
            int errors = 0;
            foreach (Ray P in Rin)
            {
                int row = (int)Math.Round((P.pvector.X + maxbin) / binsize);
                int col = (int)Math.Round((P.pvector.Y + maxbin) / binsize);
                if ((row >= 0) && (row < sbins) && (col >= 0) && (col < sbins))
                {
                    indata[row, col] += 1;
                }
                else
                    errors++;
            }
            return indata;
        }
    }
}
