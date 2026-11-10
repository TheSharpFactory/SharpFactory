#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SF.Models;
#endregion

namespace SF.Utils;

internal static class JsonUtils
{
    #region Private Fields
    private static readonly JsonSerializerSettings _settingsIndent = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new CustomJsonContractResolver(),
    };
    private static readonly JsonSerializerSettings _settingsNoIdent = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ContractResolver = new CustomJsonContractResolver(),
    };
    private static readonly JsonSerializerSettings _settingsForCompare = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };
    #endregion
    #region Properties
    //public static JsonSerializerSettings SettingsIndent
    //{
    //  get
    //  {
    //    return _settingsIndent;
    //  }
    //}
    //public static JsonSerializerSettings SettingsNoIdent
    //{
    //  get
    //  {
    //    return _settingsNoIdent;
    //  }
    //}
    //public static JsonSerializerSettings SettingsForCompare
    //{
    //  get
    //  {
    //    return _settingsNoIdent;
    //  }
    //}
    #endregion

    static JsonUtils()
    {
        _settingsIndent.Converters.Add(new CustomJsonConverter());
    }

    public static string Serialize<TModel>(TModel models)
    {//think about removing all indentation.  then figure out a way to display it indented ONLY when it is loaded on the JSON Editor in Visual Studio                
        return JsonConvert.SerializeObject(models, _settingsIndent);
    }
    public static TModel Deserialize<TModel>(string jsonContent, List<string> errors)
    {
        var errorCollector = new JsonErrorCollector(errors);
        return JsonConvert.DeserializeObject<TModel>(jsonContent, new JsonSerializerSettings
        {
            Error = errorCollector.OnError,
        });
    }
    public static TModel Deserialize<TModel>(string jsonContent)
    {
        return JsonConvert.DeserializeObject<TModel>(jsonContent);
    }
    public static bool AreDifferent(object one, object two)
    {
        return string.CompareOrdinal(SerializeForCompare(one), SerializeForCompare(two)) != 0;
    }
    public static bool AreEqual(object one, object two)
    {
        return string.CompareOrdinal(SerializeForCompare(one), SerializeForCompare(two)) == 0;
    }
    private static string SerializeForCompare(object obj)
    {
        return JsonConvert.SerializeObject(obj, _settingsForCompare);
    }
    #region CustomConverter
    private sealed class CustomJsonConverter : JsonConverter
    {
        private static readonly HashSet<int> _typesNoIndent = new HashSet<int>
        {
          typeof(Entity).MetadataToken,
          //typeof(View).MetadataToken,
          typeof(StoredProc).MetadataToken,
          typeof(Function).MetadataToken,
          typeof(Property).MetadataToken,
          typeof(Key).MetadataToken,
          typeof(KeyProperty).MetadataToken,
          typeof(NavProperty).MetadataToken,
          typeof(ConnectionInfo).MetadataToken,
          typeof(LayerInfo).MetadataToken,
          typeof(FKey).MetadataToken,
          typeof(Sequence).MetadataToken
        };
        public override bool CanConvert(Type objectType)
        {
            return _typesNoIndent.Contains(objectType.MetadataToken);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(JsonConvert.SerializeObject(value, _settingsNoIdent));
        }
    }
    #endregion
    #region CustomerResolver
    private sealed class CustomJsonContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var result = new List<JsonProperty>();
            foreach(var p in base.CreateProperties(type, memberSerialization))
            {
                if(!p.Writable)
                    continue;

                //its default is Yes (1): No (0) must be written too, or it reads back as Yes
                if(p.DeclaringType == typeof(CommonSettings) && string.Equals(p.PropertyName, nameof(CommonSettings.UseRootNSInProjectName), StringComparison.Ordinal))
                    p.DefaultValueHandling = DefaultValueHandling.Include;

                result.Add(p);
            }

            return result;
        }
    }
    #endregion
    #region Error collector
    private sealed class JsonErrorCollector
    {
        private readonly List<string> _errors;

        public JsonErrorCollector(List<string> errors)
        {
            _errors = errors;
        }

        public void OnError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            _errors.Add(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
        }
    }
    #endregion
}
