using System;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;
 namespace TextBezier
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

        // 曲線の長さを基準とした t の算出
        private float GetNormalizedT(float targetProgress, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            int segments = 100; // 100分割して曲線の長さを測る
            float[] lengths = new float[segments + 1];
            float totalLength = 0;
            Vector2 prevPoint = p0;

            lengths[0] = 0;
            for (int i = 1; i <= segments; i++)
            {
                float tempT = i / (float)segments;
                float u = 1f - tempT;
                Vector2 pt = (u * u * u) * p0 + 3 * (u * u) * tempT * p1 + 3 * u * (tempT * tempT) * p2 + (tempT * tempT * tempT) * p3;
                totalLength += Vector2.Distance(prevPoint, pt);
                lengths[i] = totalLength;
                prevPoint = pt;
            }

            // 目的の進捗（0.0～1.0）を、実際の長さに変換
            float targetLength = targetProgress * totalLength;

            // 範囲外の処理
            if (targetLength <= 0) return 0f;
            if (targetLength >= totalLength) return 1f;

            // どの区間に収まっているかを探して、正確な t を補間する
            for (int i = 0; i < segments; i++)
            {
                if (targetLength >= lengths[i] && targetLength <= lengths[i + 1])
                {
                    float segmentLength = lengths[i + 1] - lengths[i];
                    float fraction = (segmentLength > 0) ? (targetLength - lengths[i]) / segmentLength : 0;
                    return (i + fraction) / segments;
                }
            }
            return 1f;
        }

        public DrawDescription Update(EffectDescription description)
        {
            var frame = description.ItemPosition.Frame;
            var length = description.ItemDuration.Frame;
            var fps = description.FPS;

            // 各座標の取得
            float p0x = (float)_effect.P0X.GetValue(frame, length, fps);
            float p0y = (float)_effect.P0Y.GetValue(frame, length, fps);
            float p1x = (float)_effect.P1X.GetValue(frame, length, fps);
            float p1y = (float)_effect.P1Y.GetValue(frame, length, fps);
            float p2x = (float)_effect.P2X.GetValue(frame, length, fps);
            float p2y = (float)_effect.P2Y.GetValue(frame, length, fps);
            float p3x = (float)_effect.P3X.GetValue(frame, length, fps);
            float p3y = (float)_effect.P3Y.GetValue(frame, length, fps);

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
                // 中心を基準に文字を前後に配置
                float offsetIndex = index - (count - 1) / 2f;

                // 1文字あたりの間隔
                targetProgress += offsetIndex * (spacing * 0.1f);
            }

            Vector2 p0 = new Vector2(p0x, p0y);
            Vector2 p1 = new Vector2(p1x, p1y);
            Vector2 p2 = new Vector2(p2x, p2y);
            Vector2 p3 = new Vector2(p3x, p3y);

            double P0x = p0x;
            double P0y = p0y;
            double P1x = p1x;
            double P1y = p1y;
            double P2x = p2x;
            double P2y = p2y;
            double P3x = p3x;
            double P3y = p3y;
           
            var control =
            new VideoEffectController(
                _effect,
                [
                    new ControllerPoint(
                        new((float)P0x, (float)P0y, 0f),
                        x =>
                        {
                            _effect.P0X.AddToEachValues(x.Delta.X);
                            _effect.P0Y.AddToEachValues(x.Delta.Y);
                        }),

                    new ControllerPoint(
                        new((float)P1x, (float)P1y, 0f),
                        x =>
                        {
                            _effect.P1X.AddToEachValues(x.Delta.X);
                            _effect.P1Y.AddToEachValues(x.Delta.Y);
                        }),

                    new ControllerPoint(
                        new((float)P2x, (float)P2y, 0f),
                        x =>
                        {
                            _effect.P2X.AddToEachValues(x.Delta.X);
                            _effect.P2Y.AddToEachValues(x.Delta.Y);
                        }),

                    new ControllerPoint(
                        new((float)P3x, (float)P3y, 0f),
                        x =>
                        {
                            _effect.P3X.AddToEachValues(x.Delta.X);
                            _effect.P3Y.AddToEachValues(x.Delta.Y);
                        })
                ]);


            // 等間隔になるよう t を補正
            float t = GetNormalizedT(targetProgress, p0, p1, p2, p3);

            float u = 1f - t;

            // 現在地と角度の計算
            Vector2 position = (u * u * u) * p0 + 3 * (u * u) * t * p1 + 3 * u * (t * t) * p2 + (t * t * t) * p3;
            float angle = Angle_Degree * (MathF.PI / 180f); ;
            Vector2 Scale2D = new(Scale, Scale);
            if (_effect.AutoRotate)
            {
                Vector2 tangent = 3 * (u * u) * (p1 - p0) + 6 * u * t * (p2 - p1) + 3 * (t * t) * (p3 - p2);
                angle += MathF.Atan2(tangent.Y, tangent.X);
            }

            // 画像を変形（移動・回転）
            Matrix3x2 matrix;
            
            // 曲線範囲内の場合の変形マトリックス
            if (targetProgress >= 0f && targetProgress <= 1f)
            {
                matrix = Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(position.X, position.Y)*Matrix3x2.CreateScale(Scale2D);
            }
            else
            {
                // 範囲外の場合は画面外へ移動させ非表示
                matrix = Matrix3x2.CreateTranslation(-99999f, -99999f);
            }

            _transformEffect.SetValue((int)AffineTransform2DProperties.TransformMatrix, matrix);

            // ガイド描画判定
            _isGuideDrawn = _effect.ShowGuide && description.Usage != TimelineSourceUsage.Exporting;

            // ガイド描画処理
            if (_isGuideDrawn)
            {
                // コマンドリストの初期化
                _commandList?.Dispose();
                _commandList = _devices.DeviceContext.CreateCommandList();

                // 描画ターゲットの変更
                var oldTarget = _devices.DeviceContext.Target;
                _devices.DeviceContext.Target = _commandList;
                _devices.DeviceContext.BeginDraw();
                _devices.DeviceContext.Clear(null); // 透明で塗りつぶし

                // ベジェ曲線パスの描画
                using (var geometry = _devices.DeviceContext.Factory.CreatePathGeometry())
                using (var sink = geometry.Open())
                {
                    sink.BeginFigure(p0, FigureBegin.Hollow);
                    sink.AddBezier(new BezierSegment { Point1 = p1, Point2 = p2, Point3 = p3 });
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();

                    _devices.DeviceContext.DrawGeometry(geometry, _lineBrush, 3.0f);
                }

                // ハンドルの描画
                _devices.DeviceContext.DrawLine(p0, p1, _handleBrush, 2.0f);
                _devices.DeviceContext.DrawLine(p3, p2, _handleBrush, 2.0f);

                // 描画ターゲットの復元
                _devices.DeviceContext.EndDraw();
                _commandList.Close();
                _devices.DeviceContext.Target = oldTarget;

                // 合成エフェクトの入力設定
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
            // リソースの解放
            _commandList?.Dispose();
            _lineBrush.Dispose();
            _handleBrush.Dispose();
            _transformEffect.Dispose();
            _compositeEffect.Dispose();
        }
    }
}