using System.Reflection;
using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Views.Converters;

namespace Curver
{
    internal class CurveEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new CurveEditor();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            var editor = (CurveEditor)control;
            editor.SetBinding(CurveEditor.CurveDataProperty, ItemPropertiesBinding.Create2(itemProperties));
        }

        public override void ClearBindings(FrameworkElement control)
        {
            BindingOperations.ClearBinding(control, CurveEditor.CurveDataProperty);
        }
    }
}
