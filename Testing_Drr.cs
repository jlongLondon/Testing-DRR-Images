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

// I am testing the use of git.

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
                Bitmap drr_fieldLines = FieldLines(beam, png, Drr);
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


        private static Bitmap FieldLines(Beam beam, System.Drawing.Image source, VMS.TPS.Common.Model.API.Image Drr)
        {
            Bitmap bitmap = new Bitmap(source);

            //Isocentre = center of image.
            int Centre_X = Drr.XSize / 2;
            int Centre_Y = Drr.YSize / 2;

            // Shifts to user orgin.
            int X_offset = (int)beam.IsocenterPosition.y;
            int Y_offset = (int)beam.IsocenterPosition.z;

            // User origin.
            int user_origin_X = Centre_X + X_offset;
            int user_origin_Y = Centre_Y + Y_offset;

            double collimatorAngle = (beam.ControlPoints.FirstOrDefault().CollimatorAngle)*(Math.PI/180.00);

            VRect<double> jawPositions = beam.ControlPoints.FirstOrDefault().JawPositions;
            double minJawX = Math.Min(jawPositions.X1, jawPositions.X2);
            double maxJawX = Math.Max(jawPositions.X1, jawPositions.X2);

            double minJawY = Math.Min(jawPositions.Y1, jawPositions.Y2);
            double maxJawY = Math.Max(jawPositions.Y1, jawPositions.Y2);

            double MinY_centerx = Centre_X - ( maxJawY * Math.Sin(collimatorAngle));
            double MinY_centery = Centre_Y - ( maxJawY * Math.Cos(collimatorAngle));

            double MaxY_centerx = Centre_X + ( minJawY * Math.Sin(collimatorAngle));
            double MaxY_centery = Centre_Y + (minJawY * Math.Cos(collimatorAngle));

            Console.WriteLine(string.Format("{0},{1}", MinY_centerx , MinY_centery));

            double MinY_upperx = MinY_centerx + maxJawX * Math.Cos(collimatorAngle);
            double MinY_uppery = MinY_centery - maxJawX * Math.Sin(collimatorAngle);

            double MinY_lowerx = MinY_centerx + minJawX * Math.Cos(collimatorAngle);
            double MinY_lowery = MinY_centery - minJawX * Math.Sin(collimatorAngle);

            double MaxY_upperx = MaxY_centerx + maxJawX * Math.Cos(collimatorAngle);
            double MaxY_uppery = MaxY_centery - maxJawX * Math.Sin(collimatorAngle);

            double MaxY_lowerx = MaxY_centerx + minJawX * Math.Cos(collimatorAngle);
            double MaxY_lowery = MaxY_centery - minJawX * Math.Sin(collimatorAngle);


            bitmap.SetPixel(user_origin_X, user_origin_Y, System.Drawing.Color.Red);
            bitmap.SetPixel(user_origin_X - 1, user_origin_Y, System.Drawing.Color.Red);
            bitmap.SetPixel(user_origin_X + 1, user_origin_Y, System.Drawing.Color.Red);
            bitmap.SetPixel(user_origin_X, user_origin_Y - 1, System.Drawing.Color.Red);
            bitmap.SetPixel(user_origin_X, user_origin_Y + 1, System.Drawing.Color.Red);

            bitmap.SetPixel(Centre_X, Centre_Y, System.Drawing.Color.Blue);
            bitmap.SetPixel(Centre_X - 1, Centre_Y, System.Drawing.Color.Blue);
            bitmap.SetPixel(Centre_X + 1, Centre_Y, System.Drawing.Color.Blue);
            bitmap.SetPixel(Centre_X, Centre_Y - 1, System.Drawing.Color.Blue);
            bitmap.SetPixel(Centre_X, Centre_Y + 1, System.Drawing.Color.Blue);

            bitmap.SetPixel((int)MinY_centerx, (int)MinY_centery, System.Drawing.Color.Orange);

            
            System.Drawing.Pen fieldPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 3);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawLine(fieldPen, (int)MinY_lowerx, (int)MinY_lowery, (int)MinY_upperx, (int)MinY_uppery);
                graphics.DrawLine(fieldPen, (int)MaxY_lowerx, (int)MaxY_lowery, (int)MaxY_upperx, (int)MaxY_uppery);
                graphics.DrawLine(fieldPen, (int)MinY_lowerx, (int)MinY_lowery, (int)MaxY_lowerx, (int)MaxY_lowery);
                graphics.DrawLine(fieldPen, (int)MinY_upperx, (int)MinY_uppery, (int)MaxY_upperx, (int)MaxY_uppery);

            }

            bitmap.SetPixel((int)MinY_centerx, (int)MinY_centery, System.Drawing.Color.Orange);

            return bitmap;

        }

    }
}
