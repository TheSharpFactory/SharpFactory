#region Usings
using SF.Common.Logging;
using SF.Engine;
using SF.Engine.Settings;
using SF.Engine.Sync;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace SF.Cli;

/// <summary>
/// The Sharp Factory from the command line.
///   sharpfactory sync &lt;model.edmj&gt; &lt;solution.sln|.slnx&gt; [--preview]
///   sharpfactory generate &lt;model.edmj&gt; &lt;output folder&gt;
///   sharpfactory update &lt;model.edmj&gt; [--model &lt;name&gt;] [--check]
///   sharpfactory info &lt;model.edmj&gt;
///   sharpfactory --version (or version)
/// Exit code: 0 success, 1 failure (or, for update --check, a model that would change), 2 usage error (an unknown
/// command or argument, a missing model file).
/// Progress goes to standard output; errors and the usage go to standard error.
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        LogManager.Enable(AppPaths.LogsFolder);
        IOUtils.LogFolder = AppPaths.LogsFolder;

        try
        {
            return Run(args);
        }
        catch(Exception ex)
        {
            //for example a model file that is read-only when update saves it
            LogManager.WriteErrorLog(ex, "The command failed", string.Join(" ", args));
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if(args.Length < 1)
            return Usage();

        switch(args[0].ToLowerInvariant())
        {
            case "sync":
                return Sync(args);
            case "generate":
                return Generate(args);
            case "update":
                return Update(args);
            case "info":
                return Info(args);
            case "help":
            case "--help":
            case "-h":
                WriteUsage(Console.Out);
                return 0;
            case "version":
            case "--version":
                Console.Out.WriteLine($"sharpfactory {Version()}");
                return 0;
            default:
                return Usage();
        }
    }

    private static int Sync(string[] args)
    {
        if(args.Length < 3)
            return Usage();

        var unknown = FindUnknownArgument(args, 2, "--preview", null);
        if(unknown != null)
            return UnknownArgument(unknown);

        var modelFile = args[1];
        var solutionPath = args[2];
        var preview = HasFlag(args, "--preview");

        if(!SolutionFile.IsSupported(solutionPath))
        {
            Console.Error.WriteLine($"Not a .sln or .slnx file: {solutionPath}");
            return 2;
        }

        var modelContainer = LoadModel(modelFile);
        if(modelContainer == null)
            return LoadFailedCode(modelFile);

        var blocked = ModelFileCheck.SyncBlockText(modelContainer);
        if(blocked != null)
        {
            Console.Error.WriteLine(blocked);
            return 1;
        }

        var report = SolutionSync.Run(new SyncRequest { ModelContainer = modelContainer, SolutionPath = solutionPath, Preview = preview }, new ConsoleProgressLog());
        if(!report.Success)
            return 1;

        PrintSummary(report);

        if(!preview)
        {
            var settings = AppSettingsStore.Load();
            AppSettingsStore.RecordSyncTarget(settings, modelFile, solutionPath);
            AppSettingsStore.Save(settings);
        }

        return 0;
    }

    private static int Generate(string[] args)
    {
        if(args.Length < 3)
            return Usage();

        var unknown = FindUnknownArgument(args, 2, null, null);
        if(unknown != null)
            return UnknownArgument(unknown);

        var modelContainer = LoadModel(args[1]);
        if(modelContainer == null)
            return LoadFailedCode(args[1]);

        var unsupported = ModelNameCheck.Find(modelContainer);
        if(unsupported.Count > 0)
        {
            Console.Error.WriteLine("Nothing was written. These objects are not supported: " + string.Join("; ", unsupported) + ". Update the model to remove them.");
            return 1;
        }

        var badNames = ModelValidation.ValidatePathNames(modelContainer);
        if(badNames.Count > 0)
        {
            Console.Error.WriteLine("Nothing was written. These names cannot be used in file paths: " + string.Join("; ", badNames));
            return 1;
        }

        var outputFolder = Path.GetFullPath(args[2]);
        var host = new FolderProjectHost(outputFolder);
        try
        {
            GenerationRunner.Run(modelContainer, host, new ConsoleProgressLog());
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to generate", outputFolder);
            Console.Error.WriteLine($"ERROR: generation stopped after {host.FilesWritten} files: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"Generated {host.FilesWritten} files into {outputFolder}");
        return 0;
    }

    /// <summary>
    /// Reads each model (or the one named with --model) again from its database and saves the file. With --check
    /// nothing is saved: the exit code is 1 when a model would change, for a build that wants to know.
    /// </summary>
    private static int Update(string[] args)
    {
        if(args.Length < 2)
            return Usage();

        var unknown = FindUnknownArgument(args, 1, "--check", "--model");
        if(unknown != null)
            return UnknownArgument(unknown);

        var modelFile = args[1];
        var check = HasFlag(args, "--check");
        var onlyModel = FlagValue(args, "--model");
        var modelContainer = LoadModel(modelFile);
        if(modelContainer == null)
            return LoadFailedCode(modelFile);

        var models = new List<Models.Model>();
        foreach(var model in modelContainer.Models)
        {
            if(onlyModel == null || string.Equals(model.Name, onlyModel, StringComparison.OrdinalIgnoreCase))
                models.Add(model);
        }

        if(models.Count == 0)
        {
            Console.Error.WriteLine(onlyModel == null ? $"The file has no models: {modelFile}" : $"No model named {onlyModel} in {modelFile}");
            return 2;
        }

        var log = new ConsoleProgressLog();
        var changed = 0;
        foreach(var model in models)
        {
            var errorsBefore = log.ErrorCount;
            Console.WriteLine($"Updating {model.Name} from {model.ConInfo.Server} / {model.ConInfo.Database}");
            var result = (ModelUpdateResult)null;
            try
            {
                result = ModelUpdater.Update(modelContainer, model, log);
            }
            catch(Exception ex)
            {
                LogManager.WriteErrorLog(ex, "Unable to update the model", model.Name);
                Console.Error.WriteLine($"ERROR: unable to read {model.Name} from {model.ConInfo.Server} / {model.ConInfo.Database}: {ex.Message}");
                return 1;
            }

            Console.WriteLine($"   {result.ModelName}: {Describe(result.Model)}{(result.IsChanged ? "" : " (no changes)")}");

            //an object the database could not describe is left out of the model: it is not gone from the database
            var readFailed = log.ErrorCount > errorsBefore;
            foreach(var removed in result.Removed)
                Console.WriteLine(readFailed ? $"   left out: {removed}" : $"   no longer in the database: {removed}");

            foreach(var record in result.NotSupported)
                Console.WriteLine("   not supported: " + record);

            if(result.IsChanged)
                changed++;
        }

        if(log.ErrorCount > 0)
        {
            Console.Error.WriteLine("ERROR: some objects could not be read, so the model file was not saved. The details are in the error log.");
            return 1;
        }

        if(check)
        {
            Console.WriteLine(changed == 0 ? "The model file is up to date." : $"{changed} of {models.Count} models would change (nothing was saved).");
            return changed == 0 ? 0 : 1;
        }

        if(changed > 0)
        {
            ModelUtils.Save(modelContainer, modelFile);
            Console.WriteLine($"Saved {modelFile}");
        }
        else
        {
            Console.WriteLine("The model file is up to date.");
        }

        return 0;
    }

    /// <summary>
    /// The file's common settings and, for each model, its connection and object counts.
    /// </summary>
    private static int Info(string[] args)
    {
        if(args.Length < 2)
            return Usage();

        var unknown = FindUnknownArgument(args, 1, null, null);
        if(unknown != null)
            return UnknownArgument(unknown);

        var modelContainer = LoadModel(args[1]);
        if(modelContainer == null)
            return LoadFailedCode(args[1]);

        var settings = modelContainer.CommonSettings;
        Console.WriteLine(Path.GetFullPath(args[1]));
        Console.WriteLine($"   root namespace: {settings?.RootNamespace}, dependencies folder: {settings?.DependenciesFolder}, indent: {settings?.IndentSize}");
        Console.WriteLine($"   {modelContainer.Models.Count} {(modelContainer.Models.Count == 1 ? "model" : "models")}");
        foreach(var model in modelContainer.Models)
        {
            var conInfo = model.ConInfo;
            var signIn = conInfo?.DisableIntegratedSecurity == true ? $"SQL login {conInfo.User}" : "Windows authentication";
            Console.WriteLine();
            Console.WriteLine($"   {model.Name}");
            Console.WriteLine($"      {conInfo?.Server} / {conInfo?.Database} ({signIn})");
            Console.WriteLine($"      {Describe(model)}");
        }

        return 0;
    }

    /// <summary>
    /// "14 tables, 2 views, 5 procedures, 4 functions, 2 sequences".
    /// </summary>
    private static string Describe(Models.Model model)
    {
        return $"{Count(model, Models.CategoryNames.Entities, "table")}, {Count(model, Models.CategoryNames.Views, "view")}, " +
            $"{Count(model, Models.CategoryNames.Procs, "procedure")}, {Count(model, Models.CategoryNames.Funcs, "function")}, {Count(model, Models.CategoryNames.Seqs, "sequence")}";
    }

    private static string Count(Models.Model model, string category, string word)
    {
        var found = model.Categories?.GetByName(category);
        var count = found == null ? 0 : found.Count;
        return count == 1 ? $"1 {word}" : $"{count} {word}s";
    }

    /// <summary>
    /// The first argument after the command's <paramref name="positional"/> ones that it does not take; null when
    /// there is none. A misspelled --preview or --check must not run the command for real.
    /// </summary>
    /// <param name="flag">The option the command takes on its own, or null.</param>
    /// <param name="valueOption">The option the command takes with a value after it, or null.</param>
    private static string FindUnknownArgument(string[] args, int positional, string flag, string valueOption)
    {
        for(var i = 1 + positional; i < args.Length; i++)
        {
            if(flag != null && string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                continue;

            if(valueOption != null && string.Equals(args[i], valueOption, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            return args[i];
        }

        return null;
    }

    private static int UnknownArgument(string argument)
    {
        Console.Error.WriteLine($"Unexpected argument: {argument}");
        return Usage();
    }

    private static bool HasFlag(string[] args, string flag)
    {
        foreach(var arg in args)
        {
            if(string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string FlagValue(string[] args, string flag)
    {
        for(var i = 0; i < args.Length - 1; i++)
        {
            if(string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static Models.ModelContainer LoadModel(string modelFile)
    {
        if(!File.Exists(modelFile))
        {
            Console.Error.WriteLine($"Model file not found: {modelFile}");
            return null;
        }

        var modelContainer = ModelFileCheck.Load(modelFile, out var problem);
        if(modelContainer == null)
            Console.Error.WriteLine($"Unable to load the model {modelFile}: {problem}");

        return modelContainer;
    }

    /// <summary>
    /// A model file that is not there is a wrong argument (2); one that is there but cannot be read is a failure (1).
    /// </summary>
    private static int LoadFailedCode(string modelFile)
    {
        return File.Exists(modelFile) ? 1 : 2;
    }

    private static void PrintSummary(SyncReport report)
    {
        Console.WriteLine();
        Console.WriteLine(report.IsPreview ? "Preview (nothing was written):" : "Done:");
        Console.WriteLine($"   solution: {report.SolutionPath}{(report.SolutionCreated ? " (new)" : "")}");
        Console.WriteLine($"   projects created: {report.ProjectsCreated.Count}, existing added: {report.ExistingProjectsAdded.Count}, moved: {report.ProjectsMoved.Count}, references added: {report.ReferencesAdded.Count}, updated: {report.ReferencesUpdated.Count}");
        Console.WriteLine($"   files added: {report.CountFiles(SyncFileChangeKind.Added)}, changed: {report.CountFiles(SyncFileChangeKind.Changed)}, deleted: {report.CountFiles(SyncFileChangeKind.Deleted)}, unchanged: {report.UnchangedFileCount}");

        foreach(var moved in report.ProjectsMoved)
            Console.WriteLine($"   moved: {moved}");

        foreach(var unmarked in report.UnmarkedFiles)
            Console.WriteLine($"   not a Sharp Factory file (Sync will stop): {unmarked}");
    }

    /// <summary>
    /// A usage error: the usage on standard error, exit code 2.
    /// </summary>
    private static int Usage()
    {
        WriteUsage(Console.Error);
        return 2;
    }

    /// <summary>
    /// The version the build stamps into the assembly from the VERSION file, e.g. 1.1.9.2.
    /// </summary>
    private static string Version()
    {
        return typeof(Program).Assembly.GetName().Version.ToString();
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine($"The Sharp Factory {Version()}");
        writer.WriteLine("  sharpfactory sync <model.edmj> <solution.sln|.slnx> [--preview]");
        writer.WriteLine("      Generates the model's code into the solution (created when missing). --preview writes nothing.");
        writer.WriteLine("  sharpfactory generate <model.edmj> <output folder>");
        writer.WriteLine("      Generates every file into a folder, one subfolder per project, without project files.");
        writer.WriteLine("  sharpfactory update <model.edmj> [--model <name>] [--check]");
        writer.WriteLine("      Reads the models' objects again from their databases and saves the file. Objects the database");
        writer.WriteLine("      no longer has drop out. --check saves nothing and exits with 1 when a model would change.");
        writer.WriteLine("  sharpfactory info <model.edmj>");
        writer.WriteLine("      Shows the file's settings and each model's connection and object counts.");
        writer.WriteLine("  sharpfactory --version");
        writer.WriteLine("      Shows the version.");
    }

    /// <summary>
    /// Prints progress messages.
    /// </summary>
    private sealed class ConsoleProgressLog : IProgressLog
    {
        /// <summary>
        /// The errors logged so far, for example objects the database could not describe.
        /// </summary>
        public int ErrorCount { get; private set; }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message, string userMessage)
        {
            ErrorCount++;
            Console.Error.WriteLine($"ERROR: {message}");
            if(!string.IsNullOrWhiteSpace(userMessage))
                Console.Error.WriteLine($"       {userMessage}");
        }
    }
}
