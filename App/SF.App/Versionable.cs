#region Usings
using SF.Common.Versioning;
#endregion

namespace SF.App;

//the app's version, kept in step with the VERSION file
internal sealed class Versionable : IVersionable
{
    private const string _version = "1.1.8.9";
    public string CurrentVersion
    {
        get
        {
            return _version;
        }
    }
}
