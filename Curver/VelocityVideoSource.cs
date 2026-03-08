using System.Numerics;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.FileSource;
using YukkuriMovieMaker.Plugin.FileSource.FFmpeg;
using YukkuriMovieMaker.Plugin.FileSource.MediaFoundation;
using YukkuriMovieMaker.Plugin.Shape;

namespace Curver
{
    public class VelocityVideoSource : IShapeSource2
    {
        private readonly IGraphicsDevicesAndContext devices;
        private readonly VelocityVideoParameter parameter;
        private IVideoFileSource? innerSource;
        private string? currentFilePath;
        private readonly FFmpegVideoFileSourcePlugin ffmpegPlugin = new FFmpegVideoFileSourcePlugin();
        private readonly MFVideoFileSourcePlugin mfPlugin = new MFVideoFileSourcePlugin();
        private readonly DisposeCollector disposer = new DisposeCollector();
        // Cache for curve
        private string? cachedCurveData;
        private VelocityCurve? cachedCurve;

        private ID2D1CommandList? commandList;

        public VelocityVideoSource(IGraphicsDevicesAndContext devices, VelocityVideoParameter parameter)
        {
            this.devices = devices;
            this.parameter = parameter;
        }

        public ID2D1Image Output => (parameter.ShowGraph && commandList != null) ? commandList : (innerSource?.Output ?? GetEmptyBitmap());

        public IEnumerable<VideoController> Controllers => [];  

        private ID2D1Bitmap? emptyBitmap;
        private ID2D1Image GetEmptyBitmap()
        {
            if (emptyBitmap == null)
            {
                emptyBitmap = devices.DeviceContext.CreateEmptyBitmap(1920, 1080);
                disposer.Collect(emptyBitmap);
            }
            return emptyBitmap;
        }

        public void Update(TimelineItemSourceDescription desc)
        {
            if (currentFilePath != parameter.FilePath)
            {
                innerSource?.Dispose();
                innerSource = null;
                currentFilePath = parameter.FilePath;

                if (!string.IsNullOrEmpty(currentFilePath) && System.IO.File.Exists(currentFilePath))
                {
                    try 
                    {
                        innerSource = ffmpegPlugin.CreateVideoFileSource(devices, currentFilePath);
                    }
                    catch {  }

                    if (innerSource == null)
                    {
                        try
                        {
                            innerSource = mfPlugin.CreateVideoFileSource(devices, currentFilePath);
                        }
                        catch { }
                    }
                }
            }

            if (innerSource == null) return;

            if (cachedCurveData != parameter.CurveData)
            {
                cachedCurveData = parameter.CurveData;
                cachedCurve = VelocityCurve.Deserialize(cachedCurveData);
            }

            double normalizedTime = 0;
            if (desc.ItemDuration.Frame > 0)
            {
                normalizedTime = (double)desc.ItemPosition.Frame / desc.ItemDuration.Frame;
            }

            double distance = cachedCurve?.GetDistanceAt(normalizedTime) ?? normalizedTime * 100.0;
            double mappedSeconds = (distance / 100.0) * ((double)desc.ItemDuration.Frame / desc.FPS);
            mappedSeconds = Math.Max(0, mappedSeconds);

            innerSource.Update(TimeSpan.FromSeconds(mappedSeconds));

            if (parameter.ShowGraph && cachedCurve != null)
            {
                UpdateGraphOverlay();
            }
            else
            {
                commandList?.Dispose();
                commandList = null;
            }
        }

        private void UpdateGraphOverlay()
        {
            // Use local DeviceContext for thread safety as Update runs in parallel
            using var d2d = devices.DeviceContext.Device.CreateDeviceContext(Vortice.Direct2D1.DeviceContextOptions.None);
            
            var inputImage = innerSource?.Output ?? GetEmptyBitmap();
            
            // Get bounds
            var bounds = d2d.GetImageLocalBounds(inputImage);
            float width = bounds.Right - bounds.Left;
            float height = bounds.Bottom - bounds.Top;

            if (width <= 0 || height <= 0) return;

            // Recreate CommandList
            commandList?.Dispose();
            commandList = d2d.CreateCommandList();
            disposer.Collect(commandList);

            d2d.Target = commandList;
            d2d.BeginDraw();
            
            // Draw Video
            d2d.DrawImage(inputImage);

            using var brush = d2d.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.0f, 0.8f, 1.0f, 0.8f));
            using var brushZero = d2d.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1.0f, 1.0f, 1.0f, 0.4f));
            
            float strokeWidth = Math.Max(2, height / 200.0f);

            // Draw Zero line (0%) and Normal line (100%)
            float y0 = height - ((0 - (-100)) / 400.0f * height);
            d2d.DrawLine(new System.Numerics.Vector2(0, y0), new System.Numerics.Vector2(width, y0), brushZero, strokeWidth / 2);

            float y100 = height - ((100 - (-100)) / 400.0f * height);
            d2d.DrawLine(new System.Numerics.Vector2(0, y100), new System.Numerics.Vector2(width, y100), brushZero, strokeWidth / 2);

            // Draw Curve
            // Sample 100 points
            int segments = 100;
            System.Numerics.Vector2? prevPoint = null;

            for (int i = 0; i <= segments; i++)
            {
                double t = i / (double)segments;
                double speed = cachedCurve!.GetSpeedAt(t);
                
                float x = (float)(t * width);
                float y = height - (float)((speed - (-100)) / 400.0 * height);
                var currentPoint = new System.Numerics.Vector2(x, y);

                if (prevPoint.HasValue)
                {
                    d2d.DrawLine(prevPoint.Value, currentPoint, brush, strokeWidth);
                }
                prevPoint = currentPoint;
            }

            d2d.EndDraw();
            d2d.Target = null;
        }

        public void Dispose()
        {
            innerSource?.Dispose();
            disposer.DisposeAndClear();
        }
    }
}
