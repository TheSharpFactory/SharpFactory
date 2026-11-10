namespace SF.Models;

public sealed class ModelLayers : ModelBase
{
    private LayerInfo _entities = new LayerInfo();//these are the entities    
    private LayerInfo _propEnums = new LayerInfo();//property enums
    private LayerInfo _navPropEnums = new LayerInfo();//nav property enums
    private LayerInfo _entityUtils = new LayerInfo();//comparing dtos, merging dto, merging keys, cloning dtos
    private LayerInfo _repositories = new LayerInfo();//repository classes   
    private LayerInfo _iRepository = new LayerInfo();//repository interfaces

    public LayerInfo Entity
    {
        get
        {
            return _entities;
        }
        set
        {
            if(_entities != value)
            {
                _entities = value;
                OnPropertyChanged(nameof(Entity));
            }
        }
    }
    public LayerInfo PropEnum
    {
        get
        {
            return _propEnums;
        }
        set
        {
            if(_propEnums != value)
            {
                _propEnums = value;
                OnPropertyChanged(nameof(PropEnum));
            }
        }
    }
    public LayerInfo NavPropEnum
    {
        get
        {
            return _navPropEnums;
        }
        set
        {
            if(_navPropEnums != value)
            {
                _navPropEnums = value;
                OnPropertyChanged(nameof(NavPropEnum));
            }
        }
    }
    public LayerInfo EntityUtil
    {
        get
        {
            return _entityUtils;
        }
        set
        {
            if(_entityUtils != value)
            {
                _entityUtils = value;
                OnPropertyChanged(nameof(EntityUtil));
            }
        }
    }
    public LayerInfo Repository
    {
        get
        {
            return _repositories;
        }
        set
        {
            if(_repositories != value)
            {
                _repositories = value;
                OnPropertyChanged(nameof(Repository));
            }
        }
    }
    public LayerInfo IRepository
    {
        get
        {
            return _iRepository;
        }
        set
        {
            if(_iRepository != value)
            {
                _iRepository = value;
                OnPropertyChanged(nameof(IRepository));
            }
        }
    }

    public ModelLayers Clone()
    {
        var result = new ModelLayers
        {
            Entity = _entities.Clone(),
            PropEnum = _propEnums.Clone(),
            NavPropEnum = _navPropEnums.Clone(),
            EntityUtil = _entityUtils.Clone(),
            Repository = _repositories.Clone(),
            IRepository = _iRepository.Clone(),
        };
        return result;
    }
}
