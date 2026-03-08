using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace Curver
{
    [VideoEffect("AiuCurver", ["Curver"], [])]
    public class VelocityCurveEffect : VideoEffectBase
    {

        public override string Label => "AiuCurver";


        [Display(Name = "Curve", Description = "Velocity curve editor")]
        [CurveEditor]
        public string CurveData
        {
            get => curveData;
            set => Set(ref curveData, value);
        }
        private string curveData = new VelocityCurve().Serialize();

        [Display(Name = "Speed Scale", Description = "Overall speed multiplier")]
        [AnimationSlider("F1", "%", 0, 500)]
        public Animation SpeedScale { get; } = new Animation(100, 0, 1000);

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new VelocityCurveEffectProcessor(this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [SpeedScale];
    }
}
