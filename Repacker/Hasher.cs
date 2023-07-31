using System;
using System.IO;
using System.Security.Cryptography;
using Serilog;
using Shared.Logger;

namespace RepackerRoot;

public class Hasher
{
    private string? _hashPath;
    private bool _isValidStorage;

    public void Init(string? saveHashPath)
    {
        _hashPath = saveHashPath;
        _isValidStorage = !string.IsNullOrWhiteSpace(_hashPath);
    }

    public bool TryCalcSaveHash(string filePath)
    {
        return _isValidStorage &&
            TryCalculateHash(filePath, out string? hash) &&
            TrySaveHash(hash!);
    }

    public bool IsHashMatchesSaved(string filePath)
    {
        return _isValidStorage &&
            TryGetSavedHash(out string? savedHash) &&
            TryCalculateHash(filePath, out string? newHash) &&
            string.Equals(savedHash, newHash);
    }

    private static bool TryCalculateHash(string filePath, out string? hash)
    {
        bool done = false;
        hash = null;

        try
        {
            using var sha256 = SHA256.Create();

            using var fileStream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] hashBytes = sha256.ComputeHash(fileStream);

            hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            done = true;
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to calculate hash for file '{0}'.", filePath);
            LogHelpers.LogMessage(ex, LogKind.Warning);
        }

        return done;
    }

    private bool TrySaveHash(string hash)
    {
        bool saved = false;

        try
        {
            File.WriteAllText(_hashPath!, hash);
            saved = true;
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to write hash to file '{0}'.", _hashPath);
            LogHelpers.LogMessage(ex, LogKind.Warning);
        }

        return saved;
    }

    private bool TryGetSavedHash(out string? hash)
    {
        bool retrieved = false;
        hash = null;

        // if file does not exist, then it is first run, and no need log warning
        if (!File.Exists(_hashPath))
        {
            return retrieved;
        }

        try
        {
            hash = File.ReadAllText(_hashPath);

            retrieved = true;
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to read hash from file '{0}'.", _hashPath);
            LogHelpers.LogMessage(ex, LogKind.Warning);
        }

        return retrieved;
    }
}
