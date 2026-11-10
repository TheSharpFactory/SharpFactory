#region Usings
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateStoredProcs(ModelContainer modelContainer, Model model, Schema schema)
    {//stored procedure repositories are by schema. single file contains all stored procedures for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.StoredProcRepository;
        var baseClass = Common.Names.StoredProcRepositoryBase;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Repository, model, schema);
        var interfaceTypeName = Common.Names.IStoredProcRepository;
        var interfaceTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        var entityTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);
        var filterNamespace = modelContainer.CommonLayers.Query.FullNamespace(modelContainer.CommonSettings);
        var propEnumNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.PropEnum, model, schema);
        var repoCommonNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);
        //var repoLookupModelId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupModelId}";

        var sqlClientNamespace = Common.CalculateSqlClientNamespace(modelContainer);

        var usingNamespaces = new List<UsingNamespace>(10)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = sqlClientNamespace },
          new UsingNamespace { Name = entityTypeNamespace },
          new UsingNamespace { Name = interfaceTypeNamespace },
          new UsingNamespace { Name = repoCommonNamespace },
          new UsingNamespace { Name = propEnumNamespace },
          new UsingNamespace { Name = filterNamespace }
        };

        var summary = $"This class represents the Repository for all Stored Procedures under the {schema.Name} schema.";

        var type = new ClassDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Interfaces = new List<string>(2) { baseClass, interfaceTypeName },
            Summary = new List<string>(1) { summary }
        };
        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenClassWithNamespace(type);
        GenerateDefaultConstructorForSPFuncSeq(model, typeName);

        foreach(var proc in schema.StoredProcs)
        {
            GenerateStoredProc(proc, model, schema, false);
            GenerateStoredProc(proc, model, schema, true);
        }

        CSharpFile.CloseClassWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }

    private static void GenerateStoredProc(StoredProc proc, Model model, Schema schema, bool isAsync)
    {
        var queryFilter = Common.Names.QueryFilter;
        var filterConditionEnum = Common.Names.FilterConditionsEnum;
        var await = isAsync ? "await " : "";
        var asyncSuffix = isAsync ? "Async" : "";

        RepoMethodSignatures.StoredProcs.Generate(proc, isAsync, false, false);
        CSharpFile.OpenBlock();
        var repoLookupProcId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupStoredProcId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, proc.N)}";
        if(proc.HPar)
        {
            #region Has Parameters
            var propEnum = Common.GeneratePropEnumName(proc);
            var propClass = Common.GeneratePropClassName(proc);
            var filterCollection = $"List<{Common.Names.IQueryFilter}>";
            var paramList = GenerateParametersForStoredProcCallHelper(proc.Par, queryFilter, propClass, filterConditionEnum, "request");
            CSharpFile.OpenRegion("Parameters");
            CSharpFile.Line($"var parameters = new {filterCollection}{{ {paramList} }}");
            CSharpFile.CloseRegion();

            if(proc.REn)
            {
                GenerateRoutineMaterializer(proc.En);

                CSharpFile.Line($"var procReturnVals = {await}{RepoMethodNames.StoredProcedure}{asyncSuffix}({repoLookupProcId}, parameters, materialize)");
            }
            else
            {
                CSharpFile.Line($"var procReturnVals = {await}{RepoMethodNames.StoredProcedure}{asyncSuffix}({repoLookupProcId}, parameters, null)");
            }

            var outputParams = new List<Property>();
            foreach(var p in proc.Par)
            {
                if(p.M.HasValue && (p.M.Value == ParamMode.InOut || p.M.Value == ParamMode.Out))
                    outputParams.Add(p);
            }
            // assign all the ouput params
            if(outputParams?.Count > 0)
            {
                CSharpFile.OpenRegion("Assign Ouput Values");
                foreach(var p in outputParams)
                {
                    if(!p.Str)
                        CSharpFile.Line($"request.{p.N} = procReturnVals[(int){propEnum}.{p.N}] as {p.T}?");
                    else
                        CSharpFile.Line($"request.{p.N} = procReturnVals[(int){propEnum}.{p.N}] as string");
                }

                CSharpFile.CloseRegion();
            }

            if(proc.REn)
            {
                CSharpFile.LineNoEnding($"var procResult = new {Common.Names.StoredProcResult}<{proc.En.N}>");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"ReturnValue = (int)procReturnVals[ReturnParamId],");
                CSharpFile.Line("Result = result.Count > 0 ? result : null,");
                CSharpFile.CloseBlockWithSemicolon();
                CSharpFile.Return("procResult");
            }
            else
            {
                CSharpFile.Return($"(int)procReturnVals[ReturnParamId]");
            }
            #endregion
        }
        else
        {
            #region No Parameters
            if(proc.REn)
            {
                GenerateRoutineMaterializer(proc.En);
                CSharpFile.Line($"var procReturnVals = {await}{RepoMethodNames.StoredProcedure}{asyncSuffix}({repoLookupProcId}, null, materialize)");
                CSharpFile.LineNoEnding($"var procResult = new {Common.Names.StoredProcResult}<{proc.En.N}>");
                CSharpFile.OpenBlock();
                CSharpFile.Line($"ReturnValue = (int)procReturnVals[ReturnParamId],");
                CSharpFile.Line("Result = result.Count > 0 ? result : null,");
                CSharpFile.CloseBlockWithSemicolon();
                CSharpFile.Return("procResult");
            }
            else
            {
                CSharpFile.Line($"var procReturnVals = {await}{RepoMethodNames.StoredProcedure}{asyncSuffix}({repoLookupProcId}, null, null)");
                CSharpFile.Return($"(int)procReturnVals[ReturnParamId]");
            }
            #endregion
        }

        CSharpFile.CloseBlock();
    }

    private static void GenerateDefaultConstructorForSPFuncSeq(Model model, string typeName)
    {
        var repoLookupModelId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupModelId}";

        CSharpFile.OpenRegion("Constructors");

        CSharpFile.Constructor(new ConstructorDeclaration
        {
            Name = typeName,
            AccessModifier = AccessModifiers.Public,
        });

        CSharpFile.OpenBlock();
        CSharpFile.Comment("this is the unique id for the model to which this repository belongs to. used to find the correct connection string");
        CSharpFile.Line($"{ModelIdField} = {repoLookupModelId}.{model.Name}");
        CSharpFile.CloseBlock();
        CSharpFile.CloseRegion();
    }
    private static string GenerateParametersForStoredProcCallHelper<TModelMember>(ObservableCollection<TModelMember> props, string queryFilter, string propEnum, string filterConditionEnum, string entityParam) where TModelMember : IModelMember
    {
        var keys = $"{queryFilter}.New({propEnum}.{props[0].N}, {filterConditionEnum}.Equals, {entityParam}.{props[0].N})";
        var propCount = props.Count;
        for(var i = 1; i < propCount; i++)
        {
            keys += $", {queryFilter}.New({propEnum}.{props[i].N}, {filterConditionEnum}.Equals, {entityParam}.{props[i].N})";
        }

        return keys;
    }
}
