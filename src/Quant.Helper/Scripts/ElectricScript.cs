using OpenCvSharp;
using OpenCvSharp.Extensions;
using Quant.Helper.Common;
using Quant.Helper.Scripts.Abstractions;
using SharpHook.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using WindowsInput;
namespace Quant.Helper.Scripts;

internal class ElectricScript(ILogger logger, InputSimulator input) : LoopingScriptBase(KeyCode.VcQ, "Електрик", input)
{
    private readonly InputSimulator _input = input;
    private readonly string _templateResourceName = "damaged_fuse_template.jpg";

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await PressEKey(500, token);
        await Task.Delay(2000);
        Mat? template;
        Bitmap? screenBmp;
        Mat? screenMat;
        Mat? screenGray;
        Mat? resizedScreen;
        Mat? resizedTemplate;
        List<OpenCvSharp.Point> matches = new List<OpenCvSharp.Point>();
        byte[] templateBytes = ResourceHelper.GetEmbeddedResource(_templateResourceName);
        using (Mat templateColor = Cv2.ImDecode(templateBytes, ImreadModes.Color))
        {
            template = templateColor.CvtColor(ColorConversionCodes.BGR2GRAY);
            screenBmp = ElectricScript.CaptureScreen();
            screenMat = screenBmp.ToMat();
            screenGray = screenMat.CvtColor(ColorConversionCodes.BGR2GRAY);
            resizedScreen = screenGray.Resize(new OpenCvSharp.Size(), 0.6, 0.6);
            resizedTemplate = template.Resize(new OpenCvSharp.Size(), 0.6, 0.6);
            try
            {
                matches = FilterOverlappingPoints(FindMatches(resizedScreen, resizedTemplate, 0.9), 20.0);
                await Task.Delay(3000);
                logger.Log($"[{Name}]: Знайшов {matches.Count} елементів");
                for (int i = 0; i < matches.Count && !token.IsCancellationRequested; ++i)
                {
                    OpenCvSharp.Point point = matches[i];
                    ElectricScript.SetCursorPos((int)((double)point.X / 0.6), (int)((double)point.Y / 0.6));
                    ClickAt();
                    await Task.Delay(500, token);
                }
                logger.Log($"[{Name}]: Завершив працю");
                ElectricScript.SetCursorPos(0, 0);
            }
            finally
            {
                resizedTemplate?.Dispose();
                resizedScreen?.Dispose();
                screenGray?.Dispose();
                screenMat?.Dispose();
                screenBmp?.Dispose();
                template?.Dispose();
            }
        }

        template = null;
        screenBmp = null;
        screenMat = null;
        screenGray = null;
        resizedScreen = null;
        resizedTemplate = null;
        matches = new List<OpenCvSharp.Point>();
    }

    private List<OpenCvSharp.Point> FindMatches(Mat screen, Mat template, double threshold)
    {
        List<OpenCvSharp.Point> matches = new List<OpenCvSharp.Point>();
        using (Mat mat = new Mat())
        {
            Cv2.MatchTemplate((InputArray)screen, (InputArray)template, (OutputArray)mat, TemplateMatchModes.CCoeffNormed);
            Cv2.Threshold((InputArray)mat, (OutputArray)mat, threshold, 1.0, ThresholdTypes.Tozero);
            while (true)
            {
                double maxVal;
                OpenCvSharp.Point maxLoc;
                Cv2.MinMaxLoc((InputArray)mat, out double _, out maxVal, out OpenCvSharp.Point _, out maxLoc);
                if (maxVal >= threshold)
                {
                    OpenCvSharp.Point point = new(maxLoc.X + template.Width / 2, maxLoc.Y + template.Height / 2);
                    matches.Add(point);
                    logger.Log($"[Електрий]: Знайшов: X:{point.X} Y:{point.Y}");
                    Cv2.Rectangle(mat, new OpenCvSharp.Rect(maxLoc.X, maxLoc.Y, template.Width, template.Height), new Scalar(0.0), -1);
                }
                else
                    break;
            }
            return matches;
        }
    }

    private List<OpenCvSharp.Point> FilterOverlappingPoints(IEnumerable<OpenCvSharp.Point> points, double minDistance)
    {
        List<OpenCvSharp.Point> pointList = new List<OpenCvSharp.Point>();
        foreach (OpenCvSharp.Point point in points)
        {
            OpenCvSharp.Point p = point;
            if (Enumerable.All(pointList, existing => Math.Sqrt(Math.Pow(existing.X - p.X, 2.0) + Math.Pow(existing.Y - p.Y, 2.0)) >= minDistance))
                pointList.Add(p);
        }
        return pointList;
    }

    private static Bitmap CaptureScreen()
    {
        int primaryScreenWidth = (int)SystemParameters.PrimaryScreenWidth;
        int num = (int)SystemParameters.PrimaryScreenHeight * 2;
        Bitmap bitmap = new Bitmap(primaryScreenWidth, num, (PixelFormat)2498570);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(primaryScreenWidth, num), CopyPixelOperation.SourceCopy);
            return bitmap;
        }
    }

    private void ClickAt() => _input.Mouse.LeftButtonClick();

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
}
