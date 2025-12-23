using OpenCvSharp;
using OpenCvSharp.Extensions;
using Quant.Helper.Common;
using Quant.Helper.Scripts.Abstractions;
using SharpHook.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using WindowsInput;

namespace Quant.Helper.Scripts;

internal class MineScript(ILogger logger, InputSimulator input) : LoopingScriptBase(KeyCode.VcU, "Каменяр", input)
{
    private readonly string[] _stoneImages = { "stone_1.jpg", "stone_2.jpg", "stone_3.jpg", "stone_4.jpg" };
    private const int ClickCount = 3;
    private const int SearchLoops = 9;
    private const double MatchThreshold = 0.8;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await PressEKey(2500, token);

        while (!token.IsCancellationRequested)
        {
            for (int loop = 0; loop < SearchLoops && !token.IsCancellationRequested; loop++)
            {
                await CheckAndClick(token);
                await Task.Delay(100, token);
            }

            if (!token.IsCancellationRequested)
            {
                await MoveBackAndForward(token);
            }
        }
    }

    private async Task CheckAndClick(CancellationToken token)
    {
        foreach (string imagePath in _stoneImages)
        {
            if (token.IsCancellationRequested) break;

            await ClickOnImage(imagePath, token);
        }
    }

    private async Task ClickOnImage(string imagePath, CancellationToken token)
    {
        try
        {
            string fullPath = Path.Combine("Assets", imagePath);

            if (!File.Exists(fullPath))
            {
                logger.Log($"[{Name}]: Зображення не знайдено: {fullPath}");
                return;
            }

            var matches = FindImageMatches(fullPath);

            if (matches.Count > 0)
            {
                logger.Log($"[{Name}]: Знайдено {matches.Count} збігів для {imagePath}");

                foreach (var match in matches)
                {
                    if (token.IsCancellationRequested) break;

                    for (int i = 0; i < ClickCount && !token.IsCancellationRequested; i++)
                    {
                        SetCursorPos(match.X, match.Y);
                        await Task.Delay(300, token);

                        input.Mouse.LeftButtonClick();
                        await Task.Delay(300, token);
                    }

                    logger.Log($"[{Name}]: Кліки виконано на позиції ({match.X}, {match.Y})");
                }

                await Task.Delay(500, token);
                SetCursorPos((int)SystemParameters.PrimaryScreenWidth / 2,
                            (int)SystemParameters.PrimaryScreenHeight / 2);
            }
        }
        catch (Exception ex)
        {
            logger.Log($"[{Name}]: Помилка при пошуку {imagePath}: {ex.Message}");
        }
    }

    private List<OpenCvSharp.Point> FindImageMatches(string templatePath)
    {
        var matches = new List<OpenCvSharp.Point>();

        try
        {
            using var templateColor = Cv2.ImRead(templatePath);
            if (templateColor.Empty())
            {
                logger.Log($"[{Name}]: Не вдалося завантажити шаблон: {templatePath}");
                return matches;
            }

            using var template = templateColor.CvtColor(ColorConversionCodes.BGR2GRAY);
            using var screenBmp = CaptureScreen();
            using var screenMat = screenBmp.ToMat();
            using var screenGray = screenMat.CvtColor(ColorConversionCodes.BGR2GRAY);

            using var resizedScreen = screenGray.Resize(new OpenCvSharp.Size(), 0.6, 0.6);
            using var resizedTemplate = template.Resize(new OpenCvSharp.Size(), 0.6, 0.6);

            matches = FindMatches(resizedScreen, resizedTemplate, MatchThreshold);

            for (int i = 0; i < matches.Count; i++)
            {
                matches[i] = new OpenCvSharp.Point((int)(matches[i].X / 0.6), (int)(matches[i].Y / 0.6));
            }

            matches = FilterOverlappingPoints(matches, 50.0);
        }
        catch (Exception ex)
        {
            logger.Log($"[{Name}]: Помилка OpenCV: {ex.Message}");
        }

        return matches;
    }

    private List<OpenCvSharp.Point> FindMatches(Mat screen, Mat template, double threshold)
    {
        var matches = new List<OpenCvSharp.Point>();

        using var result = new Mat();
        Cv2.MatchTemplate(screen, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

        while (true)
        {
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
                var centerPoint = new OpenCvSharp.Point(
                    maxLoc.X + template.Width / 2,
                    maxLoc.Y + template.Height / 2);

                matches.Add(centerPoint);

                Cv2.Rectangle(result,
                    new OpenCvSharp.Rect(maxLoc.X, maxLoc.Y, template.Width, template.Height),
                    new Scalar(0.0), -1);
            }
            else
            {
                break;
            }
        }

        return matches;
    }

    private List<OpenCvSharp.Point> FilterOverlappingPoints(IEnumerable<OpenCvSharp.Point> points, double minDistance)
    {
        var filtered = new List<OpenCvSharp.Point>();

        foreach (var point in points)
        {
            bool tooClose = filtered.Any(existing =>
                Math.Sqrt(Math.Pow(existing.X - point.X, 2.0) + Math.Pow(existing.Y - point.Y, 2.0)) < minDistance);

            if (!tooClose)
            {
                filtered.Add(point);
            }
        }

        return filtered;
    }

    private static Bitmap CaptureScreen()
    {
        int width = (int)SystemParameters.PrimaryScreenWidth;
        int height = (int)SystemParameters.PrimaryScreenHeight;
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    private async Task MoveBackAndForward(CancellationToken token)
    {
        input.Keyboard.KeyDown(VirtualKeyCode.VK_S);
        await Task.Delay(350, token);
        input.Keyboard.KeyUp(VirtualKeyCode.VK_S);

        await Task.Delay(200, token);

        input.Keyboard.KeyDown(VirtualKeyCode.VK_W);
        await Task.Delay(350, token);
        input.Keyboard.KeyUp(VirtualKeyCode.VK_W);

        await Task.Delay(600, token);

        await PressEKey(2500, token);
    }
}
