#region Usings
using SF.Engine;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#endregion

namespace SF.GenCheck;

/// <summary>
/// Regression check for the code generator, without Visual Studio.
/// Usage: SF.GenCheck &lt;model.edmj&gt; &lt;output folder&gt; [reference folder]
/// Generates every file for the model into the output folder, laid out like the projects Sync creates
/// (output/&lt;ProjectName&gt;/...). With a reference folder (for example the DataAccess folder of a solution
/// generated earlier) every generated file is compared byte for byte; the exit code is 1 if any differs.
/// It uses the same generation order as Sync (SF.Engine.GenerationRunner).
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        if(args.Length < 2)
        {
            Console.WriteLine("Usage: SF.GenCheck <model.edmj> <output folder> [reference folder]");
            return 2;
        }

        var edmjFile = args[0];
        var outputFolder = Path.GetFullPath(args[1]);
        var referenceFolder = args.Length > 2 ? Path.GetFullPath(args[2]) : null;

        var modelContainer = ModelUtils.Load(edmjFile);
        if(modelContainer == null)
        {
            Console.WriteLine($"Unable to load the model: {edmjFile}");
            return 2;
        }

        if(Directory.Exists(outputFolder) && Directory.EnumerateFileSystemEntries(outputFolder).Any())
        {
            Console.WriteLine($"The output folder must be empty or not exist: {outputFolder}");
            return 2;
        }

        var filesWritten = Generate(modelContainer, outputFolder);
        Console.WriteLine($"Generated {filesWritten} files into {outputFolder}");

        if(referenceFolder == null)
            return 0;

        return Compare(outputFolder, referenceFolder);
    }

    private static int Generate(ModelContainer modelContainer, string outputFolder)
    {
        var host = new FolderProjectHost(outputFolder);
        GenerationRunner.Run(modelContainer, host, NullProgressLog.Instance);
        return host.FilesWritten;
    }

    private static int Compare(string outputFolder, string referenceFolder)
    {
        var identical = 0;
        var different = new List<string>();
        var lineEndingsOnly = new List<string>();
        var missing = new List<string>();

        var files = new List<string>(Directory.EnumerateFiles(outputFolder, "*.cs", SearchOption.AllDirectories));
        files.Sort(StringComparer.OrdinalIgnoreCase);
        foreach(var file in files)
        {
            var relativePath = Path.GetRelativePath(outputFolder, file);
            var referenceFile = Path.Combine(referenceFolder, relativePath);

            if(!File.Exists(referenceFile))
            {
                missing.Add(relativePath);
                continue;
            }

            var generated = File.ReadAllBytes(file);
            var reference = File.ReadAllBytes(referenceFile);

            if(generated.AsSpan().SequenceEqual(reference))
                identical++;
            else if(string.Equals(Normalize(generated), Normalize(reference), StringComparison.Ordinal))
                lineEndingsOnly.Add(relativePath);
            else
                different.Add(relativePath);
        }

        Console.WriteLine($"Compared with {referenceFolder}: {identical} identical, {different.Count} different, {lineEndingsOnly.Count} differ only in line endings or BOM, {missing.Count} not in the reference");

        Report("different", different);
        Report("line endings/BOM", lineEndingsOnly);
        Report("not in reference", missing);

        return different.Count == 0 && lineEndingsOnly.Count == 0 && missing.Count == 0 ? 0 : 1;
    }

    private static string Normalize(byte[] content)
    {
        var text = System.Text.Encoding.UTF8.GetString(content);
        if(text.Length > 0 && text[0] == '﻿')
            text = text.Substring(1);

        return text.Replace("\r\n", "\n");
    }

    private static void Report(string label, List<string> files)
    {
        const int maxListed = 20;

        foreach(var file in files.Take(maxListed))
            Console.WriteLine($"   {label}: {file}");

        if(files.Count > maxListed)
            Console.WriteLine($"   {label}: ... and {files.Count - maxListed} more");
    }
}
