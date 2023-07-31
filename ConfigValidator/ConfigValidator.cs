using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Configuration.Loader;
using Configuration.Models;
using Configuration.Models.Epg;
using Configuration.Models.Ftp;
using FtpEngineRoot;
using Microsoft.Extensions.Configuration;
using Serilog;
using Shared;
using Throw;

namespace ConfigValidatorRoot;

public class ConfigValidator : IDisposable, IValidatable
{
    private readonly AllConfig? _config;
    private readonly FtpEngine _ftpEngine;

    private bool _disposed;

    public ConfigValidator(FtpEngine ftpEngine)
    {
        try
        {
            _config = ConfigLoader.Load().Get<AllConfig>();
        }
        catch (Exception)
        {
            throw;
        }

        _ftpEngine = ftpEngine;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No unmanaged resources, no need to call from finalizer")]
    public void Dispose()
    {
        if (!_disposed)
        {
            _ftpEngine.Dispose();

            _disposed = true;
        }
    }

    public void EnsureValid()
    {
        _config.ThrowIfNull();

        EnsureTempDirValid();
        EnsureFtpCoreValid();
        EnsureEpgCoreValid();
        EnsureEpgValid();
    }

    private void EnsureTempDirValid()
    {
        _config!.TempDir.ThrowIfNull();
        _config.TempDir.Bind();

        bool alreadyExists = Directory.Exists(_config!.TempDir!.Path);

        if (_config.TempDir.Delete && alreadyExists)
        {
            PathHelpers.TryDeleteDir(_config.TempDir.Path!, recursive: true)
                .Throw().IfFalse();

            CreateTempDir();
        }

        if (!alreadyExists)
        {
            CreateTempDir();
        }

        string path = PathHelpers.PickRandomFilePath(_config.TempDir.Path!);
        PathHelpers.IsFileWritable(path).Throw().IfFalse();
        PathHelpers.TryDeleteFile(path).Throw().IfFalse();

        void CreateTempDir()
        {
            PathHelpers.TryCreateDir(_config!.TempDir!.Path!).Throw().IfFalse();
            Log.Debug("Created temp folder '{0}'.", _config.TempDir.Path);
        }
    }

    #region FTP

    private void EnsureFtpCoreValid()
    {
        if (FtpHasItems(_config?.Ftp))
        {
            FtpCollectionHasDuplicateNames(_config?.Ftp!).Throw().IfTrue();
        }
    }

    private static bool FtpHasItems(IEnumerable<FtpItemConfig>? items)
    {
        return items != null && items.Any();
    }

    private static bool FtpCollectionHasDuplicateNames(
        IEnumerable<FtpItemConfig> ftpItems)
    {
        return ftpItems
            .Select(f => f.Name)
            .GroupBy(n => n)
            .Any(g => g.Count() > 1);
    }

    private void EnsureFtpItemValid(FtpItemConfig? ftpItem, EpgItemConfig epgItem)
    {
        MakeBaseChecks(ftpItem);

        _ftpEngine.Init(ftpItem);

        if (_ftpEngine.Exists(epgItem.Result?.Path!, disconnect: false))
        {
            // not touching remote file, just checking permissions (to be able to overwrite it later)
            _ftpEngine.IsWritable(epgItem.Result?.Path!).Throw().IfFalse();
        }
        else
        {
            // blank empty file for upload check
            string blankFile = PathHelpers.PickRandomFilePath(_config!.TempDir!.Path!);
            File.WriteAllText(blankFile, string.Empty);

            _ftpEngine.TryUpload(blankFile, epgItem.Result?.Path!, disconnect: false)
                .Throw().IfFalse();

            _ftpEngine.TryDelete(epgItem.Result?.Path!).Throw().IfFalse();

            File.Delete(blankFile);
        }

        static void MakeBaseChecks(FtpItemConfig? ftpItem)
        {
            string.IsNullOrWhiteSpace(ftpItem?.Server).Throw().IfTrue();
            string.IsNullOrWhiteSpace(ftpItem?.User).Throw().IfTrue();
            string.IsNullOrWhiteSpace(ftpItem?.Password).Throw().IfTrue();
        }
    }

    #endregion

    #region EPG

    private void EnsureEpgCoreValid()
    {
        _config!.Epg.ThrowIfNull();
        _config.Epg.Throw().IfEmpty();

        EpgContainsInvalidFtp(_config.Epg, _config.Ftp).Throw().IfTrue();
    }

    private static bool EpgContainsInvalidFtp(
        IEnumerable<EpgItemConfig> epgItems, IEnumerable<FtpItemConfig>? ftpItems)
    {
        // checking if some EPG entity has FTP name assigned, which does not exist in FTPs list
        return FtpHasItems(ftpItems) && epgItems
            .Where(e =>
                // FTP name can be empty to allow saving to local path and
                // we need to avoid flagging it as invalid
                !string.IsNullOrWhiteSpace(e.Result?.Ftp) &&
                !ftpItems!.Any(f => f.Name == e.Result?.Ftp))
            .Any();
    }

    private void EnsureEpgValid()
    {
        foreach (EpgItemConfig item in _config?.Epg!)
        {
            EnsureEpgItemValid(item);
        }
    }

    private void EnsureEpgItemValid(EpgItemConfig epgItem)
    {
        MakeBaseChecks(epgItem);

        MakeChecksBeforeFtp(epgItem, _config?.Web?.UserAgent, _config?.Web?.Mime);

        if (!string.IsNullOrWhiteSpace(epgItem.Result?.Ftp))
        {
            CheckFtp();

            IsUploadingPathWritable(_config?.TempDir?.Path!, epgItem.Result?.Path!)
                .Throw().IfFalse();
        }
        else
        {
            PathHelpers.IsFileWritable(epgItem.Result?.Path!).Throw().IfFalse();
        }

        static void MakeBaseChecks(EpgItemConfig item)
        {
            item.Source.ThrowIfNull();
            item.Result.ThrowIfNull();

            string.IsNullOrWhiteSpace(item.Channels).Throw().IfTrue();
            string.IsNullOrWhiteSpace(item.Source.Path).Throw().IfTrue();
            string.IsNullOrWhiteSpace(item.Result.Path).Throw().IfTrue();
        }

        static void MakeChecksBeforeFtp(
            EpgItemConfig epgItem, string? userAgent, IEnumerable<string>? mime)
        {
            PathHelpers.IsFileReadableAndNotEmpty(epgItem.Channels!)
                .Throw().IfFalse();

            IsValidFileOrUrl(epgItem.Source?.Path!, userAgent, mime).Throw().IfFalse();

            IsUnsupportedArchive(epgItem.Source?.Path!).Throw().IfTrue();
            IsUnsupportedArchive(epgItem.Result?.Path!).Throw().IfTrue();

            IsHashPathValid(epgItem.Source?.HashPath).Throw().IfFalse();
        }

        void CheckFtp()
        {
            FtpItemConfig? ftpConfig =
                FtpConfiguration.Get(_config?.Ftp, epgItem.Result.Ftp);

            EnsureFtpItemValid(ftpConfig, epgItem);
        }
    }

    private static bool IsValidFileOrUrl(
        string fileOrUrl, string? userAgent, IEnumerable<string>? mime = null)
    {
        return PathHelpers.IsHttpUrl(fileOrUrl)
            ? WebHelpers.IsUrlValidAsync(fileOrUrl, userAgent, mime).Result
            : PathHelpers.IsFileReadableAndNotEmpty(fileOrUrl);
    }

    private static bool IsUnsupportedArchive(string filePath)
    {
        return Path.GetExtension(filePath) switch
        {
            ".zip" => true,
            ".7z" => true,
            ".rar" => true,
            ".tar" => true,
            ".bz2" => true,
            ".bz" => true,
            ".tgz" => true,
            ".tbz2" => true,
            ".tbz" => true,
            _ => false
        };
    }

    private static bool IsHashPathValid(string? path)
    {
        // if hash path is set, it must be writable; blank hash path is forgiving -
        // in such case no hash calculations will follow
        return string.IsNullOrWhiteSpace(path) || PathHelpers.IsFileWritable(path);
    }

    // before uploading to FTP server a temp file will be created/overwritten
    // with same name as on server, so we need to make sure it is also writable
    private static bool IsUploadingPathWritable(string tempDir, string remotePath)
    {
        string resultFile = Path.Combine(tempDir, Path.GetFileName(remotePath));

        return PathHelpers.IsFileWritable(resultFile);
    }

    #endregion
}
