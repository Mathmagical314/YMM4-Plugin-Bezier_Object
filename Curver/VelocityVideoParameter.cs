using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace Curver
{
    public class VelocityVideoParameter : ShapeParameterBase
    {
        [Display(Name = "File", Description = "Video File")]
        [FileSelector(YukkuriMovieMaker.Settings.FileGroupType.VideoItem)]
        public string FilePath { get => filePath; set => Set(ref filePath, value); }
        private string filePath = string.Empty;

        [Display(Name = "Velocity Curve")]
        [CurveEditor]
        public string CurveData { get => curveData; set => Set(ref curveData, value); }
        private string curveData = new VelocityCurve().Serialize(); 

        [Display(Name = "Show Curve Guide", Description = "Show velocity curve overlay on video")]
        [ToggleSlider]
        public bool ShowGraph { get => showGraph; set => Set(ref showGraph, value); }
        private bool showGraph = false;

        public VelocityVideoParameter(SharedDataStore? sharedData) : base(sharedData) { }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            return new VelocityVideoSource(devices, this);
        }

        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc)
        {
            return []; // EXO出力は未対応
        }

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskParameters)
        {
            return []; // マスク未対応
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [];

        protected override void LoadSharedData(SharedDataStore store)
        {
            var data = store.Load<SharedData>();
            if (data is null) return;
            FilePath = data.FilePath;
            CurveData = data.CurveData;
        }

        protected override void SaveSharedData(SharedDataStore store)
        {
            store.Save(new SharedData(this));
        }

        public class SharedData
        {
            public string FilePath { get; }
            public string CurveData { get; }

            public SharedData(VelocityVideoParameter parameter)
            {
                FilePath = parameter.FilePath;
                CurveData = parameter.CurveData;
            }

            public SharedData() { FilePath = ""; CurveData = ""; }
        }
    }
}
