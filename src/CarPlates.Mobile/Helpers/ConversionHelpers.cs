namespace CarPlates.Mobile.Helpers;

public static class ConversionHelpers
{
    public static float ToFloat(double value) => (float)value;

    public static PointF ToPointF(double x, double y) => new((float)x, (float)y);

    public static SizeF ToSizeF(double width, double height) => new((float)width, (float)height);

    public static RectF ToRectF(double x, double y, double width, double height) =>
        new((float)x, (float)y, (float)width, (float)height);
}
