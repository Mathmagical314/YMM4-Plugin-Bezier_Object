using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using YukkuriMovieMaker.Commons;

namespace Curver
{
    public partial class CurveEditorControl : UserControl, INotifyPropertyChanged
    {
        private VelocityCurve curve;
        private VelocityKeyframe? draggedKeyframe;
        private Point dragStartPoint;
        private const double KeyframeRadius = 6;
        private const double HandleRadius = 4;

        public event EventHandler? CurveChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand SavePresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand AddKeyframeCommand { get; }

        public ObservableCollection<string> PresetNames { get; } = new ObservableCollection<string>();

        private string selectedPresetName = "Linear";
        public string SelectedPresetName
        {
            get => selectedPresetName;
            set
            {
                if (selectedPresetName != value)
                {
                    selectedPresetName = value;
                    OnPropertyChanged();
                    ApplyPreset(value);
                }
            }
        }

        private readonly Dictionary<string, string> customPresets = new Dictionary<string, string>();
        private readonly string presetFilePath;

        public CurveEditorControl()
        {
            InitializeComponent();
            DataContext = this;

            curve = new VelocityCurve();

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pluginData = System.IO.Path.Combine(appData, "YukkuriMovieMaker4", "Plugins", "Curver");
            Directory.CreateDirectory(pluginData);
            presetFilePath = System.IO.Path.Combine(pluginData, "user_presets.json");

            LoadPresets();

            SavePresetCommand = new ActionCommand(_ => true, _ => SaveCurrentPreset());
            DeletePresetCommand = new ActionCommand(_ => true, _ => DeleteCurrentPreset());
            AddKeyframeCommand = new ActionCommand(_ => true, _ => AddKeyframeAtCenter());

            SizeChanged += (s, e) => RedrawCurve();
            Loaded += (s, e) => RedrawCurve();
        }

        private void LoadPresets()
        {
            PresetNames.Clear();
            PresetNames.Add("Linear");
            PresetNames.Add("EaseIn");
            PresetNames.Add("EaseOut");
            PresetNames.Add("EaseInOut");
            PresetNames.Add("EaseOutIn");

            if (File.Exists(presetFilePath))
            {
                try
                {
                    var json = File.ReadAllText(presetFilePath);
                    var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (saved != null)
                    {
                        foreach (var kvp in saved)
                        {
                            customPresets[kvp.Key] = kvp.Value;
                            PresetNames.Add(kvp.Key);
                        }
                    }
                }
                catch { }
            }
        }

        private void SavePresets()
        {
            try
            {
                var json = JsonSerializer.Serialize(customPresets);
                File.WriteAllText(presetFilePath, json);
            }
            catch { }
        }

        private void SaveCurrentPreset()
        {
            if (string.IsNullOrWhiteSpace(SelectedPresetName)) return;
            
            var data = curve.Serialize();
            customPresets[SelectedPresetName] = data;
            
            if (!PresetNames.Contains(SelectedPresetName))
            {
                PresetNames.Add(SelectedPresetName);
            }
            
            SavePresets();
            MessageBox.Show($"Preset '{SelectedPresetName}' saved.");
        }

        private void DeleteCurrentPreset()
        {
            if (customPresets.ContainsKey(SelectedPresetName))
            {
                customPresets.Remove(SelectedPresetName);
                PresetNames.Remove(SelectedPresetName);
                SavePresets();
                SelectedPresetName = "Linear";
            }
        }

        public void SetCurve(VelocityCurve newCurve)
        {
            curve = newCurve;
            RedrawCurve();
        }

        public VelocityCurve GetCurve()
        {
            return curve;
        }

        private void ApplyPreset(string presetName)
        {
           if (customPresets.TryGetValue(presetName, out var data))
           {
               curve = VelocityCurve.Deserialize(data);
               curve.Preset = CurvePreset.Custom;
               RedrawCurve();
               CurveChanged?.Invoke(this, EventArgs.Empty);
               return;
           }

            curve.Preset = presetName switch
            {
                "Linear" => CurvePreset.Linear,
                "EaseIn" => CurvePreset.EaseIn,
                "EaseOut" => CurvePreset.EaseOut,
                "EaseInOut" => CurvePreset.EaseInOut,
                "EaseOutIn" => CurvePreset.EaseOutIn,
                _ => CurvePreset.Custom
            };

            RedrawCurve();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        private void AddKeyframeAtCenter()
        {
            curve.Preset = CurvePreset.Custom;
            SelectedPresetName = "Custom"; 
            
            var newKeyframe = new VelocityKeyframe
            {
                Time = 0.5,
                Speed = 100.0,
                HandleOut = 0,
                HandleIn = 0
            };
            
            int insertIndex = curve.Keyframes.Count;
            for (int i = 0; i < curve.Keyframes.Count; i++)
            {
                if (curve.Keyframes[i].Time > newKeyframe.Time)
                {
                    insertIndex = i;
                    break;
                }
            }
            curve.Keyframes.Insert(insertIndex, newKeyframe);

            RedrawCurve();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RedrawCurve()
        {
            if (CurveCanvas == null || GridCanvas == null)
                return;

            double width = CurveCanvas.ActualWidth;
            double height = CurveCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            CurveCanvas.Children.Clear();
            GridCanvas.Children.Clear();

            DrawGrid(width, height);
            DrawCurveLine(width, height);
            DrawKeyframes(width, height);
        }
        
        private const double MinSpeed = 000.0;
        private const double MaxSpeed = 500.0;
        private double SpeedRange => MaxSpeed - MinSpeed;

        private void DrawGrid(double width, double height)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));

