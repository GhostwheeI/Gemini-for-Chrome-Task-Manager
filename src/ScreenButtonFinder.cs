using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GeminiForChromeManager;

internal readonly struct ScreenButtonCandidate
{
    public ScreenButtonCandidate(int x, int y, int width, int height, int area)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Area = area;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Area { get; }

    public int CenterX => X + Width / 2;

    public int CenterY => Y + Height / 2;
}

internal static class ScreenButtonFinder
{
    public static ScreenButtonCandidate? FindBestLightBlueActionButton()
    {
        List<ScreenButtonCandidate> candidates = FindLightBlueActionButtons();
        return candidates.Count == 0 ? null : candidates[0];
    }

    private static List<ScreenButtonCandidate> FindLightBlueActionButtons()
    {
        Rectangle virtualScreen = SystemInformation.VirtualScreen;

        using Bitmap bitmap = new(virtualScreen.Width, virtualScreen.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                virtualScreen.Left,
                virtualScreen.Top,
                0,
                0,
                virtualScreen.Size);
        }

        List<ScreenButtonCandidate> candidates = FindCandidatesInBitmap(bitmap);

        for (int index = 0; index < candidates.Count; index++)
        {
            ScreenButtonCandidate candidate = candidates[index];
            candidates[index] = new ScreenButtonCandidate(
                candidate.X + virtualScreen.Left,
                candidate.Y + virtualScreen.Top,
                candidate.Width,
                candidate.Height,
                candidate.Area);
        }

        candidates.Sort(static (left, right) => right.Area.CompareTo(left.Area));
        return candidates;
    }

    private static List<ScreenButtonCandidate> FindCandidatesInBitmap(Bitmap bitmap)
    {
        Rectangle rectangle = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride;
            int byteCount = Math.Abs(stride) * bitmap.Height;
            byte[] pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, byteCount);

            bool[] mask = BuildLightBlueMask(bitmap.Width, bitmap.Height, stride, pixels);
            return FindConnectedButtonShapes(bitmap.Width, bitmap.Height, mask);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static bool[] BuildLightBlueMask(int width, int height, int stride, byte[] pixels)
    {
        bool[] mask = new bool[width * height];

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;

            for (int x = 0; x < width; x++)
            {
                int pixelOffset = rowOffset + x * 4;
                byte blue = pixels[pixelOffset];
                byte green = pixels[pixelOffset + 1];
                byte red = pixels[pixelOffset + 2];

                // Gemini's current approval button is a wide light-blue pill.
                // Shape filtering below keeps small icons and links out.
                bool isLightBlue =
                    red >= 120 && red <= 205 &&
                    green >= 160 && green <= 230 &&
                    blue >= 200 && blue <= 255 &&
                    blue - red >= 35 &&
                    blue - green >= 5;

                if (isLightBlue)
                {
                    mask[y * width + x] = true;
                }
            }
        }

        return mask;
    }

    private static List<ScreenButtonCandidate> FindConnectedButtonShapes(int width, int height, bool[] mask)
    {
        bool[] seen = new bool[mask.Length];
        List<ScreenButtonCandidate> candidates = [];
        int[] queueX = new int[mask.Length];
        int[] queueY = new int[mask.Length];
        int[] deltaX = [1, -1, 0, 0];
        int[] deltaY = [0, 0, 1, -1];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int startIndex = y * width + x;

                if (!mask[startIndex] || seen[startIndex])
                {
                    continue;
                }

                ScreenButtonCandidate? candidate = FloodFillCandidate(
                    x,
                    y,
                    width,
                    height,
                    mask,
                    seen,
                    queueX,
                    queueY,
                    deltaX,
                    deltaY);

                if (candidate is not null)
                {
                    candidates.Add(candidate.Value);
                }
            }
        }

        return candidates;
    }

    private static ScreenButtonCandidate? FloodFillCandidate(
        int startX,
        int startY,
        int width,
        int height,
        bool[] mask,
        bool[] seen,
        int[] queueX,
        int[] queueY,
        int[] deltaX,
        int[] deltaY)
    {
        int head = 0;
        int tail = 0;
        int minX = startX;
        int maxX = startX;
        int minY = startY;
        int maxY = startY;
        int area = 0;

        queueX[tail] = startX;
        queueY[tail] = startY;
        tail++;
        seen[startY * width + startX] = true;

        while (head < tail)
        {
            int currentX = queueX[head];
            int currentY = queueY[head];
            head++;
            area++;

            minX = Math.Min(minX, currentX);
            maxX = Math.Max(maxX, currentX);
            minY = Math.Min(minY, currentY);
            maxY = Math.Max(maxY, currentY);

            for (int direction = 0; direction < 4; direction++)
            {
                int nextX = currentX + deltaX[direction];
                int nextY = currentY + deltaY[direction];

                if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                {
                    continue;
                }

                int nextIndex = nextY * width + nextX;

                if (mask[nextIndex] && !seen[nextIndex])
                {
                    seen[nextIndex] = true;
                    queueX[tail] = nextX;
                    queueY[tail] = nextY;
                    tail++;
                }
            }
        }

        int candidateWidth = maxX - minX + 1;
        int candidateHeight = maxY - minY + 1;
        double aspectRatio = (double)candidateWidth / candidateHeight;

        if (candidateWidth >= 180 &&
            candidateHeight >= 30 &&
            candidateHeight <= 90 &&
            area >= 3000 &&
            aspectRatio >= 3.0)
        {
            return new ScreenButtonCandidate(minX, minY, candidateWidth, candidateHeight, area);
        }

        return null;
    }
}
