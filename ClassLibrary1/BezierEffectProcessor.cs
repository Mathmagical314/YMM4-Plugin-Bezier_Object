using System;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;
 namespace Curver
{
    internal class TextBezierProcessor : IVideoEffectProcessor
    {
        private readonly TextBezierEffect _effect;
        private readonly IGraphicsDevicesAndContext _devices;

        // エフェクト部品
        private readonly ID2D1Effect _transformEffect; // 画像を動かすやつ
        private readonly ID2D1Effect _compositeEffect; // 画像とガイド線を合成するやつ

        // 描画用ブラシ
        private readonly ID2D1SolidColorBrush _lineBrush;     // 軌道（赤）
        private readonly ID2D1SolidColorBrush _handleBrush;   // 制御線のハンドル（緑）

        // ガイド描画用コマンドリスト
        private ID2D1CommandList? _commandList;

        // 今のフレームで実際にガイドを描画したかどうかを判定するフラグ
        private bool _isGuideDrawn;

        // ガイド表示がONなら合成結果を、OFFなら画像だけをYMM4に返す
        public ID2D1Image Output => _isGuideDrawn ? _compositeEffect.Output : _transformEffect.Output;

        VelocityCurve? curve;

        public TextBezierProcessor(IGraphicsDevicesAndContext devices, TextBezierEffect effect)
        {
            _effect = effect;
            _devices = devices;

            // 各種エフェクトの初期化
            _transformEffect = (ID2D1Effect)_devices.DeviceContext.CreateEffect(Vortice.Direct2D1.EffectGuids.AffineTransform2D);
            _compositeEffect = (ID2D1Effect)_devices.DeviceContext.CreateEffect(Vortice.Direct2D1.EffectGuids.Composite);

            // ブラシの色を設定
            _lineBrush = _devices.DeviceContext.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1.0f, 0.2f, 0.2f, 0.8f));
            _handleBrush = _devices.DeviceContext.CreateSolidColorBrush(new Vortice.Mathematics.Color4(0.2f, 1.0f, 0.2f, 0.5f));
        }

        public void SetInput(ID2D1Image? input)
        {
            _transformEffect.SetInput(0, input, true);
        }

        public void ClearInput()
        {
            _transformEffect.SetInput(0, null, true);
        }

        // 曲線の長さを基準とした t の算出（セグメント数を引数で受け取る）
        private float GetNormalizedT(float targetProgress, Vector2[] pts, int totalCurves, out int segmentIndex)
        {
            int segmentsPerCurve = 25;
            float[] lengths = new float[segmentsPerCurve * totalCurves + 1];
            float totalLength = 0;
            Vector2 prevPoint = pts[0];

            lengths[0] = 0;
            for (int c = 0; c < totalCurves; c++)
            {
                Vector2 p0 = pts[c * 3];
                Vector2 p1 = pts[c * 3 + 1];
                Vector2 p2 = pts[c * 3 + 2];
                Vector2 p3 = pts[c * 3 + 3];

                for (int i = 1; i <= segmentsPerCurve; i++)
                {
                    float tempT = i / (float)segmentsPerCurve;
                    float u = 1f - tempT;
                    Vector2 pt = (u * u * u) * p0 + 3 * (u * u) * tempT * p1 + 3 * u * (tempT * tempT) * p2 + (tempT * tempT * tempT) * p3;
                    totalLength += Vector2.Distance(prevPoint, pt);
                    lengths[c * segmentsPerCurve + i] = totalLength;
                    prevPoint = pt;
                }
            }

            float targetLength = targetProgress * totalLength;

            if (targetLength <= 0) { segmentIndex = 0; return 0f; }
            if (targetLength >= totalLength) { segmentIndex = totalCurves - 1; return 1f; }

            for (int c = 0; c < totalCurves; c++)
            {
                for (int i = 0; i < segmentsPerCurve; i++)
                {
                    int globalIdx = c * segmentsPerCurve + i;
                    if (targetLength >= lengths[globalIdx] && targetLength <= lengths[globalIdx + 1])
                    {
                        float segmentLength = lengths[globalIdx + 1] - lengths[globalIdx];
                        float fraction = (segmentLength > 0) ? (targetLength - lengths[globalIdx]) / segmentLength : 0;
                        segmentIndex = c;
                        return (i + fraction) / segmentsPerCurve;
                    }
                }
            }

            segmentIndex = totalCurves - 1;
            return 1f;
        }

        // 全プロパティのアニメーション値を配列で取得するヘルパー
        private void GetAllPoints(long frame, long length, int fps, float[] pX, float[] pY)
        {
            var props = new[]
            {
                (_effect.P0X,  _effect.P0Y),  (_effect.P1X,  _effect.P1Y),  (_effect.P2X,  _effect.P2Y),
                (_effect.P3X,  _effect.P3Y),  (_effect.P4X,  _effect.P4Y),  (_effect.P5X,  _effect.P5Y),
                (_effect.P6X,  _effect.P6Y),  (_effect.P7X,  _effect.P7Y),  (_effect.P8X,  _effect.P8Y),
                (_effect.P9X,  _effect.P9Y),  (_effect.P10X, _effect.P10Y), (_effect.P11X, _effect.P11Y),
                (_effect.P12X, _effect.P12Y), (_effect.P13X, _effect.P13Y), (_effect.P14X, _effect.P14Y),
                (_effect.P15X, _effect.P15Y), (_effect.P16X, _effect.P16Y), (_effect.P17X, _effect.P17Y),
                (_effect.P18X, _effect.P18Y), (_effect.P19X, _effect.P19Y), (_effect.P20X, _effect.P20Y),
                (_effect.P21X, _effect.P21Y), (_effect.P22X, _effect.P22Y), (_effect.P23X, _effect.P23Y),
                (_effect.P24X, _effect.P24Y), (_effect.P25X, _effect.P25Y), (_effect.P26X, _effect.P26Y),
                (_effect.P27X, _effect.P27Y),
            };
            for (int i = 0; i < 28; i++)
            {
                pX[i] = (float)props[i].Item1.GetValue(frame, length, fps);
                pY[i] = (float)props[i].Item2.GetValue(frame, length, fps);
            }
        }

        public DrawDescription Update(EffectDescription description)
        {
            var frame = description.ItemPosition.Frame;
            var length = description.ItemDuration.Frame;
            var fps = description.FPS;

            if (curve == null || _effect.CurveData != curve.Serialize())
            {
                curve = VelocityCurve.Deserialize(_effect.CurveData);
            }

            // 全座標を配列で取得
            float[] pX = new float[28];
            float[] pY = new float[28];
            GetAllPoints(frame, length, fps, pX, pY);

            float Angle_Degree = (float)_effect.Rotation.GetValue(frame, length, fps);
            float Scale = (float)_effect.Scale.GetValue(frame, length, fps);


            // 進捗と字間倍率を取得
            float base_progress = (float)_effect.Progress.GetValue(frame, length, fps) / 100f;
            float spacing = (float)_effect.LetterSpacing.GetValue(frame, length, fps) / 100f;

            int index = Math.Max(description.InputIndex, description.GroupIndex);
            int count = Math.Max(description.InputCount, description.GroupCount);

            // 基準進捗の算出
            float targetProgress = base_progress;
            if (count > 1)
            {
                float offsetIndex = index - (count - 1) / 2f;
                targetProgress += offsetIndex * (spacing * 0.1f);
            }

            // セグメント数（1-9）を取得
            int segmentCount = Math.Clamp((int)_effect.SegmentCount.GetValue(frame, length, fps), 1, 9);

            // 直線フラグ配列
            bool[] isLinear = new bool[9]
            {
                _effect.IsLinear1, _effect.IsLinear2, _effect.IsLinear3,
                _effect.IsLinear4, _effect.IsLinear5, _effect.IsLinear6,
                _effect.IsLinear7, _effect.IsLinear8, _effect.IsLinear9
            };

            // 使用する点数分だけ Vector2 配列を作るして、直線スナップを適用
            int totalPts = segmentCount * 3 + 1;
            Vector2[] pts = new Vector2[totalPts];
            for (int i = 0; i < totalPts; i++)
                pts[i] = new Vector2(pX[i], pY[i]);

            for (int i = 0; i < segmentCount; i++)
            {
                if (isLinear[i])
                {
                    pts[i * 3 + 1] = pts[i * 3];
                    pts[i * 3 + 2] = pts[i * 3 + 3];
                }
            }

            // 等間隔になるよう t を補正
            int segmentIndex;
            float t = GetNormalizedT(targetProgress, pts, segmentCount, out segmentIndex);

            // 対象セグメントの制御点
            Vector2 curP0 = pts[segmentIndex * 3];
            Vector2 curP1 = pts[segmentIndex * 3 + 1];
            Vector2 curP2 = pts[segmentIndex * 3 + 2];
            Vector2 curP3 = pts[segmentIndex * 3 + 3];

            // ハンドル対称の強制スナップ（AlignHandles ON時）
            if (_effect.AlignHandles)
            {
                Action<Animation, Animation, float, float, float, float> enforceAlign = (AnimX, AnimY, ax, ay, hx, hy) =>
                {
                    float nx = hx - ax; float ny = hy - ay;
                    float angle = MathF.Atan2(ny, nx) + MathF.PI;
                    float currentOppositeLen = MathF.Sqrt(
                        MathF.Pow((float)AnimX.GetValue(frame, length, fps) - ax, 2) +
                        MathF.Pow((float)AnimY.GetValue(frame, length, fps) - ay, 2));
                    float targetX = ax + MathF.Cos(angle) * currentOppositeLen;
                    float targetY = ay + MathF.Sin(angle) * currentOppositeLen;
                    float currentValX = (float)AnimX.GetValue(frame, length, fps);
                    float currentValY = (float)AnimY.GetValue(frame, length, fps);
                    if (MathF.Abs(targetX - currentValX) > 0.1f) AnimX.AddToEachValues(targetX - currentValX);
                    if (MathF.Abs(targetY - currentValY) > 0.1f) AnimY.AddToEachValues(targetY - currentValY);
                };

                // 中間アンカーごとに対向ハンドルを対称にスナップ
                // セグメント数 >= 2 → アンカーP3(idx3): P4 を P2 に対称
                if (segmentCount >= 2) enforceAlign(_effect.P4X,  _effect.P4Y,  pX[3],  pY[3],  pX[2],  pY[2]);
                if (segmentCount >= 3) enforceAlign(_effect.P7X,  _effect.P7Y,  pX[6],  pY[6],  pX[5],  pY[5]);
                if (segmentCount >= 4) enforceAlign(_effect.P10X, _effect.P10Y, pX[9],  pY[9],  pX[8],  pY[8]);
                if (segmentCount >= 5) enforceAlign(_effect.P13X, _effect.P13Y, pX[12], pY[12], pX[11], pY[11]);
                if (segmentCount >= 6) enforceAlign(_effect.P16X, _effect.P16Y, pX[15], pY[15], pX[14], pY[14]);
                if (segmentCount >= 7) enforceAlign(_effect.P19X, _effect.P19Y, pX[18], pY[18], pX[17], pY[17]);
                if (segmentCount >= 8) enforceAlign(_effect.P22X, _effect.P22Y, pX[21], pY[21], pX[20], pY[20]);
                if (segmentCount >= 9) enforceAlign(_effect.P25X, _effect.P25Y, pX[24], pY[24], pX[23], pY[23]);
            }

            // コントローラー（プレビュー上のドラッグ可能な点）の定義
            // 使用セグメント数に関係なく全28点を登録する（余り点はドラッグしても無視される）
            var control = new VideoEffectController(
                _effect,
                [
                    // P0: 始点アンカー
                    new ControllerPoint(new(pX[0], pY[0], 0f), x => { _effect.P0X.AddToEachValues(x.Delta.X); _effect.P0Y.AddToEachValues(x.Delta.Y); }),
                    // P1: 始点ハンドル出
                    new ControllerPoint(new(pX[1], pY[1], 0f), x => { _effect.P1X.AddToEachValues(x.Delta.X); _effect.P1Y.AddToEachValues(x.Delta.Y); }),
                    // P2: アンカー2入ハンドル
                    new ControllerPoint(new(pX[2], pY[2], 0f), x => {
                        _effect.P2X.AddToEachValues(x.Delta.X); _effect.P2Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[2]+x.Delta.X-pX[3]; float ny = pY[2]+x.Delta.Y-pY[3];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[4]-pX[3])*(pX[4]-pX[3])+(pY[4]-pY[3])*(pY[4]-pY[3]));
                            _effect.P4X.AddToEachValues(pX[3]+MathF.Cos(a)*l-pX[4]); _effect.P4Y.AddToEachValues(pY[3]+MathF.Sin(a)*l-pY[4]);
                        }
                    }),
                    // P3: アンカー2
                    new ControllerPoint(new(pX[3], pY[3], 0f), x => {
                        _effect.P3X.AddToEachValues(x.Delta.X); _effect.P3Y.AddToEachValues(x.Delta.Y);
                        _effect.P2X.AddToEachValues(x.Delta.X); _effect.P2Y.AddToEachValues(x.Delta.Y);
                        _effect.P4X.AddToEachValues(x.Delta.X); _effect.P4Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P4: アンカー2出ハンドル
                    new ControllerPoint(new(pX[4], pY[4], 0f), x => {
                        _effect.P4X.AddToEachValues(x.Delta.X); _effect.P4Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[4]+x.Delta.X-pX[3]; float ny = pY[4]+x.Delta.Y-pY[3];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[2]-pX[3])*(pX[2]-pX[3])+(pY[2]-pY[3])*(pY[2]-pY[3]));
                            _effect.P2X.AddToEachValues(pX[3]+MathF.Cos(a)*l-pX[2]); _effect.P2Y.AddToEachValues(pY[3]+MathF.Sin(a)*l-pY[2]);
                        }
                    }),
                    // P5: アンカー3入ハンドル
                    new ControllerPoint(new(pX[5], pY[5], 0f), x => {
                        _effect.P5X.AddToEachValues(x.Delta.X); _effect.P5Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[5]+x.Delta.X-pX[6]; float ny = pY[5]+x.Delta.Y-pY[6];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[7]-pX[6])*(pX[7]-pX[6])+(pY[7]-pY[6])*(pY[7]-pY[6]));
                            _effect.P7X.AddToEachValues(pX[6]+MathF.Cos(a)*l-pX[7]); _effect.P7Y.AddToEachValues(pY[6]+MathF.Sin(a)*l-pY[7]);
                        }
                    }),
                    // P6: アンカー3
                    new ControllerPoint(new(pX[6], pY[6], 0f), x => {
                        _effect.P6X.AddToEachValues(x.Delta.X); _effect.P6Y.AddToEachValues(x.Delta.Y);
                        _effect.P5X.AddToEachValues(x.Delta.X); _effect.P5Y.AddToEachValues(x.Delta.Y);
                        _effect.P7X.AddToEachValues(x.Delta.X); _effect.P7Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P7: アンカー3出ハンドル
                    new ControllerPoint(new(pX[7], pY[7], 0f), x => {
                        _effect.P7X.AddToEachValues(x.Delta.X); _effect.P7Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[7]+x.Delta.X-pX[6]; float ny = pY[7]+x.Delta.Y-pY[6];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[5]-pX[6])*(pX[5]-pX[6])+(pY[5]-pY[6])*(pY[5]-pY[6]));
                            _effect.P5X.AddToEachValues(pX[6]+MathF.Cos(a)*l-pX[5]); _effect.P5Y.AddToEachValues(pY[6]+MathF.Sin(a)*l-pY[5]);
                        }
                    }),
                    // P8: アンカー4入ハンドル
                    new ControllerPoint(new(pX[8], pY[8], 0f), x => {
                        _effect.P8X.AddToEachValues(x.Delta.X); _effect.P8Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[8]+x.Delta.X-pX[9]; float ny = pY[8]+x.Delta.Y-pY[9];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[10]-pX[9])*(pX[10]-pX[9])+(pY[10]-pY[9])*(pY[10]-pY[9]));
                            _effect.P10X.AddToEachValues(pX[9]+MathF.Cos(a)*l-pX[10]); _effect.P10Y.AddToEachValues(pY[9]+MathF.Sin(a)*l-pY[10]);
                        }
                    }),
                    // P9: アンカー4
                    new ControllerPoint(new(pX[9], pY[9], 0f), x => {
                        _effect.P9X.AddToEachValues(x.Delta.X); _effect.P9Y.AddToEachValues(x.Delta.Y);
                        _effect.P8X.AddToEachValues(x.Delta.X); _effect.P8Y.AddToEachValues(x.Delta.Y);
                        _effect.P10X.AddToEachValues(x.Delta.X); _effect.P10Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P10: アンカー4出ハンドル
                    new ControllerPoint(new(pX[10], pY[10], 0f), x => {
                        _effect.P10X.AddToEachValues(x.Delta.X); _effect.P10Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[10]+x.Delta.X-pX[9]; float ny = pY[10]+x.Delta.Y-pY[9];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[8]-pX[9])*(pX[8]-pX[9])+(pY[8]-pY[9])*(pY[8]-pY[9]));
                            _effect.P8X.AddToEachValues(pX[9]+MathF.Cos(a)*l-pX[8]); _effect.P8Y.AddToEachValues(pY[9]+MathF.Sin(a)*l-pY[8]);
                        }
                    }),
                    // P11: アンカー5入ハンドル
                    new ControllerPoint(new(pX[11], pY[11], 0f), x => {
                        _effect.P11X.AddToEachValues(x.Delta.X); _effect.P11Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[11]+x.Delta.X-pX[12]; float ny = pY[11]+x.Delta.Y-pY[12];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[13]-pX[12])*(pX[13]-pX[12])+(pY[13]-pY[12])*(pY[13]-pY[12]));
                            _effect.P13X.AddToEachValues(pX[12]+MathF.Cos(a)*l-pX[13]); _effect.P13Y.AddToEachValues(pY[12]+MathF.Sin(a)*l-pY[13]);
                        }
                    }),
                    // P12: アンカー5
                    new ControllerPoint(new(pX[12], pY[12], 0f), x => {
                        _effect.P12X.AddToEachValues(x.Delta.X); _effect.P12Y.AddToEachValues(x.Delta.Y);
                        _effect.P11X.AddToEachValues(x.Delta.X); _effect.P11Y.AddToEachValues(x.Delta.Y);
                        _effect.P13X.AddToEachValues(x.Delta.X); _effect.P13Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P13: アンカー5出ハンドル
                    new ControllerPoint(new(pX[13], pY[13], 0f), x => {
                        _effect.P13X.AddToEachValues(x.Delta.X); _effect.P13Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[13]+x.Delta.X-pX[12]; float ny = pY[13]+x.Delta.Y-pY[12];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[11]-pX[12])*(pX[11]-pX[12])+(pY[11]-pY[12])*(pY[11]-pY[12]));
                            _effect.P11X.AddToEachValues(pX[12]+MathF.Cos(a)*l-pX[11]); _effect.P11Y.AddToEachValues(pY[12]+MathF.Sin(a)*l-pY[11]);
                        }
                    }),
                    // P14: アンカー6入ハンドル
                    new ControllerPoint(new(pX[14], pY[14], 0f), x => {
                        _effect.P14X.AddToEachValues(x.Delta.X); _effect.P14Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[14]+x.Delta.X-pX[15]; float ny = pY[14]+x.Delta.Y-pY[15];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[16]-pX[15])*(pX[16]-pX[15])+(pY[16]-pY[15])*(pY[16]-pY[15]));
                            _effect.P16X.AddToEachValues(pX[15]+MathF.Cos(a)*l-pX[16]); _effect.P16Y.AddToEachValues(pY[15]+MathF.Sin(a)*l-pY[16]);
                        }
                    }),
                    // P15: アンカー6
                    new ControllerPoint(new(pX[15], pY[15], 0f), x => {
                        _effect.P15X.AddToEachValues(x.Delta.X); _effect.P15Y.AddToEachValues(x.Delta.Y);
                        _effect.P14X.AddToEachValues(x.Delta.X); _effect.P14Y.AddToEachValues(x.Delta.Y);
                        _effect.P16X.AddToEachValues(x.Delta.X); _effect.P16Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P16: アンカー6出ハンドル
                    new ControllerPoint(new(pX[16], pY[16], 0f), x => {
                        _effect.P16X.AddToEachValues(x.Delta.X); _effect.P16Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[16]+x.Delta.X-pX[15]; float ny = pY[16]+x.Delta.Y-pY[15];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[14]-pX[15])*(pX[14]-pX[15])+(pY[14]-pY[15])*(pY[14]-pY[15]));
                            _effect.P14X.AddToEachValues(pX[15]+MathF.Cos(a)*l-pX[14]); _effect.P14Y.AddToEachValues(pY[15]+MathF.Sin(a)*l-pY[14]);
                        }
                    }),
                    // P17: アンカー7入ハンドル
                    new ControllerPoint(new(pX[17], pY[17], 0f), x => {
                        _effect.P17X.AddToEachValues(x.Delta.X); _effect.P17Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[17]+x.Delta.X-pX[18]; float ny = pY[17]+x.Delta.Y-pY[18];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[19]-pX[18])*(pX[19]-pX[18])+(pY[19]-pY[18])*(pY[19]-pY[18]));
                            _effect.P19X.AddToEachValues(pX[18]+MathF.Cos(a)*l-pX[19]); _effect.P19Y.AddToEachValues(pY[18]+MathF.Sin(a)*l-pY[19]);
                        }
                    }),
                    // P18: アンカー7
                    new ControllerPoint(new(pX[18], pY[18], 0f), x => {
                        _effect.P18X.AddToEachValues(x.Delta.X); _effect.P18Y.AddToEachValues(x.Delta.Y);
                        _effect.P17X.AddToEachValues(x.Delta.X); _effect.P17Y.AddToEachValues(x.Delta.Y);
                        _effect.P19X.AddToEachValues(x.Delta.X); _effect.P19Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P19: アンカー7出ハンドル
                    new ControllerPoint(new(pX[19], pY[19], 0f), x => {
                        _effect.P19X.AddToEachValues(x.Delta.X); _effect.P19Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[19]+x.Delta.X-pX[18]; float ny = pY[19]+x.Delta.Y-pY[18];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[17]-pX[18])*(pX[17]-pX[18])+(pY[17]-pY[18])*(pY[17]-pY[18]));
                            _effect.P17X.AddToEachValues(pX[18]+MathF.Cos(a)*l-pX[17]); _effect.P17Y.AddToEachValues(pY[18]+MathF.Sin(a)*l-pY[17]);
                        }
                    }),
                    // P20: アンカー8入ハンドル
                    new ControllerPoint(new(pX[20], pY[20], 0f), x => {
                        _effect.P20X.AddToEachValues(x.Delta.X); _effect.P20Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[20]+x.Delta.X-pX[21]; float ny = pY[20]+x.Delta.Y-pY[21];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[22]-pX[21])*(pX[22]-pX[21])+(pY[22]-pY[21])*(pY[22]-pY[21]));
                            _effect.P22X.AddToEachValues(pX[21]+MathF.Cos(a)*l-pX[22]); _effect.P22Y.AddToEachValues(pY[21]+MathF.Sin(a)*l-pY[22]);
                        }
                    }),
                    // P21: アンカー8
                    new ControllerPoint(new(pX[21], pY[21], 0f), x => {
                        _effect.P21X.AddToEachValues(x.Delta.X); _effect.P21Y.AddToEachValues(x.Delta.Y);
                        _effect.P20X.AddToEachValues(x.Delta.X); _effect.P20Y.AddToEachValues(x.Delta.Y);
                        _effect.P22X.AddToEachValues(x.Delta.X); _effect.P22Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P22: アンカー8出ハンドル
                    new ControllerPoint(new(pX[22], pY[22], 0f), x => {
                        _effect.P22X.AddToEachValues(x.Delta.X); _effect.P22Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[22]+x.Delta.X-pX[21]; float ny = pY[22]+x.Delta.Y-pY[21];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[20]-pX[21])*(pX[20]-pX[21])+(pY[20]-pY[21])*(pY[20]-pY[21]));
                            _effect.P20X.AddToEachValues(pX[21]+MathF.Cos(a)*l-pX[20]); _effect.P20Y.AddToEachValues(pY[21]+MathF.Sin(a)*l-pY[20]);
                        }
                    }),
                    // P23: アンカー9入ハンドル
                    new ControllerPoint(new(pX[23], pY[23], 0f), x => {
                        _effect.P23X.AddToEachValues(x.Delta.X); _effect.P23Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[23]+x.Delta.X-pX[24]; float ny = pY[23]+x.Delta.Y-pY[24];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[25]-pX[24])*(pX[25]-pX[24])+(pY[25]-pY[24])*(pY[25]-pY[24]));
                            _effect.P25X.AddToEachValues(pX[24]+MathF.Cos(a)*l-pX[25]); _effect.P25Y.AddToEachValues(pY[24]+MathF.Sin(a)*l-pY[25]);
                        }
                    }),
                    // P24: アンカー9
                    new ControllerPoint(new(pX[24], pY[24], 0f), x => {
                        _effect.P24X.AddToEachValues(x.Delta.X); _effect.P24Y.AddToEachValues(x.Delta.Y);
                        _effect.P23X.AddToEachValues(x.Delta.X); _effect.P23Y.AddToEachValues(x.Delta.Y);
                        _effect.P25X.AddToEachValues(x.Delta.X); _effect.P25Y.AddToEachValues(x.Delta.Y);
                    }),
                    // P25: アンカー9出ハンドル
                    new ControllerPoint(new(pX[25], pY[25], 0f), x => {
                        _effect.P25X.AddToEachValues(x.Delta.X); _effect.P25Y.AddToEachValues(x.Delta.Y);
                        if (_effect.AlignHandles) {
                            float nx = pX[25]+x.Delta.X-pX[24]; float ny = pY[25]+x.Delta.Y-pY[24];
                            float a = MathF.Atan2(ny, nx)+MathF.PI; float l = MathF.Sqrt((pX[23]-pX[24])*(pX[23]-pX[24])+(pY[23]-pY[24])*(pY[23]-pY[24]));
                            _effect.P23X.AddToEachValues(pX[24]+MathF.Cos(a)*l-pX[23]); _effect.P23Y.AddToEachValues(pY[24]+MathF.Sin(a)*l-pY[23]);
                        }
                    }),
                    // P26: 終点入ハンドル
                    new ControllerPoint(new(pX[26], pY[26], 0f), x => { _effect.P26X.AddToEachValues(x.Delta.X); _effect.P26Y.AddToEachValues(x.Delta.Y); }),
                    // P27: 終点アンカー
                    new ControllerPoint(new(pX[27], pY[27], 0f), x => { _effect.P27X.AddToEachValues(x.Delta.X); _effect.P27Y.AddToEachValues(x.Delta.Y); }),
                ]);

            //
            double curveScale = curve.GetSpeedAt(targetProgress) / 100.0;
            float curveScale2 = (float)curveScale;
            float u = 1f - t;

            // 現在地と角度の計算
            Vector2 position = (u * u * u) * curP0 + 3 * (u * u) * t * curP1 + 3 * u * (t * t) * curP2 + (t * t * t) * curP3;
            float angle = Angle_Degree * (MathF.PI / 180f);
            Vector2 Scale2D = new(Scale * curveScale2, Scale * curveScale2);

            if (_effect.AutoRotate)
            {
                Vector2 tangent = 3 * (u * u) * (curP1 - curP0) + 6 * u * t * (curP2 - curP1) + 3 * (t * t) * (curP3 - curP2);
                angle += MathF.Atan2(tangent.Y, tangent.X);
            }

            // 画像を変形（移動・回転）
            Matrix3x2 matrix;
            if (targetProgress >= 0f && targetProgress <= 1f)
            {
                matrix = Matrix3x2.CreateScale(Scale2D) * Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(position.X, position.Y);
            }
            else
            {
                matrix = Matrix3x2.CreateTranslation(-99999f, -99999f);
            }

            _transformEffect.SetValue((int)AffineTransform2DProperties.TransformMatrix, matrix);

            // ガイド描画判定
            _isGuideDrawn = _effect.ShowGuide && description.Usage != TimelineSourceUsage.Exporting;

            if (_isGuideDrawn)
            {
                _commandList?.Dispose();
                _commandList = _devices.DeviceContext.CreateCommandList();

                var oldTarget = _devices.DeviceContext.Target;
                _devices.DeviceContext.Target = _commandList;
                _devices.DeviceContext.BeginDraw();
                _devices.DeviceContext.Clear(null);

                // ベジェ曲線パスの描画
                using (var geometry = _devices.DeviceContext.Factory.CreatePathGeometry())
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(pts[0], FigureBegin.Hollow);
                    for (int i = 0; i < segmentCount; i++)
                    {
                        if (isLinear[i])
                            sink.AddLine(pts[i * 3 + 3]);
                        else
                            sink.AddBezier(new BezierSegment { Point1 = pts[i * 3 + 1], Point2 = pts[i * 3 + 2], Point3 = pts[i * 3 + 3] });
                    }
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();
                    _devices.DeviceContext.DrawGeometry(geometry, _lineBrush, 3.0f);
                }

                // ハンドルの描画（使用セグメント分のみ）
                for (int i = 0; i < segmentCount; i++)
                {
                    if (!isLinear[i])
                    {
                        _devices.DeviceContext.DrawLine(pts[i * 3],     pts[i * 3 + 1], _handleBrush, 2.0f);
                        _devices.DeviceContext.DrawLine(pts[i * 3 + 3], pts[i * 3 + 2], _handleBrush, 2.0f);
                    }
                }

                _devices.DeviceContext.EndDraw();
                _commandList.Close();
                _devices.DeviceContext.Target = oldTarget;

                _compositeEffect.SetInput(0, _transformEffect.Output, true);
                _compositeEffect.SetInput(1, _commandList, true);
            }

            return description.DrawDescription with
            {
                Controllers = [
                    ..description.DrawDescription.Controllers,
                    control
                ]
            };
        }

        public void Dispose()
        {
            _commandList?.Dispose();
            _lineBrush.Dispose();
            _handleBrush.Dispose();
            _transformEffect.Dispose();
            _compositeEffect.Dispose();
        }
    }
}