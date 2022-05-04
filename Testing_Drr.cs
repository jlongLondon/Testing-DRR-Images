using ExecutableLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;


[assembly: ESAPIScript(IsWriteable = true)]

namespace Testing_DRR_Images
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Perform(app, args);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                Console.Read();
            }
        }
        public static void Perform(Application app, string[] args)
        {
            ScriptContextArgs ctx = ScriptContextArgs.From(args);
            Console.WriteLine(ctx.PlanSetupId);
            Patient patient = app.OpenPatientById(ctx.PatientId);
            Course course = patient.Courses.First(e => e.Id == ctx.CourseId);
            PlanSetup plan = course.PlanSetups.First(e => e.Id == ctx.PlanSetupId);
            List<Beam> beams = plan.Beams.Where(b => !b.IsSetupField).OrderBy(b => b.BeamNumber).ToList();

            patient.BeginModifications();
     
            foreach(Beam beam in beams)
            {
                VMS.TPS.Common.Model.API.Image Drr = beam.ReferenceImage;
                WriteableBitmap drr = BuildDRRImage(beam,Drr);
                SourcetoPng(drr);
                System.Drawing.Image png = System.Drawing.Image.FromFile(@"\\Tevirari002\va_data$\ProgramData\Vision\PublishedScripts\TemporaryImages\Drr.png");
                Bitmap drr_fieldLines = FieldLines(beam, png, Drr, plan);
                png.Dispose();
                drr_fieldLines.Save(@"\\Tevirari002\va_data$\ProgramData\Vision\PublishedScripts\TemporaryImages\Drr_Field" + beam.Id +".png", ImageFormat.Png);
            }


            Console.ReadKey();
            app.ClosePatient();


        }

        public static void SourcetoPng(BitmapSource bmp)
        {
            using (var fileStream = new FileStream(@"\\Tevirari002\va_data$\ProgramData\Vision\PublishedScripts\TemporaryImages\Drr.png", FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(fileStream);
                fileStream.Close();
            }
        }

        private static WriteableBitmap BuildDRRImage(Beam beam, VMS.TPS.Common.Model.API.Image Drr)
        {
            if (beam.ReferenceImage == null) {  return null; }
            
            
            int[,] pixels = new int[Drr.YSize, Drr.XSize];
            Drr.GetVoxels(0, pixels);
            int[] flat_pixels = new int[Drr.YSize * Drr.XSize];

            for (int i = 0; i < Drr.YSize; i++)
            {
                for (int j = 0; j < Drr.XSize; j++)
                {
                    flat_pixels[i + Drr.XSize * j] = pixels[i, j];
                }
            }

            var Drr_max = flat_pixels.Max();
            var Drr_min = flat_pixels.Min();

            System.Windows.Media.PixelFormat format = PixelFormats.Gray8;
            int stride = (Drr.XSize * format.BitsPerPixel + 7) / 8;
            byte[] image_bytes = new byte[stride * Drr.YSize];

            for (int i = 0; i < flat_pixels.Length; i++)
            {
                double value = flat_pixels[i];
                image_bytes[i] = Convert.ToByte(255 * ((value - Drr_min) / (Drr_max - Drr_min)));
            }

            BitmapSource source = BitmapSource.Create(Drr.XSize, Drr.YSize, 25.4 / Drr.XRes, 25.4 / Drr.YRes, format, null, image_bytes, stride);

            WriteableBitmap writeableBitmap = new WriteableBitmap(source);

            return writeableBitmap;
            
        }


        private static Bitmap FieldLines(Beam beam, System.Drawing.Image source, VMS.TPS.Common.Model.API.Image Drr, PlanSetup plan)
        {
            Bitmap bitmap = new Bitmap(source);

            //Isocentre = center of image.
            int Centre_X = Drr.XSize / 2;
            int Centre_Y = Drr.YSize / 2;

            // Shifts to user orgin.
            int X_offset = (int)beam.IsocenterPosition.y;
            int Y_offset = (int)beam.IsocenterPosition.z;
            Console.WriteLine(X_offset.ToString());

            // User origin.
            int user_origin_X = Centre_X + X_offset;
            int user_origin_Y = Centre_Y + Y_offset;

            double collimatorAngle = (beam.ControlPoints.FirstOrDefault().CollimatorAngle)*(Math.PI/180.00);

            VRect<double> jawPositions = beam.ControlPoints.FirstOrDefault().JawPositions;

            double Y2_CenterX = Centre_X - ( jawPositions.Y2 * Math.Sin(collimatorAngle));
            double Y2_CenterY = Centre_Y - ( jawPositions.Y2 * Math.Cos(collimatorAngle));

            double Y1_CenterX= Centre_X - ( jawPositions.Y1 * Math.Sin(collimatorAngle));
            double Y1_CenterY = Centre_Y - (jawPositions.Y1 * Math.Cos(collimatorAngle));


            double Y2_UpperX = Y2_CenterX + jawPositions.X2 * Math.Cos(collimatorAngle);
            double Y2_UpperY = Y2_CenterY - jawPositions.X2 * Math.Sin(collimatorAngle);

            double Y2_LowerX = Y2_CenterX + jawPositions.X1 * Math.Cos(collimatorAngle);
            double Y2_LowerY = Y2_CenterY - jawPositions.X1 * Math.Sin(collimatorAngle);

            double Y1_UpperX = Y1_CenterX + jawPositions.X2 * Math.Cos(collimatorAngle);
            double Y1_UpperY = Y1_CenterY - jawPositions.X2 * Math.Sin(collimatorAngle);

            double Y1_LowerX = Y1_CenterX + jawPositions.X1 * Math.Cos(collimatorAngle);
            double Y1_LowerY = Y1_CenterY - jawPositions.X1 * Math.Sin(collimatorAngle);

            int Graticule_Length = (int)(Math.Round(Drr.XSize * 2 / 100d, 0) * 100);

            int Graticule_PositiveX = (int)(Graticule_Length * Math.Sin(collimatorAngle));
            int Graticule_PositiveY = (int)(Graticule_Length * Math.Cos(collimatorAngle));


            System.Drawing.Pen fieldPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1);
            System.Drawing.Pen isoPen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            System.Drawing.Pen graticulePen = new System.Drawing.Pen(System.Drawing.Color.Yellow, (float)0.5);

            bitmap.SetPixel(user_origin_X, user_origin_Y, System.Drawing.Color.YellowGreen);
            bitmap.SetPixel(user_origin_X - 1, user_origin_Y, System.Drawing.Color.YellowGreen);
            bitmap.SetPixel(user_origin_X + 1, user_origin_Y, System.Drawing.Color.YellowGreen);
            bitmap.SetPixel(user_origin_X, user_origin_Y - 1, System.Drawing.Color.Red);
            bitmap.SetPixel(user_origin_X, user_origin_Y + 1, System.Drawing.Color.Red);


            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawLine(fieldPen, (int)Y2_LowerX, (int)Y2_LowerY, (int)Y2_UpperX, (int)Y2_UpperY);
                graphics.DrawLine(fieldPen, (int)Y1_LowerX, (int)Y1_LowerY, (int)Y1_UpperX, (int)Y1_UpperY);
                graphics.DrawLine(fieldPen, (int)Y2_LowerX, (int)Y2_LowerY, (int)Y1_LowerX, (int)Y1_LowerY);
                graphics.DrawLine(fieldPen, (int)Y2_UpperX, (int)Y2_UpperY, (int)Y1_UpperX, (int)Y1_UpperY);

                graphics.DrawEllipse(isoPen, Centre_X - 5, Centre_Y - 5, 10, 10);

                graphics.DrawLine(graticulePen,(Centre_X + Graticule_PositiveX),(Centre_Y + Graticule_PositiveY), (Centre_X - Graticule_PositiveX), (Centre_Y - Graticule_PositiveY));
                graphics.DrawLine(graticulePen,(Centre_X + Graticule_PositiveY), (Centre_Y - Graticule_PositiveX), (Centre_X - Graticule_PositiveY), (Centre_Y + Graticule_PositiveX));

                for (int A = -Graticule_Length; A < Graticule_Length; A+=10)
                {
                    double Marker_CenterX1 = Centre_X + (A * Math.Sin(collimatorAngle));
                    double Marker_CenterY1 = Centre_Y + (A * Math.Cos(collimatorAngle));
                    double Marker_CenterX2 = Centre_X + (A * Math.Cos(collimatorAngle));
                    double Marker_CenterY2 = Centre_Y - (A * Math.Sin(collimatorAngle));


                    if (A % 50 ==0)
                    {
                        double Marker_Cosine = (10 * Math.Cos(collimatorAngle));
                        double Marker_Sine = (10 * Math.Sin(collimatorAngle));
                        graphics.DrawLine(graticulePen, (int)(Marker_CenterX1 + Marker_Cosine), (int)(Marker_CenterY1 - Marker_Sine), (int)(Marker_CenterX1 - Marker_Cosine), (int)(Marker_CenterY1 + Marker_Sine));
                        graphics.DrawLine(graticulePen, (int)(Marker_CenterX2 + Marker_Sine), (int)(Marker_CenterY2 + Marker_Cosine), (int)(Marker_CenterX2 - Marker_Sine), (int)(Marker_CenterY2 - Marker_Cosine));

                    }
                    else
                    {
                        double Marker_Cosine = (5 * Math.Cos(collimatorAngle));
                        double Marker_Sine = (5 * Math.Sin(collimatorAngle));
                        graphics.DrawLine(graticulePen, (int)(Marker_CenterX1 + Marker_Cosine), (int)(Marker_CenterY1 - Marker_Sine), (int)(Marker_CenterX1 - Marker_Cosine), (int)(Marker_CenterY1 + Marker_Sine));
                        graphics.DrawLine(graticulePen, (int)(Marker_CenterX2 + Marker_Sine), (int)(Marker_CenterY2 + Marker_Cosine), (int)(Marker_CenterX2 - Marker_Sine), (int)(Marker_CenterY2 - Marker_Cosine));

                    }


                }


            }

            bitmap.SetPixel((int)Y2_CenterX, (int)Y2_CenterY, System.Drawing.Color.Orange);


            return bitmap;

        }

    }
}