            for (int i = 0; i <= 10; i++)
            {
                double x = width * i / 10.0;
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = gridBrush,
                    StrokeThickness = i % 5 == 0 ? 1 : 0.5
                };
                GridCanvas.Children.Add(line);
            }

            for (double s = MinSpeed; s <= MaxSpeed; s += 50)
            {
                double y = SpeedToY(s, height);
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = (s % 100 == 0) ? 1 : 0.5
                };
                GridCanvas.Children.Add(line);
            }

            double y100 = SpeedToY(100.0, height);
            var centerLine = new Line
            {
                X1 = 0,
                Y1 = y100,
                X2 = width,
                Y2 = y100,
                Stroke = new SolidColorBrush(Color.FromArgb(120, 0, 180, 240)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            GridCanvas.Children.Add(centerLine);

            double y0 = SpeedToY(0.0, height);
            var zeroLine = new Line
            {
                X1 = 0,
                Y1 = y0,
                X2 = width,
                Y2 = y0,
                Stroke = new SolidColorBrush(Color.FromArgb(120, 240, 180, 0)),
                StrokeThickness = 1.5
            };
            GridCanvas.Children.Add(zeroLine);
        }

        private void DrawCurveLine(double width, double height)
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();

            double startY = SpeedToY(curve.GetSpeedAt(0), height);
            pathFigure.StartPoint = new Point(0, startY);

            const int segments = 100;
            for (int i = 1; i <= segments; i++)
            {
                double t = i / (double)segments;
                double speed = curve.GetSpeedAt(t);
                double x = TimeToX(t, width);
                double y = SpeedToY(speed, height);
                
                pathFigure.Segments.Add(new LineSegment(new Point(x, y), true));
            }

            pathGeometry.Figures.Add(pathFigure);

            var curvePath = new System.Windows.Shapes.Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 180, 240)),
                StrokeThickness = 2.5,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 180, 240),
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.6
                }
            };

            CurveCanvas.Children.Add(curvePath);
        }

        private void DrawKeyframes(double width, double height)
        {
            foreach (var kf in curve.Keyframes)
            {
                double x = TimeToX(kf.Time, width);
                double y = SpeedToY(kf.Speed, height);

                var keyframeEllipse = new Ellipse
                {
                    Width = KeyframeRadius * 2,
                    Height = KeyframeRadius * 2,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    StrokeThickness = 2,
                    Tag = kf,
                    Cursor = Cursors.Hand,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 4,
                        ShadowDepth = 2,
                        Opacity = 0.5
                    }
                };

                Canvas.SetLeft(keyframeEllipse, x - KeyframeRadius);
                Canvas.SetTop(keyframeEllipse, y - KeyframeRadius);

                CurveCanvas.Children.Add(keyframeEllipse);
            }
        }

        private double TimeToX(double time, double width) => time * width;
        private double SpeedToY(double speed, double height) => height - ((speed - MinSpeed) / SpeedRange * height);
        private double XToTime(double x, double width) => Math.Clamp(x / width, 0, 1);
        private double YToSpeed(double y, double height) => Math.Clamp(((height - y) / height * SpeedRange) + MinSpeed, MinSpeed, MaxSpeed);

        private void CurveCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(CurveCanvas);

            foreach (var child in CurveCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is VelocityKeyframe kf)
                {
                    var ellipsePos = new Point(Canvas.GetLeft(ellipse) + KeyframeRadius, Canvas.GetTop(ellipse) + KeyframeRadius);
                    double distance = Math.Sqrt(Math.Pow(pos.X - ellipsePos.X, 2) + Math.Pow(pos.Y - ellipsePos.Y, 2));

                    if (distance <= KeyframeRadius * 2)
                    {
                        draggedKeyframe = kf;
                        dragStartPoint = pos;
                        CurveCanvas.CaptureMouse();
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void CurveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(CurveCanvas);

            double time = XToTime(pos.X, CurveCanvas.ActualWidth);
            double speed = YToSpeed(pos.Y, CurveCanvas.ActualHeight);
            InfoText.Text = $"Time: {time:P0} | Speed: {speed:F0}%";

            if (draggedKeyframe != null && e.LeftButton == MouseButtonState.Pressed)
            {
                curve.Preset = CurvePreset.Custom;

                bool isFirstOrLast = draggedKeyframe == curve.Keyframes.First() || draggedKeyframe == curve.Keyframes.Last();

                if (!isFirstOrLast)
                {
                    draggedKeyframe.Time = XToTime(pos.X, CurveCanvas.ActualWidth);
                }

                draggedKeyframe.Speed = YToSpeed(pos.Y, CurveCanvas.ActualHeight);

                RedrawCurve();
                e.Handled = true;
            }
        }

        private void CurveCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedKeyframe != null)
            {
                CurveChanged?.Invoke(this, EventArgs.Empty);
                draggedKeyframe = null;
                CurveCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void CurveCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(CurveCanvas);

            foreach (var child in CurveCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is VelocityKeyframe kf)
                {
                    var ellipsePos = new Point(Canvas.GetLeft(ellipse) + KeyframeRadius, Canvas.GetTop(ellipse) + KeyframeRadius);
                    double distance = Math.Sqrt(Math.Pow(pos.X - ellipsePos.X, 2) + Math.Pow(pos.Y - ellipsePos.Y, 2));

                    if (distance <= KeyframeRadius * 2)
                    {
                        if (kf != curve.Keyframes.First() && kf != curve.Keyframes.Last())
                        {
                            curve.Preset = CurvePreset.Custom;
                            curve.Keyframes.Remove(kf);
                            RedrawCurve();
                            CurveChanged?.Invoke(this, EventArgs.Empty);
                        }
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
