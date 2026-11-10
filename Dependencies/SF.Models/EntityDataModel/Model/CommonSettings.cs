#region Usings
using System;
#endregion

namespace SF.Models;

public sealed class CommonSettings : ModelBase
{
    private string _dependenciesFolder = "";
    private string _rootNamespace = "";
    private int _indentSize = 0;
    private YesOrNo _useRootNSInProjectName = YesOrNo.Yes;
    private TargetFrameworks? _targetFramework = null;

    public YesOrNo UseRootNSInProjectName
    {
        get
        {
            return _useRootNSInProjectName;
        }
        set
        {
            if(_useRootNSInProjectName != value)
            {
                _useRootNSInProjectName = value;
                OnPropertyChanged(nameof(UseRootNSInProjectName));
            }
        }
    }
    public string RootNamespace
    {
        get
        {
            return _rootNamespace;
        }
        set
        {
            if(_rootNamespace != value)
            {
                _rootNamespace = value;
                OnPropertyChanged(nameof(RootNamespace));
            }
        }
    }
    public string DependenciesFolder
    {
        get
        {
            return _dependenciesFolder;
        }
        set
        {
            if(_dependenciesFolder != value)
            {
                _dependenciesFolder = value;
                OnPropertyChanged(nameof(DependenciesFolder));
            }
        }
    }
    public int IndentSize
    {
        get
        {
            return _indentSize;
        }
        set
        {
            if(_indentSize != value)
            {
                _indentSize = value;

                OnPropertyChanged(nameof(IndentSize));
            }
        }
    }
    public TargetFrameworks? TargetFramework
    {
        get
        {
            //models that never picked a target framework default to .NET 10
            return _targetFramework ?? TargetFrameworks.NET10;
        }
        set
        {
            //models saved with a framework that is no longer supported load as .NET 10
            if(value.HasValue && !Enum.IsDefined(typeof(TargetFrameworks), value.Value))
                value = TargetFrameworks.NET10;

            if(_targetFramework != value)
            {
                _targetFramework = value;
                OnPropertyChanged(nameof(TargetFramework));
            }
        }
    }

    public CommonSettings Clone()
    {
        var result = new CommonSettings
        {
            DependenciesFolder = _dependenciesFolder,
            IndentSize = _indentSize,
            RootNamespace = _rootNamespace,
            UseRootNSInProjectName = _useRootNSInProjectName,
            TargetFramework = _targetFramework,
        };
        return result;
    }
}
