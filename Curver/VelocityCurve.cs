using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YukkuriMovieMaker.Commons;

namespace Curver
{
    public enum CurvePreset
    {
        [Description("カスタム")]
        Custom,
        [Description("リニア")]
        Linear,
        [Description("イーズイン")]
        EaseIn,
        [Description("イーズアウト")]
        EaseOut,
        [Description("イーズインアウト")]
        EaseInOut,
        [Description("イーズアウトイン")]
        EaseOutIn
    }

    public class VelocityCurve : INotifyPropertyChanged
    {
        private CurvePreset preset = CurvePreset.Linear;
        private ObservableCollection<VelocityKeyframe> keyframes;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CurvePreset Preset
        {
            get => preset;
            set
            {
                if (preset != value)
                {
                    preset = value;
                    OnPropertyChanged();
                    ApplyPreset();
                }
            }
        }

        public ObservableCollection<VelocityKeyframe> Keyframes
        {
            get => keyframes;
            set
            {
                keyframes = value;
                OnPropertyChanged();
            }
        }

        public VelocityCurve()
        {
            keyframes = new ObservableCollection<VelocityKeyframe>
            {
                new VelocityKeyframe { Time = 0.0, Speed = 100.0 },
                new VelocityKeyframe { Time = 1.0, Speed = 100.0 }
            };
        }

        public double GetSpeedAt(double normalizedTime)
        {
            if (Keyframes.Count == 0)
                return 100.0;

            if (normalizedTime <= Keyframes[0].Time)
                return Keyframes[0].Speed;

            if (normalizedTime >= Keyframes[^1].Time)
                return Keyframes[^1].Speed;

            for (int i = 0; i < Keyframes.Count - 1; i++)
            {
                var kf1 = Keyframes[i];
                var kf2 = Keyframes[i + 1];

                if (normalizedTime >= kf1.Time && normalizedTime <= kf2.Time)
                {
                    double t = (normalizedTime - kf1.Time) / (kf2.Time - kf1.Time);
                    return HermiteInterpolation(kf1, kf2, t);
                }
            }

            return 100.0;
        }

        private double HermiteInterpolation(VelocityKeyframe kf1, VelocityKeyframe kf2, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            double m0 = kf1.HandleOut;
            double m1 = kf2.HandleIn;

            return h00 * kf1.Speed + h10 * m0 + h01 * kf2.Speed + h11 * m1;
        }

