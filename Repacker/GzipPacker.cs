using System;
using System.IO;
using System.IO.Compression;
using Configuration.Models;
using Serilog;
using Shared.Logger;

namespace RepackerRoot;

public class GzipPacker
{
    private readonly CompressionLevel _compressionLevel;

    public GzipPacker(ArchivingConfig archivingConfig)
    {
        archivingConfig.Bind();
        _compressionLevel = archivingConfig.GzipCompression;
    }

    public void Pack(string sourceFile, string resultFile)
    {
        try
        {
            using FileStream sourceFileStream = File.OpenRead(sourceFile);

            using FileStream resultFileStream = File.Open(
                resultFile, FileMode.OpenOrCreate, FileAccess.Write);

            using GZipStream compressionStream = new(
                resultFileStream, _compressionLevel);

            sourceFileStream.CopyTo(compressionStream);
        }
        catch (Exception ex)
        {
            Log.Fatal("Failed to compress file '{0}' to file '{1}'.", sourceFile, resultFile);
            LogHelpers.LogMessage(ex, LogKind.Fatal);
            throw;
        }
    }
}
