using System;
using System.IO;
using System.Text.RegularExpressions;
using Serilog;
using Shared.Logger;

namespace Shared;

public static class PathHelpers
{
    // language=regex
    private const string _urlPattern =
        @"^(https?)://([\w-]+\.)+[\w-]+(/[\w-./?%&=]*)?$";

    public static bool IsHttpUrl(string? path)
    {
        return path != null && Regex.IsMatch(path, _urlPattern);
    }

    public static string PickRandomFilePath(string dirPath, string? extension = null)
    {
        string path;

        do
        {
            path = extension == null
                ? Path.Combine(dirPath, Path.GetRandomFileName())
                : Path.Combine(dirPath, Path.GetRandomFileName()) + extension;
        }
        while (File.Exists(path) || Directory.Exists(path));

        return path;
    }

    public static bool IsFileReadableAndNotEmpty(string path)
    {
        bool readable = false;

        if (File.Exists(path))
        {
            try
            {
                using FileStream fs = File.Open(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read);

                // does not help if file is filled with whitespaces, but anyway
                if (fs.Length > 0)
                {
                    readable = true;
                }
                else
                {
                    Log.Information("File '{0}' is empty.", path);
                }
            }
            catch (Exception ex)
            {
                Log.Information("Failed to read file '{0}'.", path);
                LogHelpers.LogMessage(ex, LogKind.Information);
            }
        }
        else
        {
            Log.Information("File '{0}' does not exist.", path);
        }

        return readable;
    }

    public static bool IsFileWritable(string path)
    {
        bool writable = false;
        bool alreadyExists = File.Exists(path);

        try
        {
            using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);

            writable = true;
        }
        catch (Exception ex)
        {
            Log.Information("Failed to open/create for writing file '{0}'.", path);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }
        finally
        {
            // file did not exist, but now it does - was created during this check
            if (!alreadyExists && File.Exists(path))
            {
                DeleteFile(path);
            }
        }

        return writable;

        static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Information("Failed to delete file '{0}' after it was created for write permissions' check.", path);
                LogHelpers.LogMessage(ex, LogKind.Information);
            }
        }
    }

    public static bool TryCreateDir(string path)
    {
        bool created = false;

        try
        {
            Directory.CreateDirectory(path);

            created = true;
        }
        catch (Exception ex)
        {
            Log.Information("Failed to create folder '{0}'.", path);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        return created;
    }

    public static bool TryDeleteDir(string path, bool recursive = false)
    {
        bool deleted = false;

        try
        {
            Directory.Delete(path, recursive: recursive);

            deleted = true;
        }
        catch (Exception ex)
        {
            Log.Information("Failed to delete folder '{0}'.", path);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        return deleted;
    }

    public static bool TryDeleteFile(string path)
    {
        bool deleted = false;

        try
        {
            File.Delete(path);

            deleted = true;
        }
        catch (Exception ex)
        {
            Log.Information("Failed to delete file '{0}'.", path);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        return deleted;
    }
}
