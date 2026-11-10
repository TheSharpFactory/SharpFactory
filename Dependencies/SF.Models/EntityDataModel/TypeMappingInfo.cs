#region Usings
#endregion;

namespace SF.Models;

public sealed class TypeMappingInfo
{
    public string NetType { get; set; }
    public string StoreType { get; set; }
    //this is used for the SqlParamater SqlDbType property
    public string SqlDbType { get; set; }//this is the string representation of the ENUM SqlDbType
    public string ReaderMethod { get; set; }//for example GetInt32
}
