namespace SF.Models;

public sealed class CommonLayers : ModelBase
{
    private LayerInfo _repository = new LayerInfo();//base classes, lookups, constants and any code shared across all repositories
    private LayerInfo _repoContainer = new LayerInfo();//the actual classes that hold the instances of the repository. one per model and one for all.
    private LayerInfo _iRepoContainer = new LayerInfo();//the interface that holds a reference to the IRepoContainer interfaces for each model as properties
    private LayerInfo _filters = new LayerInfo();//classes for filter and sort filter, as well as the paging classes
    private LayerInfo _exceptions = new LayerInfo();//exceptions, ex utils   

    public LayerInfo Repository
    {
        get
        {
            return _repository;
        }
        set
        {
            if(_repository != value)
            {
                _repository = value;
                OnPropertyChanged(nameof(Repository));
            }
        }
    }
    public LayerInfo RepoContainer
    {
        get
        {
            return _repoContainer;
        }
        set
        {
            if(_repoContainer != value)
            {
                _repoContainer = value;
                OnPropertyChanged(nameof(RepoContainer));
            }
        }
    }
    public LayerInfo IRepoContainer
    {
        get
        {
            return _iRepoContainer;
        }
        set
        {
            if(_iRepoContainer != value)
            {
                _iRepoContainer = value;
                OnPropertyChanged(nameof(IRepoContainer));
            }
        }
    }
    public LayerInfo Query
    {
        get
        {
            return _filters;
        }
        set
        {
            if(_filters != value)
            {
                _filters = value;
                OnPropertyChanged(nameof(Query));
            }
        }
    }
    public LayerInfo Exceptions
    {
        get
        {
            return _exceptions;
        }
        set
        {
            if(_exceptions != value)
            {
                _exceptions = value;
                OnPropertyChanged(nameof(Exceptions));
            }
        }
    }

    public CommonLayers Clone()
    {
        var result = new CommonLayers
        {
            Repository = _repository.Clone(),
            RepoContainer = _repoContainer.Clone(),
            IRepoContainer = _iRepoContainer.Clone(),
            Query = _filters.Clone(),
            Exceptions = _exceptions.Clone(),
        };
        return result;
    }
}
