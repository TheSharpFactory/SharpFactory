#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateFunctions(ModelContainer modelContainer, Model model, Schema schema)
    {
        //function repositories are by schema. single file contains all functions for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.FunctionRepository;
        var baseClass = Common.Names.FuncRepositoryBase;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Repository, model, schema);
        var interfaceTypeName = Common.Names.IFunctionRepository;
        var interfaceTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        //var typeDeclaration = $"public partial class {typeName}";
        var entityTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Entity, model, schema);
        var repoCommonNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);

        var sqlClientNamespace = Common.CalculateSqlClientNamespace(modelContainer);

        var usingNamespaces = new List<UsingNamespace>(10)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = "System.Data" },
          new UsingNamespace { Name = sqlClientNamespace },
          new UsingNamespace { Name = entityTypeNamespace },
          new UsingNamespace { Name = interfaceTypeNamespace },
          new UsingNamespace { Name = repoCommonNamespace },
        };

        var summary = $"This class represents the Repository for all Functions under the {schema.Name} schema.";

        var type = new ClassDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            BaseClass = baseClass,
            Interfaces = new List<string> { interfaceTypeName },
            Summary = new List<string> { summary }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);

        CSharpFile.OpenClassWithNamespace(type);
        GenerateDefaultConstructorForSPFuncSeq(model, typeName);

        foreach(var func in schema.Functions)
        {
            GenerateFunction(func, model, schema, false);
            GenerateFunction(func, model, schema, true);
        }

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();

        return CSharpFile.ToString();
    }

    private static void GenerateFunction(Function func, Model model, Schema schema, bool isAsync)
    {
        RepoMethodSignatures.Functions.Generate(func, isAsync, false, false);
        CSharpFile.OpenBlock();

        var repoLookupFuncId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupFunctionId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, func.N)}";
        var paramVariable = "null";

        if(func.HPar)
        {
            #region Has Parameters
            CSharpFile.OpenRegion("Parameters");
            CSharpFile.Line($"var param = new SqlParameter[{func.Par.Count}]");
            var index = 0;
            foreach(var p in func.Par)
            {
                var typeInfo = Lookup.TypeInfo[p.TId];
                var val = string.Empty;

                if(!p.Str)
                    val = $"request.{p.N}.HasValue ? request.{p.N}.Value : (object)DBNull.Value";
                else
                    val = $"request.{p.N} != null ? request.{p.N} : (object)DBNull.Value";

                var sizeValue = string.Empty;
                if(p.S.HasValue)
                    sizeValue = ", " + p.S.Value.ToString();

                CSharpFile.Line($"param[{index}] = new SqlParameter(\"{p.SN}\", SqlDbType.{typeInfo.SqlDbType}{sizeValue}){{ Value = {val} }};", " index {index}");
                index++;
            }

            CSharpFile.CloseRegion();
            paramVariable = "param";
            #endregion
        }

        var await = isAsync ? "await " : "";
        var asyncSuffix = isAsync ? "Async" : "";

        if(func.REn)
        {
            GenerateRoutineMaterializer(func.En);
            CSharpFile.Line($"{await}{RepoMethodNames.Function}{asyncSuffix}({repoLookupFuncId}, false, {paramVariable}, materialize)");
            CSharpFile.Return("result.Count > 0 ? result : null");
        }
        else
        {
            GenerateRoutineScalarMaterializer(func.Sc);
            CSharpFile.Line($"{await}{RepoMethodNames.Function}{asyncSuffix}({repoLookupFuncId}, true, {paramVariable}, materialize)");
            CSharpFile.Return("result");
        }

        CSharpFile.CloseBlock();
    }
}
