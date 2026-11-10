#region Usings
using SF.Models;
using System;
using System.Runtime.CompilerServices;
#endregion

namespace SF.CodeGeneration;

public static partial class Common
{
    public static readonly string SharpFactoryFileMarker = "/*SFF*/";

    internal static readonly string[] _supportedFilterConditions =
    {
  #region Filter Conditions
		  "Contains",
  "DoesNotContain",
  "Equals",
  "NotEquals",
  "StartsWith",
  "DoesNotStartWith",
  "EndsWith",
  "DoesNotEndWith",
  "GreaterThan",
  "LessThan",
  "GreaterThanOrEquals",
  "LessThanOrEquals",
  //this is the date possible operations      
  "DateEquals",
  "DateNotEquals",
  "DateGreaterThan",
  "DateLessThan",
  "DateGreaterThanOrEquals",
  "DateLessThanOrEquals",
  "DateContains",
  "DateDoesNotContain",
  //this is the year possible operations      
  "YearEquals",
  "YearNotEquals",
  "YearGreaterThan",
  "YearLessThan",
  "YearGreaterThanOrEquals",
  "YearLessThanOrEquals",
  "YearContains",
  "YearDoesNotContain",
  //this is the month possible operations      
  "MonthEquals",
  "MonthNotEquals",
  "MonthGreaterThan",
  "MonthLessThan",
  "MonthGreaterThanOrEquals",
  "MonthLessThanOrEquals",
  "MonthContains",
  "MonthDoesNotContain",
  //this is the day possible operations      
  "DayEquals",
  "DayNotEquals",
  "DayGreaterThan",
  "DayLessThan",
  "DayGreaterThanOrEquals",
  "DayLessThanOrEquals",
  "DayContains",
  "DayDoesNotContain" 
	    #endregion
};
    public static string GenerateTypeNamespace(CommonSettings commonSettings, LayerInfo layerInfo, Model model, Schema schema)//generates the namespace for an ENTITY
    {
        if(model == null)
            return $"{layerInfo.FullNamespace(commonSettings)}.{schema.Name}";
        return $"{layerInfo.FullNamespace(commonSettings)}.{model.Name}.{schema.Name}";
    }
    public static string GenerateTypeNamespace(CommonSettings commonSettings, LayerInfo layerInfo, Model model, string schema)//generates the namespace for an ENTITY
    {
        if(model == null)
            return $"{layerInfo.FullNamespace(commonSettings)}.{schema}";
        return $"{layerInfo.FullNamespace(commonSettings)}.{model.Name}.{schema}";
    }

    public static string GenerateTypeFullName(CommonSettings commonSettings, LayerInfo layerInfo, Model model, Schema schema, string entityName)
    {
        return GenerateTypeNamespace(commonSettings, layerInfo, model, schema) + "." + GenerateTypeName(entityName);
    }

    public static string GenerateTypeName(Entity entity)
    {
        return GenerateTypeName(entity.N);
    }
    public static string GenerateTypeName(string currentName)
    {
        return currentName;
    }
    public static string GenerateRequestTypeName(string currentName)
    {
        return currentName + "_Request";
    }

    public static string GenerateEntityUtilName(Entity entity)
    {
        return $"{entity.N}Utils";
    }
    public static string GeneratePropEnumName(Entity entity)
    {
        return GeneratePropEnumName(entity.N);
    }
    public static string GeneratePropEnumName(StoredProc entity)
    {
        return GeneratePropEnumName(GenerateRequestTypeName(entity.N));
    }
    public static string GeneratePropEnumName(string entityName)
    {
        return $"{entityName}Prop";
    }

    /// <summary>
    /// The property container of a table or view: PersonProperty.Age, one typed Prop per column.
    /// </summary>
    public static string GeneratePropClassName(Entity entity)
    {
        return $"{entity.N}Property";
    }

    /// <summary>
    /// The property container of a stored procedure request: Person_RequestProperty.Param.
    /// </summary>
    public static string GeneratePropClassName(StoredProc entity)
    {
        return $"{GenerateRequestTypeName(entity.N)}Property";
    }

    /// <summary>
    /// The property container of an entity known only by its name, such as the target of a navigation property.
    /// </summary>
    public static string GeneratePropClassName(string entityName)
    {
        return $"{entityName}Property";
    }

    public static string GenerateEqualityComparer(string forType)
    {
        return $"{forType}Comparer";
    }
    public static string GenerateNavPropEnumName(Entity entity)
    {
        return $"{entity.N}NavProp";
    }

    /// <summary>
    /// The navigation property container of a table: PersonNavProperty.Client.
    /// </summary>
    public static string GenerateNavPropClassName(Entity entity)
    {
        return $"{entity.N}NavProperty";
    }

    public static string GenerateIRepositoryName(Entity entity)
    {
        return "I" + GenerateRepositoryName(entity);
    }
    //public static string GenerateIRepositoryName(View entity)
    //{
    //  return "I" + GenerateRepositoryName(entity);
    //}
    public static string GenerateRepositoryName(Entity entity)
    {
        return entity.N + "Repository";
    }
    public static string CalculateSqlClientNamespace(ModelContainer modelContainer)
    {
        //every supported target framework (.NET 10 and later) uses Microsoft.Data.SqlClient
        return "Microsoft.Data.SqlClient";
    }
    public static string GenerateModelRepositoryContainerFullName(ModelContainer modelContainer, Model model)
    {
        return modelContainer.CommonLayers.RepoContainer.FullNamespace(modelContainer.CommonSettings) + "." + model.Name + "." + Names.ModelRepositoryContainer;
    }
    public static string GenerateModelRepositoryContainerInterfaceFullName(ModelContainer modelContainer, Model model)
    {
        return modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings) + "." + model.Name + "." + Names.IModelRepositoryContainer;
    }
    public static string GenerateSchemaRepositoryContainerInterfaceFullName(ModelContainer modelContainer, Model model, string schemaName)
    {
        return $"{modelContainer.CommonLayers.IRepoContainer.FullNamespace(modelContainer.CommonSettings)}.{model.Name}.{schemaName}.{Names.ISchemaRepositoryContainer}";
    }

    public static string GenerateRepositoryLookupModelObjectIdsFieldName(string modelName, string schemaName, string objectName)
    {
        return $"{modelName}_{schemaName}_{objectName}";
    }
    public static string GenerateRepositoryLookupEntityInfosFieldName()
    {
        return "Entities";
    }
    public static string GenerateRepositoryLookupViewInfosFieldName()
    {
        return "Views";
    }
    public static string GenerateRepositoryLookupStoredProcInfosFieldName()
    {
        return "StoredProcs";
    }
    public static string GenerateRepositoryLookupSequenceInfosFieldName()
    {
        return "Sequences";
    }
    public static string GenerateRepositoryLookupFunctionInfosFieldName()
    {
        return "Functions";
    }
    public static string GenerateRepositoryLookupModelIdFieldName()
    {
        return "Models";
    }

    public static string GetCategoryName(ModelObjectType objectType, [CallerMemberName] string memberName = "")
    {
        switch(objectType)
        {
            case ModelObjectType.Entities:
                return CategoryNames.Entities;
            case ModelObjectType.Views:
                return CategoryNames.Views;
            default:
                throw new ArgumentException($"{memberName} does not support Object Type {objectType}");
        }
    }
}
