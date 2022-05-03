/*
private static Bitmap FieldLines(Beam beam, Bitmap drr_Non_indexed)
{
    VRect<double> jawPositions = beam.ControlPoints.FirstOrDefault().JawPositions;
    double minJawX = Math.Min(jawPositions.X1, jawPositions.X2);
    double maxJawX = Math.Max(jawPositions.X1, jawPositions.X2);

    double minJawY = Math.Min(jawPositions.Y1, jawPositions.Y2);
    double maxJawY = Math.Max(jawPositions.Y1, jawPositions.Y2);

    int xViewSize = (int)Math.Abs(minJawX - maxJawX);
    int yViewSize = (int)Math.Abs(minJawY - maxJawY);

    drr_Non_indexed.SetPixel(xViewSize, yViewSize, System.Drawing.Color.Red);
    drr_Non_indexed.SetPixel(xViewSize - 1, yViewSize, System.Drawing.Color.Red);
    drr_Non_indexed.SetPixel(xViewSize + 1, yViewSize, System.Drawing.Color.Red);
    drr_Non_indexed.SetPixel(xViewSize, yViewSize - 1, System.Drawing.Color.Red);
    drr_Non_indexed.SetPixel(xViewSize, yViewSize + 1, System.Drawing.Color.Red);

    Graphics gLineField = Graphics.FromImage(drr_Non_indexed);

    Point fieldUpperLeft = new System.Drawing.Point(xViewSize - (int)Math.Abs(jawPositions.X1), yViewSize - (int)Math.Abs(jawPositions.Y2));
    Point fieldUpperRight = new System.Drawing.Point(xViewSize + (int)Math.Abs(jawPositions.X2), yViewSize - (int)Math.Abs(jawPositions.Y2));
    Point fieldLowerLeft = new System.Drawing.Point(xViewSize - (int)Math.Abs(jawPositions.X1), yViewSize + (int)Math.Abs(jawPositions.Y1));
    Point fieldLowerRight = new System.Drawing.Point(xViewSize + (int)Math.Abs(jawPositions.X2), yViewSize + (int)Math.Abs(jawPositions.Y1));

    System.Drawing.Pen fieldPen = new System.Drawing.Pen(System.Drawing.Color.Red, 5);

    gLineField.DrawLine(fieldPen, fieldUpperLeft, fieldUpperRight);
    gLineField.DrawLine(fieldPen, fieldUpperRight, fieldLowerRight);
    gLineField.DrawLine(fieldPen, fieldLowerRight, fieldLowerLeft);
    gLineField.DrawLine(fieldPen, fieldLowerLeft, fieldUpperLeft);
    gLineField.Flush();

    return drr_Non_indexed;
}
*/