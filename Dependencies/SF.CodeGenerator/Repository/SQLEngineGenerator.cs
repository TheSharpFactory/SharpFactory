#region Usings
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static Dictionary<string, string> GenerateSQLEngine(ModelContainer modelContainer)
    {
        return SqlEngine.Generate(modelContainer);
    }

    private static class SqlEngine
    {
        private static readonly string[] _fileNames = new string[]
        {
            "Database.cs",
            "DatabaseDelete.cs",
            "DatabaseFunction.cs",
            "DatabaseInsert.cs",
            "DatabaseSelect.cs",
            "DatabaseSequence.cs",
            "DatabaseStoredProc.cs",
            "DatabaseUpdate.cs"
        };

        private static readonly Dictionary<string, string> _fileContents = new Dictionary<string, string>(8);

        public static Dictionary<string, string> Generate(ModelContainer modelContainer)
        {
            LoadFileContents();

            return PrepareFiles(modelContainer);
        }

        private static void LoadFileContents()
        {
            var count = _fileNames.Length;

            //all files have been loaded
            if(_fileContents.Count == count)
                return;

            //get the assembly
            var assembly = Assembly.GetExecutingAssembly();

            for(var i = 0; i < count; i++)
            {
                var fileName = _fileNames[i];

                //skip the ones that have already been added
                if(_fileContents.ContainsKey(fileName))
                    continue;

                //component name plus the file name
                using(var stream = assembly.GetManifestResourceStream($"SF.CodeGeneration.{fileName}"))
                {
                    using(var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();

                        _fileContents.Add(fileName, content);
                    }
                }
            }
        }

        private static Dictionary<string, string> PrepareFiles(ModelContainer modelContainer)
        {
            var count = _fileNames.Length;

            var result = new Dictionary<string, string>(count);

            for(var i = 0; i < count; i++)
            {
                var name = _fileNames[i];

                result.Add(name, PrepareFile(modelContainer, _fileContents[name]));
            }

            return result;
        }

        private static string PrepareFile(ModelContainer modelContainer, string content)
        {
            var commonSettings = modelContainer.CommonSettings;

            CSharpFile.Start(commonSettings.IndentSize);

            var commonLayers = modelContainer.CommonLayers;

            var rootNamespace = commonSettings.RootNamespace;
            var commonRepoNamespace = rootNamespace + "." + commonLayers.Repository.Namespace;

            if(!content.StartsWith(Common.SharpFactoryFileMarker, StringComparison.Ordinal))//add the header only if it is not already present
            {
                var type = new ClassDeclaration
                {
                    IsPartial = true,
                    Name = "Database",
                    Namespace = commonRepoNamespace
                };

                CSharpFile.Header(type, Common.SharpFactoryFileMarker);
            }

            CSharpFile.LineNoEnding(content);

            CSharpFile.Replace("SF.Query", rootNamespace + "." + commonLayers.Query.Namespace);
            CSharpFile.Replace("SF.Repository.Common", commonRepoNamespace);

            var sqlClientNameSpace = Common.CalculateSqlClientNamespace(modelContainer);

            if(string.CompareOrdinal(sqlClientNameSpace, "System.Data.SqlClient") != 0)
                CSharpFile.Replace("System.Data.SqlClient", sqlClientNameSpace);

            CSharpFile.Footer();

            return CSharpFile.ToString();
        }
    }
}
