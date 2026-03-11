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

        // =========================================================
        // アニメーション全般
        // =========================================================

        [Display(Name = "進捗", Order = 1)]
        [AnimationSlider("F1", "%", -50, 150)]
        public Animation Progress { get; } = new Animation(0, -10000, 10000);

        [Display(Name = "字間倍率", Order = 2)]
        [AnimationSlider("F1", "%", -500, 500)]
        public Animation LetterSpacing { get; } = new Animation(100, -500, 500);

        [Display(Name = "Curve", Description = "Scale curve editor", Order = 3)]
        [CurveEditor]
        public string CurveData
        {
            get => curveData;
            set => Set(ref curveData, value);
        }
        private string curveData = new VelocityCurve().Serialize();

        [Display(Name = "スケール", Order = 4)]
        [AnimationSlider("F1", "", 0, 500)]
        public Animation Scale { get; } = new Animation(1, 0, 10);

        [Display(Name = "回転", Order = 5)]
        [AnimationSlider("F1", "%", -500, 500)]
        public Animation Rotation { get; } = new Animation(0, -9999, 9999);

        bool autoRotate = true;
        [Display(Name = "向く", Order = 6)]
        [ToggleSlider]
        public bool AutoRotate { get => autoRotate; set => Set(ref autoRotate, value); }

        bool alignHandles = false;
        [Display(Name = "ハンドル対称", Order = 7, Description = "中間アンカーのIn/Outハンドルを対称に連動させます")]
        [ToggleSlider]
        public bool AlignHandles { get => alignHandles; set => Set(ref alignHandles, value); }

        // =========================================================
        // セグメント数
        // =========================================================

        [Display(Name = "セグメント数 (最大9)", Order = 8)]
        [AnimationSlider("F0", "個", 1, 9)]
        public Animation SegmentCount { get; } = new Animation(4, 1, 9);

        // =========================================================
        // セグメント直線化トグル
        // =========================================================

        bool isLinear1 = false;
        [Display(Name = "セグメント1 直線", GroupName = "モード切替", Order = 9)]
        [ToggleSlider]
        public bool IsLinear1 { get => isLinear1; set => Set(ref isLinear1, value); }

        bool isLinear2 = false;
        [Display(Name = "セグメント2 直線", GroupName = "モード切替", Order = 10)]
        [ToggleSlider]
        public bool IsLinear2 { get => isLinear2; set => Set(ref isLinear2, value); }

        bool isLinear3 = false;
        [Display(Name = "セグメント3 直線", GroupName = "モード切替", Order = 11)]
        [ToggleSlider]
        public bool IsLinear3 { get => isLinear3; set => Set(ref isLinear3, value); }

        bool isLinear4 = false;
        [Display(Name = "セグメント4 直線", GroupName = "モード切替", Order = 12)]
        [ToggleSlider]
        public bool IsLinear4 { get => isLinear4; set => Set(ref isLinear4, value); }

        bool isLinear5 = false;
        [Display(Name = "セグメント5 直線", GroupName = "モード切替", Order = 13)]
        [ToggleSlider]
        public bool IsLinear5 { get => isLinear5; set => Set(ref isLinear5, value); }

        bool isLinear6 = false;
        [Display(Name = "セグメント6 直線", GroupName = "モード切替", Order = 14)]
        [ToggleSlider]
        public bool IsLinear6 { get => isLinear6; set => Set(ref isLinear6, value); }

        bool isLinear7 = false;
        [Display(Name = "セグメント7 直線", GroupName = "モード切替", Order = 15)]
        [ToggleSlider]
        public bool IsLinear7 { get => isLinear7; set => Set(ref isLinear7, value); }

        bool isLinear8 = false;
        [Display(Name = "セグメント8 直線", GroupName = "モード切替", Order = 16)]
        [ToggleSlider]
        public bool IsLinear8 { get => isLinear8; set => Set(ref isLinear8, value); }

        bool isLinear9 = false;
        [Display(Name = "セグメント9 直線", GroupName = "モード切替", Order = 17)]
        [ToggleSlider]
        public bool IsLinear9 { get => isLinear9; set => Set(ref isLinear9, value); }

        // =========================================================
        // ガイド表示
        // =========================================================

        bool showGuide = true;
        [Display(Name = "ガイド表示", Order = 18)]
        [ToggleSlider]
        public bool ShowGuide { get => showGuide; set => Set(ref showGuide, value); }

        // =========================================================
        // 点1（始点）: P0（アンカー）P1（ハンドル出）
        // =========================================================

        [Display(Name = "始点X", GroupName = "点1", Order = 19)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P0X { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "始点Y", GroupName = "点1", Order = 20)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P0Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点1", Order = 21)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P1X { get; } = new Animation(100, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点1", Order = 22)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P1Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点2: P2（ハンドル入）P3（アンカー）P4（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点2", Order = 23)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P2X { get; } = new Animation(100, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点2", Order = 24)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P2Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー2X", GroupName = "点2", Order = 25)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P3X { get; } = new Animation(200, -2000, 2000);

        [Display(Name = "アンカー2Y", GroupName = "点2", Order = 26)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P3Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点2", Order = 27)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P4X { get; } = new Animation(300, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点2", Order = 28)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P4Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点3: P5（ハンドル入）P6（アンカー）P7（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点3", Order = 29)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P5X { get; } = new Animation(300, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点3", Order = 30)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P5Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー3X", GroupName = "点3", Order = 31)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P6X { get; } = new Animation(400, -2000, 2000);

        [Display(Name = "アンカー3Y", GroupName = "点3", Order = 32)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P6Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点3", Order = 33)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P7X { get; } = new Animation(500, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点3", Order = 34)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P7Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点4: P8（ハンドル入）P9（アンカー）P10（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点4", Order = 35)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P8X { get; } = new Animation(500, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点4", Order = 36)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P8Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー4X", GroupName = "点4", Order = 37)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P9X { get; } = new Animation(600, -2000, 2000);

        [Display(Name = "アンカー4Y", GroupName = "点4", Order = 38)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P9Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点4", Order = 39)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P10X { get; } = new Animation(700, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点4", Order = 40)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P10Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点5: P11（ハンドル入）P12（アンカー）P13（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点5", Order = 41)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P11X { get; } = new Animation(700, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点5", Order = 42)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P11Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー5X", GroupName = "点5", Order = 43)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P12X { get; } = new Animation(800, -2000, 2000);

        [Display(Name = "アンカー5Y", GroupName = "点5", Order = 44)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P12Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点5", Order = 45)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P13X { get; } = new Animation(900, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点5", Order = 46)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P13Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点6: P14（ハンドル入）P15（アンカー）P16（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点6", Order = 47)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P14X { get; } = new Animation(900, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点6", Order = 48)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P14Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー6X", GroupName = "点6", Order = 49)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P15X { get; } = new Animation(1000, -2000, 2000);

        [Display(Name = "アンカー6Y", GroupName = "点6", Order = 50)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P15Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点6", Order = 51)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P16X { get; } = new Animation(1100, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点6", Order = 52)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P16Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点7: P17（ハンドル入）P18（アンカー）P19（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点7", Order = 53)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P17X { get; } = new Animation(1100, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点7", Order = 54)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P17Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー7X", GroupName = "点7", Order = 55)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P18X { get; } = new Animation(1200, -2000, 2000);

        [Display(Name = "アンカー7Y", GroupName = "点7", Order = 56)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P18Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点7", Order = 57)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P19X { get; } = new Animation(1300, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点7", Order = 58)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P19Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点8: P20（ハンドル入）P21（アンカー）P22（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点8", Order = 59)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P20X { get; } = new Animation(1300, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点8", Order = 60)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P20Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー8X", GroupName = "点8", Order = 61)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P21X { get; } = new Animation(1400, -2000, 2000);

        [Display(Name = "アンカー8Y", GroupName = "点8", Order = 62)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P21Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点8", Order = 63)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P22X { get; } = new Animation(1500, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点8", Order = 64)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P22Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点9: P23（ハンドル入）P24（アンカー）P25（ハンドル出）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点9", Order = 65)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P23X { get; } = new Animation(1500, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点9", Order = 66)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P23Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "アンカー9X", GroupName = "点9", Order = 67)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P24X { get; } = new Animation(1600, -2000, 2000);

        [Display(Name = "アンカー9Y", GroupName = "点9", Order = 68)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P24Y { get; } = new Animation(0, -2000, 2000);

        [Display(Name = "制御点出X", GroupName = "点9", Order = 69)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P25X { get; } = new Animation(1700, -2000, 2000);

        [Display(Name = "制御点出Y", GroupName = "点9", Order = 70)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P25Y { get; } = new Animation(100, -2000, 2000);

        // =========================================================
        // 点10（終点）: P26（ハンドル入）P27（アンカー）
        // =========================================================

        [Display(Name = "制御点入X", GroupName = "点10", Order = 71)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P26X { get; } = new Animation(1700, -2000, 2000);

        [Display(Name = "制御点入Y", GroupName = "点10", Order = 72)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P26Y { get; } = new Animation(-100, -2000, 2000);

        [Display(Name = "終点X", GroupName = "点10", Order = 73)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P27X { get; } = new Animation(1800, -2000, 2000);

        [Display(Name = "終点Y", GroupName = "点10", Order = 74)]
        [AnimationSlider("F1", "px", -2000, 2000)]
        public Animation P27Y { get; } = new Animation(0, -2000, 2000);

        // =========================================================
        // アニメーションプロパティの登録
        // =========================================================
        protected override IEnumerable<IAnimatable> GetAnimatables() => [
            P0X, P0Y, P1X, P1Y,
            P2X, P2Y, P3X, P3Y, P4X, P4Y,
            P5X, P5Y, P6X, P6Y, P7X, P7Y,
            P8X, P8Y, P9X, P9Y, P10X, P10Y,
            P11X, P11Y, P12X, P12Y, P13X, P13Y,
            P14X, P14Y, P15X, P15Y, P16X, P16Y,
            P17X, P17Y, P18X, P18Y, P19X, P19Y,
            P20X, P20Y, P21X, P21Y, P22X, P22Y,
            P23X, P23Y, P24X, P24Y, P25X, P25Y,
            P26X, P26Y, P27X, P27Y,
            Progress, LetterSpacing, Rotation, Scale, SegmentCount
        ];

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