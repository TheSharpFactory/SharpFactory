#region Usings
using SF.Models;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.Engine;

/// <summary>
/// The rules the app applies before it builds or updates a model.
/// </summary>
public static class ModelValidation
{
    public const string ModelNameRequired = "Model name is required.";
    public const string ModelNameExists = "A model with this name already exists. Choose a different name.";
    public const string ModelNameIsRootNamespace = "The model name cannot be the same as the root namespace.";
    public const string ServerRequired = "Database server is required.";
    public const string DatabaseRequired = "Database name is required.";
    public const string DatabaseNameNotSupported = "The database name has characters that are not supported: use only letters, digits, '_' and spaces.";
    public const string UserRequired = "User is required.";
    public const string PasswordRequired = "Password is required.";
    public const string IndentSizeRequired = "Indent size is required.";
    public const string IndentSizeInvalid = "Indent size must be a whole number greater than 0.";

    /// <summary>
    /// Model name and connection rules. Returns one message per problem; empty when everything is valid.
    /// </summary>
    public static List<string> ValidateDbInfo(ModelContainer modelContainer, string modelName, ConnectionInfo conInfo, bool validateModelUnique)
    {
        var result = new List<string>();
        if(string.IsNullOrWhiteSpace(modelName))
            result.Add(ModelNameRequired);

        if(validateModelUnique && modelContainer != null && modelContainer.Models.Contains(modelName))
            result.Add(ModelNameExists);

        if(string.IsNullOrWhiteSpace(conInfo.Server))
            result.Add(ServerRequired);

        if(string.IsNullOrWhiteSpace(conInfo.Database))
            result.Add(DatabaseRequired);
        else if(!NameRules.IsSupportedObjectName(conInfo.Database))
            result.Add(DatabaseNameNotSupported);

        if(conInfo.DisableIntegratedSecurity.HasValue && conInfo.DisableIntegratedSecurity.Value)
        {
            if(string.IsNullOrWhiteSpace(conInfo.User))
                result.Add(UserRequired);

            if(string.IsNullOrWhiteSpace(conInfo.Password))
                result.Add(PasswordRequired);
        }

        return result;
    }

    /// <summary>
    /// The names that become folders or project files must be usable as path parts: not rooted, no "..", no path
    /// separator and no character a file name cannot have. The dependencies folder may contain '/' and '\\': each
    /// part is checked. Returns one line per problem; empty when every name can be used.
    /// </summary>
    public static List<string> ValidatePathNames(ModelContainer container)
    {
        var result = new List<string>();
        if(container == null || container.CommonSettings == null)
            return result;

        var settings = container.CommonSettings;
        CheckDependenciesFolder(result, settings.DependenciesFolder);
        AddLayerProjectNames(result, container.CommonLayers, settings);

        if(container.Models == null)
            return result;

        foreach(var model in container.Models)
        {
            if(model == null)
                continue;

            if(IsBadPathName(model.Name))
                result.Add($"model name '{model.Name}'");

            AddLayerProjectNames(result, model.Layers, settings);
        }

        return result;
    }

    private static void AddLayerProjectNames(List<string> result, object layers, CommonSettings settings)
    {
        if(layers == null)
            return;

        foreach(var property in layers.GetType().GetProperties())
        {
            if(property.PropertyType != typeof(LayerInfo))
                continue;

            var layer = (LayerInfo)property.GetValue(layers);
            if(layer == null)
                continue;

            var project = layer.FullProjectName(settings);
            if(IsBadPathName(project))
                result.Add($"project name '{project}'");
        }
    }

    private static void CheckDependenciesFolder(List<string> result, string folder)
    {
        //no dependencies folder: the projects go at the solution root
        if(string.IsNullOrWhiteSpace(folder))
            return;

        //Sync trims the folder before it uses it
        var trimmed = folder.Trim();
        if(Path.IsPathRooted(trimmed))
        {
            result.Add($"dependencies folder '{folder}'");
            return;
        }

        var parts = trimmed.Split(new[] { '/', '\\' });
        foreach(var part in parts)
        {
            //a trailing or doubled separator adds no folder
            if(part.Length == 0)
                continue;

            if(IsBadPathSegment(part))
            {
                result.Add($"dependencies folder '{folder}'");
                return;
            }
        }
    }

