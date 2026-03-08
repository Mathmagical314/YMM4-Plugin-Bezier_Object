using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace Curver
{
    public partial class CurveEditor : UserControl, IPropertyEditorControl
    {
        public string CurveData
        {
            get { return (string)GetValue(CurveDataProperty); }
            set { SetValue(CurveDataProperty, value); }
        }

        public static readonly DependencyProperty CurveDataProperty =
            DependencyProperty.Register(nameof(CurveData), typeof(string), typeof(CurveEditor),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurveDataChanged));

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public CurveEditor()
        {
            InitializeComponent();
            InlineEditor.CurveChanged += InlineEditor_CurveChanged;
            
            InlineEditor.CurveCanvas.MouseLeftButtonDown += (s, e) => BeginEdit?.Invoke(this, EventArgs.Empty);
            InlineEditor.CurveCanvas.MouseLeftButtonUp += (s, e) => EndEdit?.Invoke(this, EventArgs.Empty);
        }

        private static void OnCurveDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && e.NewValue is string data)
            {
                var curve = VelocityCurve.Deserialize(data);
                editor.InlineEditor.SetCurve(curve);
            }
        }

        private void InlineEditor_CurveChanged(object? sender, EventArgs e)
        {
            CurveData = InlineEditor.GetCurve().Serialize();
        }

        private void OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var curve = VelocityCurve.Deserialize(CurveData);
            var window = new CurveEditorWindow(curve)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                BeginEdit?.Invoke(this, EventArgs.Empty);
                CurveData = window.ResultCurve.Serialize();
                InlineEditor.SetCurve(window.ResultCurve);
                EndEdit?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
