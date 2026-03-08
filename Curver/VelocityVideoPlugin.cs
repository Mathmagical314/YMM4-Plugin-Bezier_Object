using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace Curver
{
    public class VelocityVideoPlugin : IShapePlugin
    {
        public string Name => "AiuCurver";

        public bool IsExoShapeSupported => false;
        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new VelocityVideoParameter(sharedData);
        }
    }
}