        private void ApplyPreset()
        {
            switch (Preset)
            {
                case CurvePreset.Linear:
                    Keyframes.Clear();
                    Keyframes.Add(new VelocityKeyframe { Time = 0.0, Speed = 100.0, HandleOut = 0, HandleIn = 0 });
                    Keyframes.Add(new VelocityKeyframe { Time = 1.0, Speed = 100.0, HandleOut = 0, HandleIn = 0 });
                    break;

                case CurvePreset.EaseIn:
                    Keyframes.Clear();
                    Keyframes.Add(new VelocityKeyframe { Time = 0.0, Speed = 0.0, HandleOut = 0, HandleIn = 0 });
                    Keyframes.Add(new VelocityKeyframe { Time = 1.0, Speed = 100.0, HandleOut = 100, HandleIn = 0 });
                    break;

                case CurvePreset.EaseOut:
                    Keyframes.Clear();
                    Keyframes.Add(new VelocityKeyframe { Time = 0.0, Speed = 100.0, HandleOut = 0, HandleIn = -100 });
                    Keyframes.Add(new VelocityKeyframe { Time = 1.0, Speed = 0.0, HandleOut = 0, HandleIn = 0 });
                    break;

                case CurvePreset.EaseInOut:
                    Keyframes.Clear();
                    Keyframes.Add(new VelocityKeyframe { Time = 0.0, Speed = 0.0, HandleOut = 0, HandleIn = 0 });
                    Keyframes.Add(new VelocityKeyframe { Time = 0.5, Speed = 100.0, HandleOut = 50, HandleIn = 50 });
                    Keyframes.Add(new VelocityKeyframe { Time = 1.0, Speed = 0.0, HandleOut = 0, HandleIn = 0 });
                    break;

                case CurvePreset.EaseOutIn:
                    Keyframes.Clear();
                    Keyframes.Add(new VelocityKeyframe { Time = 0.0, Speed = 100.0, HandleOut = 0, HandleIn = 0 });
                    Keyframes.Add(new VelocityKeyframe { Time = 0.5, Speed = 0.0, HandleOut = -50, HandleIn = -50 });
                    Keyframes.Add(new VelocityKeyframe { Time = 1.0, Speed = 100.0, HandleOut = 0, HandleIn = 0 });
                    break;

                case CurvePreset.Custom:
                    break;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Serialize()
        {
            var parts = new List<string> { ((int)Preset).ToString() };
            foreach (var kf in Keyframes)
            {
                parts.Add($"{kf.Time:F4},{kf.Speed:F2},{kf.HandleOut:F2},{kf.HandleIn:F2}");
            }
            return string.Join(";", parts);
        }

        public static VelocityCurve Deserialize(string data)
        {
            var curve = new VelocityCurve();
            if (string.IsNullOrWhiteSpace(data))
                return curve;

            var parts = data.Split(';');
            if (parts.Length < 1)
                return curve;

            if (int.TryParse(parts[0], out int presetValue))
            {
                curve.preset = (CurvePreset)presetValue;
            }

            if (parts.Length > 1)
            {
                curve.Keyframes.Clear();
                for (int i = 1; i < parts.Length; i++)
                {
                    var kfData = parts[i].Split(',');
                    if (kfData.Length == 4 &&
                        double.TryParse(kfData[0], out double time) &&
                        double.TryParse(kfData[1], out double speed) &&
                        double.TryParse(kfData[2], out double handleOut) &&
                        double.TryParse(kfData[3], out double handleIn))
                    {
                        curve.Keyframes.Add(new VelocityKeyframe
                        {
                            Time = time,
                            Speed = speed,
                            HandleOut = handleOut,
                            HandleIn = handleIn
                        });
                    }
                }
            }

            return curve;
        }

        public double GetDistanceAt(double normalizedTime)
        {
            if (Keyframes.Count == 0) return normalizedTime * 100.0;
            if (Keyframes.Count == 1) return normalizedTime * Keyframes[0].Speed;

            normalizedTime = Math.Clamp(normalizedTime, 0.0, 1.0);

            double totalDistance = 0;
            var sortedKeyframes = Keyframes.OrderBy(k => k.Time).ToList();

            if (normalizedTime < sortedKeyframes[0].Time)
            {
                return normalizedTime * sortedKeyframes[0].Speed;
            }
            
            totalDistance += sortedKeyframes[0].Time * sortedKeyframes[0].Speed;

            for (int i = 0; i < sortedKeyframes.Count - 1; i++)
            {
                var p0 = sortedKeyframes[i];
                var p1 = sortedKeyframes[i + 1];

                if (normalizedTime <= p1.Time)
                {
                    double t = (normalizedTime - p0.Time) / (p1.Time - p0.Time);
                    totalDistance += IntegrateSegment(p0, p1, t) * (p1.Time - p0.Time);
                    return totalDistance;
                }
                else
                {
                    totalDistance += IntegrateSegment(p0, p1, 1.0) * (p1.Time - p0.Time);
                }
            }

            var last = sortedKeyframes.Last();
            if (normalizedTime > last.Time)
            {
                totalDistance += (normalizedTime - last.Time) * last.Speed;
            }

            return totalDistance;
        }

        private double IntegrateSegment(VelocityKeyframe p0, VelocityKeyframe p1, double t)
        {
            double m0 = p0.HandleOut;
            double m1 = p1.HandleIn;

            double t2 = t * t;
            double t3 = t2 * t;
            double t4 = t3 * t;

            double intH00 = 0.5 * t4 - t3 + t;
            double intH10 = 0.25 * t4 - (2.0 / 3.0) * t3 + 0.5 * t2;
            double intH01 = -0.5 * t4 + t3;
            double intH11 = 0.25 * t4 - (1.0 / 3.0) * t3;

            return p0.Speed * intH00 + m0 * intH10 + p1.Speed * intH01 + m1 * intH11;
        }
    }
}
