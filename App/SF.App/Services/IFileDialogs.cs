#region Usings
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace SF.App.Services;

/// <summary>
/// The file and folder pickers the app needs. Tests replace it with fixed answers.
/// Each method returns a local path, or null when the user cancels.
/// </summary>
internal interface IFileDialogs
{
    Task<string> OpenModelFileAsync();
    Task<string> SaveModelFileAsync(string suggestedFileName);
    Task<string> OpenSolutionFileAsync();
    Task<string> PickFolderAsync(string title);
}

/// <summary>
/// The OS file pickers, through the window's storage provider (Windows, macOS and Linux).
/// </summary>
internal sealed class StorageFileDialogs : IFileDialogs
{
    private readonly TopLevel _topLevel;

    public StorageFileDialogs(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<string> OpenModelFileAsync()
    {
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a model",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ModelFileType() },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string> SaveModelFileAsync(string suggestedFileName)
    {
        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the model",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "edmj",
            ShowOverwritePrompt = true,
            FileTypeChoices = new List<FilePickerFileType> { ModelFileType() },
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string> OpenSolutionFileAsync()
    {
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a solution",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Solutions (*.slnx, *.sln)") { Patterns = new List<string> { "*.slnx", "*.sln" } },
            },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string> PickFolderAsync(string title)
    {
        var folders = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private static FilePickerFileType ModelFileType()
    {
        return new FilePickerFileType("Entity Data Model JSON (*.edmj)") { Patterns = new List<string> { "*.edmj" } };
    }
}
