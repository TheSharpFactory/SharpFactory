#region Usings
using SF.Models;
using System;
using System.Collections.Generic;
#endregion

namespace SF.Engine.Sync;

/// <summary>
/// One project reference Sync makes sure exists.
/// </summary>
public sealed class ProjectReferencePair
{
    public string ProjectName { get; set; }
    public string ReferencedProjectName { get; set; }
}

/// <summary>
/// Which generated project references which, for one model (same list and order as the Visual Studio Sync had).
/// </summary>
public static class ProjectReferenceRules
{
    public static List<ProjectReferencePair> GetReferences(ModelContainer modelContainer, Model model)
    {
        var result = new List<ProjectReferencePair>(30);
        var settings = modelContainer.CommonSettings;
        var common = modelContainer.CommonLayers;
        var layers = model.Layers;

        //Common Repository
        Add(result, common.Repository.FullProjectName(settings), common.Query.FullProjectName(settings));
        Add(result, common.Repository.FullProjectName(settings), layers.PropEnum.FullProjectName(settings));

        //RepoContainer
        Add(result, common.RepoContainer.FullProjectName(settings), common.IRepoContainer.FullProjectName(settings));
        Add(result, common.RepoContainer.FullProjectName(settings), common.Repository.FullProjectName(settings));
        Add(result, common.RepoContainer.FullProjectName(settings), layers.IRepository.FullProjectName(settings));
        Add(result, common.RepoContainer.FullProjectName(settings), layers.Repository.FullProjectName(settings));

        //IRepoContainer
        Add(result, common.IRepoContainer.FullProjectName(settings), layers.IRepository.FullProjectName(settings));

        //Common Layer
        Add(result, common.Query.FullProjectName(settings), layers.PropEnum.FullProjectName(settings));
        Add(result, common.Query.FullProjectName(settings), layers.NavPropEnum.FullProjectName(settings));
        Add(result, common.Query.FullProjectName(settings), layers.Entity.FullProjectName(settings));

        //entity utils
        Add(result, layers.EntityUtil.FullProjectName(settings), layers.Entity.FullProjectName(settings));
        Add(result, layers.EntityUtil.FullProjectName(settings), layers.PropEnum.FullProjectName(settings));
        Add(result, layers.EntityUtil.FullProjectName(settings), layers.NavPropEnum.FullProjectName(settings));
        Add(result, layers.EntityUtil.FullProjectName(settings), common.Query.FullProjectName(settings));

        //repository interfaces
        Add(result, layers.IRepository.FullProjectName(settings), layers.Entity.FullProjectName(settings));
        Add(result, layers.IRepository.FullProjectName(settings), common.Query.FullProjectName(settings));
        Add(result, layers.IRepository.FullProjectName(settings), layers.PropEnum.FullProjectName(settings));
        Add(result, layers.IRepository.FullProjectName(settings), layers.NavPropEnum.FullProjectName(settings));

        //repositories
        Add(result, layers.Repository.FullProjectName(settings), layers.Entity.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), common.Query.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), layers.IRepository.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), layers.EntityUtil.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), layers.PropEnum.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), layers.NavPropEnum.FullProjectName(settings));
        Add(result, layers.Repository.FullProjectName(settings), common.Repository.FullProjectName(settings));

        return result;
    }

    private static void Add(List<ProjectReferencePair> result, string projectName, string referencedProjectName)
    {
        //a project never references itself (layers can share a project name)
        if(string.Equals(projectName, referencedProjectName, StringComparison.OrdinalIgnoreCase))
            return;

        result.Add(new ProjectReferencePair { ProjectName = projectName, ReferencedProjectName = referencedProjectName });
    }
}
