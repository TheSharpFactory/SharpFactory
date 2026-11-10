namespace SF.Models;

public sealed class ModelContainer : ModelBase
{
    private CommonSettings _commonSettings = null;
    private CommonLayers _commonLayers = null;
    private ModelCollection _models = new ModelCollection();

    public CommonSettings CommonSettings
    {
        get
        {
            return _commonSettings;
        }
        set
        {
            if(_commonSettings != value)
            {
                _commonSettings = value;

                if(_commonSettings != null)
                {
                    if(_commonSettings.DependenciesFolder != null)
                        _commonSettings.DependenciesFolder = _commonSettings.DependenciesFolder.Trim();

                    if(_commonSettings.RootNamespace != null)
                        _commonSettings.RootNamespace = _commonSettings.RootNamespace.Trim();
                }

                OnPropertyChanged(nameof(CommonSettings));
            }
        }
    }
    public CommonLayers CommonLayers
    {
        get
        {
            return _commonLayers;
        }
        set
        {
            if(_commonLayers != value)
            {
                _commonLayers = value;

                if(_commonLayers != null)
                {
                    SanitizeLayer(_commonLayers.Exceptions);
                    SanitizeLayer(_commonLayers.Query);
                    SanitizeLayer(_commonLayers.IRepoContainer);
                    SanitizeLayer(_commonLayers.RepoContainer);
                    SanitizeLayer(_commonLayers.Repository);
                }

                OnPropertyChanged(nameof(CommonLayers));
            }
        }
    }
    public ModelCollection Models
    {
        get
        {
            return _models;
        }
        set
        {
            if(_models != value)
            {
                _models = value;

                if(_models?.Count > 0)
                {
                    foreach(var m in _models)
                    {
                        if(m.Layers != null)
                        {
                            SanitizeLayer(m.Layers.Entity);
                            SanitizeLayer(m.Layers.EntityUtil);
                            SanitizeLayer(m.Layers.IRepository);
                            SanitizeLayer(m.Layers.NavPropEnum);
                            SanitizeLayer(m.Layers.PropEnum);
                            SanitizeLayer(m.Layers.Repository);
                        }
                    }
                }

                OnPropertyChanged(nameof(Models));
            }
        }
    }
    public int ObjectCount
    {
        get
        {
            var result = 0;

            foreach(var m in _models)
            {
                result += m.ObjectCount;
            }

            return result;
        }
    }
    public bool HasEntities
    {
        get
        {
            foreach(var m in _models)
            {
                if(m.HasEntities)
                    return true;
            }

            return false;
        }
    }
    public bool HasStoredProcs
    {
        get
        {
            foreach(var m in _models)
            {
                if(m.HasStoredProcs)
                    return true;
            }

            return false;
        }
    }
    public bool HasStoredProcEntities
    {
        get
        {
            foreach(var m in _models)
            {
                if(m.HasStoredProcEntities)
                    return true;
            }

            return false;
        }
    }
    public bool HasFunctions
    {
        get
        {
            foreach(var m in _models)
            {
                if(m.HasFunctions)
                    return true;
            }

            return false;
        }
    }
    public bool HasViews
    {
        get
        {
            foreach(var m in _models)
            {
                if(m.HasViews)
                    return true;
            }

            return false;
        }
    }

    private static void SanitizeLayer(LayerInfo layer)
    {
        if(layer == null)
            return;

        if(layer.Namespace != null)
            layer.Namespace = layer.Namespace.Trim().Replace(" ", "_");

        if(layer.Project != null)
            layer.Project = layer.Project.Trim().Replace(" ", "_");
    }
}
