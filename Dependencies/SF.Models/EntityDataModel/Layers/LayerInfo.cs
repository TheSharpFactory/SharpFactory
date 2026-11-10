namespace SF.Models;

public sealed class LayerInfo : ModelBase
{
    private string _projectName = null;
    private string _namespace = null;

    public string Project
    {
        get { return _projectName; }
        set
        {
            if(_projectName != value)
            {
                _projectName = value;
                OnPropertyChanged(nameof(Project));
            }
        }
    }
    public string Namespace
    {
        get { return _namespace; }
        set
        {
            if(_namespace != value)
            {
                _namespace = value;
                OnPropertyChanged(nameof(Namespace));
                OnPropertyChanged(nameof(FullNamespace));
            }
        }
    }
    public string FullProjectName(CommonSettings commonSettings)
    {
        if(string.IsNullOrWhiteSpace(commonSettings.RootNamespace) || commonSettings.UseRootNSInProjectName == YesOrNo.No)
            return _projectName;
        return $"{commonSettings.RootNamespace}.{_projectName}";
    }
    public string FullNamespace(CommonSettings commonSettings)
    {
        if(string.IsNullOrWhiteSpace(commonSettings.RootNamespace))
            return _namespace;
        return $"{commonSettings.RootNamespace}.{_namespace}";
    }

    public LayerInfo Clone()
    {
        var result = new LayerInfo
        {
            Project = _projectName,
            Namespace = _namespace,
        };
        return result;
    }
}
