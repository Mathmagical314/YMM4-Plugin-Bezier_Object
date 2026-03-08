using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace Curver
{

    internal class VelocityCurveEffectProcessor : IVideoEffectProcessor
    {
        readonly VelocityCurveEffect item;
        VelocityCurve? curve;
        ID2D1Image? input;

        public ID2D1Image Output => input ?? throw new NullReferenceException(nameof(input) + " is null");

        public VelocityCurveEffectProcessor(VelocityCurveEffect item)
        {
            this.item = item;
        }

        public DrawDescription Update(EffectDescription effectDescription)
        {
            if (curve == null || item.CurveData != curve.Serialize())
            {
                curve = VelocityCurve.Deserialize(item.CurveData);
            }

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            double normalizedTime = length > 0 ? (double)frame / length : 0.0;

            double curveSpeed = curve.GetSpeedAt(normalizedTime);
            double speedScale = item.SpeedScale.GetValue(frame, length, fps);
            double finalSpeed = (curveSpeed * speedScale) / 10000.0; 

            var drawDesc = effectDescription.DrawDescription;

            return drawDesc with
            {
            };
        }

        public void ClearInput()
        {
            input = null;
        }

        public void SetInput(ID2D1Image? input)
        {
            this.input = input;
        }

        public void Dispose()
        {
        }
    }
}