    private static bool IsBadPathName(string name)
    {
        if(string.IsNullOrWhiteSpace(name))
            return true;

        if(Path.IsPathRooted(name))
            return true;

        if(name.Contains(".."))
            return true;

        foreach(var c in name)
        {
            if(IsBadPathCharacter(c) || c == '/' || c == '\\')
                return true;
        }

        return false;
    }

    private static bool IsBadPathSegment(string segment)
    {
        if(string.IsNullOrEmpty(segment) || string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal))
            return true;

        if(Path.IsPathRooted(segment))
            return true;

        foreach(var c in segment)
        {
            if(IsBadPathCharacter(c))
                return true;
        }

        return false;
    }

    private static bool IsBadPathCharacter(char c)
    {
        if(c < 32)
            return true;

        if(c == '<' || c == '>' || c == ':' || c == '"' || c == '|' || c == '?' || c == '*')
            return true;

        return Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0;
    }

    /// <summary>
    /// A new model's name cannot be the same as the root namespace. Returns null when the name is allowed.
    /// </summary>
    public static string ValidateModelNameAgainstRootNamespace(ModelContainer modelContainer, string modelName)
    {
        if(modelContainer == null)
            return null;

        var rootNamespace = modelContainer.CommonSettings.RootNamespace;
        if(!string.IsNullOrWhiteSpace(rootNamespace) && string.CompareOrdinal(modelName, rootNamespace) == 0)
            return ModelNameIsRootNamespace;

        return null;
    }

    /// <summary>
    /// The fields needed to list the databases of a server. Returns null when they are complete.
    /// </summary>
    public static string ValidateServerInfo(ConnectionInfo conInfo)
    {
        if(string.IsNullOrWhiteSpace(conInfo.Server))
            return ServerRequired;

        if(conInfo.DisableIntegratedSecurity.HasValue && conInfo.DisableIntegratedSecurity.Value)
        {
            if(string.IsNullOrWhiteSpace(conInfo.User))
                return UserRequired;

            if(string.IsNullOrWhiteSpace(conInfo.Password))
                return PasswordRequired;
        }

        return null;
    }

    /// <summary>
    /// Indent size as typed: required and a whole number greater than zero. Returns null when valid.
    /// </summary>
    public static string ValidateIndentSize(string indentSize, out int indent)
    {
        indent = 0;
        if(string.IsNullOrWhiteSpace(indentSize))
            return IndentSizeRequired;

        if(!int.TryParse(indentSize, out indent) || indent < 1)
            return IndentSizeInvalid;

        return null;
    }

    /// <summary>
    /// Root namespace and dependencies folder are trimmed and spaces become underscores.
    /// </summary>
    public static string CleanName(string value)
    {
        if(value == null)
            return null;

        return value.Trim().Replace(" ", "_");
    }

    /// <summary>
    /// The text fields of a connection are trimmed before use.
    /// </summary>
    public static string Trim(string value)
    {
        if(value == null)
            return null;

        return value.Trim();
    }

    /// <summary>
    /// True when two connections point to the same server with the same credentials (the database is ignored).
    /// </summary>
    public static bool IsSameServerLogin(ConnectionInfo one, ConnectionInfo two)
    {
        if(one == null && two == null)
            return true;

        if(one == null || two == null)
            return false;

        if(string.CompareOrdinal(one.Server, two.Server) != 0)
            return false;

        if(one.DisableIntegratedSecurity != two.DisableIntegratedSecurity)
            return false;

        if(one.DisableIntegratedSecurity.HasValue && one.DisableIntegratedSecurity.Value)
        {
            if(string.CompareOrdinal(one.User, two.User) != 0)
                return false;

            if(string.CompareOrdinal(one.Password, two.Password) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Joins messages with new lines, for a single error text.
    /// </summary>
    public static string Join(List<string> messages)
    {
        return string.Join(Environment.NewLine, messages);
    }
}
