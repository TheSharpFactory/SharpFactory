#region Usings
using SF.Common.Versioning;
#endregion

namespace SF.App;

//the app's version: the assembly's, which the build takes from the VERSION file (Directory.Build.props)
internal sealed class Versionable : IVersionable
{
    public string CurrentVersion
    {
        get
        {
            return typeof(Versionable).Assembly.GetName().Version.ToString();
        }
    }
}
