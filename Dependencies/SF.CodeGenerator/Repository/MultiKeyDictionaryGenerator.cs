#region Usings
using System.Collections.Generic;
using SF.Models;
#endregion

namespace SF.CodeGeneration;

public static partial class RepoGenerator
{
    public static string GenerateMultiKeyDictionary(ModelContainer modelContainer)
    {
        return MultiKeyDictionary.Generate(modelContainer);
    }
    private static class MultiKeyDictionary
    {
        public static string Generate(ModelContainer modelContainer)
        {
            CSharpFile.Start(modelContainer.CommonSettings.IndentSize);
            var typeName = Common.Names.MultiKeyDictionary;
            var typeNamespace = modelContainer.CommonLayers.Repository.FullNamespace(modelContainer.CommonSettings);

            var usingNamespaces = new List<UsingNamespace>(1)
            {
              new UsingNamespace { Name = "System.Collections.Generic" },
            };

            CSharpFile.Header(ArtifactKind.Class, Common.SharpFactoryFileMarker);
            #region 1Key
            //var typeDeclaration = $"public class {typeName}<TKey1, TValue>:";
            var summary = "This dictionary supports 1 key";

            var type = new ClassDeclaration
            {
                Name = typeName,
                Namespace = typeNamespace,
                BaseClass = "Dictionary<TKey1, TValue>",
                GenericParams = new GenericParameterList
                {
                  new GenericParameter { Name = "TKey1" },
                  new GenericParameter { Name = "TValue" },
                },
                Summary = new List<string> { summary }
            };

            //Generate file header with information about the interface.       
            CSharpFile.UsingStatements(usingNamespaces);
            CSharpFile.OpenClassWithNamespace(type);

            var constructor = new ConstructorDeclaration
            {
                Name = type.Name
            };

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter> { new MethodParameter { Name = "capacity", ParamType = "int" } };
            constructor.BaseConstructor = "base(capacity)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter> { new MethodParameter { Name = "comparer", ParamType = "IEqualityComparer<TKey1>" } };
            constructor.BaseConstructor = "base(comparer)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "capacity", ParamType = "int" },
              new MethodParameter { Name = "comparer", ParamType = "IEqualityComparer<TKey1>" }
            };
            constructor.BaseConstructor = "base(capacity, comparer)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            var method = new MethodDeclaration
            {
                Name = "Add",
                InheritanceModifier = InheritanceModifiers.New,
                Parameters = new List<MethodParameter>
                {
                  new MethodParameter { Name = "k1", ParamType = "TKey1" },
                  new MethodParameter { Name = "val", ParamType = "TValue" },
                }
            };

            CSharpFile.DeclareMethod(method);
            CSharpFile.OpenBlock();
            CSharpFile.If("!ContainsKey(k1)");
            CSharpFile.LineIndented("base.Add(k1, val)");
            CSharpFile.CloseBlock();
            CSharpFile.CloseType();
            #endregion
            GenerateMultiKeyDictionaries(16, typeName, typeNamespace);
            CSharpFile.CloseNamespace();
            CSharpFile.Footer();
            return CSharpFile.ToString();
        }
        private static void GenerateMultiKeyDictionaries(int keyCount, string typeName, string typeNamespace)
        {
            for(var i = 2; i <= keyCount; i++)
                GenerateMultiKeyDictionary(i, typeName, typeNamespace);
        }
        private static void GenerateMultiKeyDictionary(int keyCount, string typeName, string typeNamespace)
        {
            var summary = $"This dictionary supports {keyCount} keys";
            var num = 1.ToString();
            var genericTypes = "TKey" + num;
            var paramsWithType = "TKey" + num + " k" + num;
            for(var i = 1; i < keyCount; i++)
            {
                num = (i + 1).ToString();
                genericTypes += ", TKey" + num;
                paramsWithType += ", TKey" + num + " k" + num;
            }

            var num2 = 2.ToString();
            var genericTypes2 = "TKey" + num2;//these start at two
            var paramsNoType = "k" + num2;//these start at two
            for(var i = 2; i < keyCount; i++)
            {
                num2 = (i + 1).ToString();
                genericTypes2 += ", TKey" + num2;
                paramsNoType += ", k" + num2;
            }

            //public void Add(TKey1 k1, TKey2 k2, TKey3 k3, TKey4 k4, TValue val)
            //{
            //  if(ContainsKey(k1))
            //    this[k1].Add(k2, k3, k4, val);
            //  else
            //    Add(k1, new MultiKeyDictionary<TKey2, TKey3, TKey4, TValue>() { { k2, k3, k4, val } });
            //}
            //public bool ContainsKey(TKey1 k1, TKey2 k2, TKey3 k3, TKey4 k4)
            //{
            //  return ContainsKey(k1) && this[k1].ContainsKey(k2, k3, k4);
            //}

            //var typeDeclaration = $"public class {typeName}<{genericTypes}, TValue>:";

            var type = new ClassDeclaration
            {
                Name = $"{typeName}<{genericTypes}, TValue>",
                Namespace = typeNamespace,
                BaseClass = $"Dictionary<TKey1, {typeName}<{genericTypes2}, TValue>>",
                Summary = new List<string> { summary }
            };

            CSharpFile.OpenClass(type);

            var constructor = new ConstructorDeclaration
            {
                Name = typeName
            };

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter> { new MethodParameter { Name = "capacity", ParamType = "int" } };
            constructor.BaseConstructor = "base(capacity)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter> { new MethodParameter { Name = "comparer", ParamType = "IEqualityComparer<TKey1>" } };
            constructor.BaseConstructor = "base(comparer)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            constructor.Parameters = new List<MethodParameter>
            {
              new MethodParameter { Name = "capacity", ParamType = "int" },
              new MethodParameter { Name = "comparer", ParamType = "IEqualityComparer<TKey1>" }
            };
            constructor.BaseConstructor = "base(capacity, comparer)";

            CSharpFile.Constructor(constructor);
            CSharpFile.OpenBlock();
            CSharpFile.CloseBlock();

            CSharpFile.LineNoEnding($"public void Add({paramsWithType}, TValue val)");
            CSharpFile.OpenBlock();
            CSharpFile.If("ContainsKey(k1)");
            CSharpFile.LineIndented($"this[k1].Add({paramsNoType}, val)");
            CSharpFile.Else();
            CSharpFile.Line($"Add(k1, new {typeName}<{genericTypes2}, TValue>() {{ {{ {paramsNoType}, val }} }})");
            CSharpFile.CloseBlock();

            CSharpFile.LineNoEnding($"public bool ContainsKey({paramsWithType})");
            CSharpFile.OpenBlock();
            CSharpFile.Return($"ContainsKey(k1) && this[k1].ContainsKey({paramsNoType})");
            CSharpFile.CloseBlock();
            CSharpFile.CloseType();
        }
    }
}
