#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    #region Sequences
    public static string GenerateSequences(ModelContainer modelContainer, Model model, Schema schema)
    {//function repositories are by schema. single file contains all functions for that schema.
        CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
        var typeName = Common.Names.SequenceRepository;
        var baseClass = Common.Names.SeqRepositoryBase;
        var typeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.Repository, model, schema);
        var interfaceTypeName = Common.Names.ISequenceRepository;
        var interfaceTypeNamespace = Common.GenerateTypeNamespace(modelContainer.CommonSettings, model.Layers.IRepository, model, schema);
        var repoCommonNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);

        var sqlClientNamespace = Common.CalculateSqlClientNamespace(modelContainer);

        var usingNamespaces = new List<UsingNamespace>(5)
        {
          new UsingNamespace { Name = "System" },
          new UsingNamespace { Name = "System.Collections.Generic" },
          new UsingNamespace { Name = "System.Threading.Tasks" },
          new UsingNamespace { Name = sqlClientNamespace },
          new UsingNamespace { Name = interfaceTypeNamespace },
          new UsingNamespace { Name = repoCommonNamespace }
        };

        var summary = $"This class represents the Repository for all Sequences under the {schema.Name} schema.";

        var type = new ClassDeclaration
        {
            Name = typeName,
            Namespace = typeNamespace,
            IsPartial = true,
            Summary = new List<string>(1) { summary },
            Interfaces = new List<string> { baseClass, interfaceTypeName }
        };

        CSharpFile.Header(type, Common.SharpFactoryFileMarker);
        CSharpFile.UsingStatements(usingNamespaces);
        CSharpFile.OpenClassWithNamespace(type);
        GenerateDefaultConstructorForSPFuncSeq(model, typeName);
        foreach(var seq in schema.Sequences)
        {
            var repoLookupSeqId = $"{Common.Names.RepositoryLookup}.{Common.Names.RepositoryLookupSequenceId}.{Common.GenerateRepositoryLookupModelObjectIdsFieldName(model.Name, schema.Name, seq.N)}";

            RepoMethodSignatures.Sequences.Generate(seq, false, false, false);
            CSharpFile.OpenBlock();
            CSharpFile.Return($"{RepoMethodNames.Sequence}<{seq.Sc.T}>({repoLookupSeqId})");
            CSharpFile.CloseBlock();

            RepoMethodSignatures.Sequences.Generate(seq, true, false, false, skipAwait: true);//only hands the call on: returns the inner Task
            CSharpFile.OpenBlock();
            CSharpFile.Return($"{RepoMethodNames.Sequence}Async<{seq.Sc.T}>({repoLookupSeqId})");
            CSharpFile.CloseBlock();
        }

        CSharpFile.CloseTypeWithNamespace();
        CSharpFile.Footer();
        return CSharpFile.ToString();
    }

    #endregion
}
