using System.Windows;

namespace Curver
{
    public partial class CurveEditorWindow : Window
    {
        private VelocityCurve originalCurve;
        private VelocityCurve workingCurve;

        public VelocityCurve ResultCurve { get; private set; }

        public CurveEditorWindow(VelocityCurve curve)
        {
            InitializeComponent();

            originalCurve = curve;
            workingCurve = VelocityCurve.Deserialize(curve.Serialize());
            ResultCurve = originalCurve;

            LargeCurveEditor.SetCurve(workingCurve);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResultCurve = workingCurve;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ResultCurve = originalCurve;
            DialogResult = false;
            Close();
        }
    }
}
