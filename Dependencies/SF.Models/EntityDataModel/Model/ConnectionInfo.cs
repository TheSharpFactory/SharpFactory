namespace SF.Models;

public sealed class ConnectionInfo : ModelBase
{
    private string _server = null;
    private string _dataBase = null;
    private string _user = null;
    private string _password = null;
    private bool? _disableIntegratedSecurity = false;

    public string Server
    {
        get { return _server; }
        set
        {
            if(_server != value)
            {
                _server = value;
                OnPropertyChanged(nameof(Server));
            }
        }
    }
    public string Database
    {
        get { return _dataBase; }
        set
        {
            if(_dataBase != value)
            {
                _dataBase = value;
                OnPropertyChanged(nameof(Database));
            }
        }
    }
    public string User
    {
        get { return _user; }
        set
        {
            if(_user != value)
            {
                _user = value;
                OnPropertyChanged(nameof(User));
            }
        }
    }
    public string Password
    {
        get { return _password; }
        set
        {
            if(_password != value)
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
    }
    public bool? DisableIntegratedSecurity
    {
        get { return _disableIntegratedSecurity; }
        set
        {
            if(_disableIntegratedSecurity != value)
            {
                _disableIntegratedSecurity = value;
                OnPropertyChanged(nameof(DisableIntegratedSecurity));
            }
        }
    }
    public ConnectionInfo Clone()
    {
        var result = new ConnectionInfo
        {
            Server = _server,
            Database = _dataBase,
            User = _user,
            Password = _password,
            DisableIntegratedSecurity = _disableIntegratedSecurity
        };
        return result;
    }
}
