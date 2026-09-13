namespace WindowMover;

internal static class GeometryMapper
{
    internal static readonly Size MinimumWindowSize = new(320, 200);

    public static Rectangle MapToMonitor(
        Rectangle windowBounds,
        Rectangle sourceWorkArea,
        Rectangle destinationWorkArea)
    {
        if (destinationWorkArea.Width <= 0 || destinationWorkArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationWorkArea));
        }

        if (sourceWorkArea.Width <= 0 || sourceWorkArea.Height <= 0)
        {
            return CenterAndClamp(windowBounds.Size, destinationWorkArea);
        }

        var relativeX = (double)(windowBounds.Left - sourceWorkArea.Left) / sourceWorkArea.Width;
        var relativeY = (double)(windowBounds.Top - sourceWorkArea.Top) / sourceWorkArea.Height;
        var relativeWidth = (double)windowBounds.Width / sourceWorkArea.Width;
        var relativeHeight = (double)windowBounds.Height / sourceWorkArea.Height;

        var minimumWidth = Math.Min(MinimumWindowSize.Width, destinationWorkArea.Width);
        var minimumHeight = Math.Min(MinimumWindowSize.Height, destinationWorkArea.Height);
        var width = Math.Clamp(
            (int)Math.Round(relativeWidth * destinationWorkArea.Width),
            minimumWidth,
            destinationWorkArea.Width);
        var height = Math.Clamp(
            (int)Math.Round(relativeHeight * destinationWorkArea.Height),
            minimumHeight,
            destinationWorkArea.Height);

        var x = destinationWorkArea.Left + (int)Math.Round(relativeX * destinationWorkArea.Width);
        var y = destinationWorkArea.Top + (int)Math.Round(relativeY * destinationWorkArea.Height);

        x = Math.Clamp(x, destinationWorkArea.Left, destinationWorkArea.Right - width);
        y = Math.Clamp(y, destinationWorkArea.Top, destinationWorkArea.Bottom - height);

        return new Rectangle(x, y, width, height);
    }

    private static Rectangle CenterAndClamp(Size size, Rectangle workArea)
    {
        var width = Math.Clamp(size.Width, 1, workArea.Width);
        var height = Math.Clamp(size.Height, 1, workArea.Height);
        var x = workArea.Left + (workArea.Width - width) / 2;
        var y = workArea.Top + (workArea.Height - height) / 2;
        return new Rectangle(x, y, width, height);
    }
}
