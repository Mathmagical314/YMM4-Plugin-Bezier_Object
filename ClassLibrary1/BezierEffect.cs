using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;

namespace Curver
{
    [VideoEffect("TextBezier", ["描画"], [])]
    public class TextBezierEffect : VideoEffectBase
    {
        public override string Label => "TextBezier";

        // パラメータ定義

        [Display(Name = "始点X", GroupName = "点1", Order = 1)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P0X { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "始点Y", GroupName = "点1", Order = 2)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P0Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点1X", GroupName = "点2", Order = 3)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P1X { get; } = new Animation(100, -2000, 2000);

        [Display(Name = "制御点1Y", GroupName = "点2", Order = 4)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P1Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点2X", GroupName = "点3", Order = 5)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P2X { get; } = new Animation(200, -2000, 2000);

        [Display(Name = "制御点2Y", GroupName = "点3", Order = 6)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P2Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "終点X", GroupName = "点4", Order = 7)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P3X { get; } = new Animation(300, -2000, 2000);

        [Display(Name = "終点Y", GroupName = "点4", Order = 8)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P3Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "進捗", Order = 10)]
        [AnimationSlider("F1", "%", -50, 150)]
        public Animation Progress { get; } = new Animation(0, -10000, 10000);

        [Display(Name = "字間倍率", Order = 9)]
        [AnimationSlider("F1", "%", -500, 500)]
        public Animation LetterSpacing { get; } = new Animation(100, -500, 500);

        [Display(Name = "回転", Order = 11)]
        [AnimationSlider("F1", "%", -500, 500)]
        public Animation Rotation { get; } = new Animation(0, -9999, 9999);

        bool autoRotate = true;
        [Display(Name = "向く", Order = 12)]
        [ToggleSlider]
        public bool AutoRotate { get => autoRotate; set => Set(ref autoRotate, value); }

        [Display(Name = "Curve", Description = "Velocity curve editor")]
        [CurveEditor]
        public string CurveData
        {
            get => curveData;
            set => Set(ref curveData, value);
        }
        private string curveData = new VelocityCurve().Serialize();


        [Display(Name = "スケール", Order = 13)]
        [AnimationSlider("F1", "%", 0, 500)]
        public Animation Scale { get; } = new Animation(1, 0, 9999);

        bool showGuide = true;
        [Display(Name = "ガイド表示", Order = 14)]
        [ToggleSlider]
        public bool ShowGuide { get => showGuide; set => Set(ref showGuide, value); }

        // アニメーションプロパティの登録
        protected override IEnumerable<IAnimatable> GetAnimatables() =>
            [P0X, P0Y, P1X, P1Y, P2X, P2Y, P3X, P3Y, Progress, LetterSpacing, Rotation, Scale];

        // 描画プロセッサを生成する
        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new TextBezierProcessor(devices, this);
        }

        // Exo出力用
        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }
    }
}