using System.IO;
using System.Threading.Tasks;
using Configuration.Models;
using FtpEngineRoot;
using Serilog;
using Shared;
using Throw;

namespace RepackerRoot;

public class Repacker
{
    private readonly FileDownloader _fileDownloader;
    private readonly GzipPacker _gzipPacker;
    private readonly XmlParser _xmlParser;
    private readonly Hasher _hasher;
    private readonly FtpEngine _ftpEngine;

    private readonly string _tempDir;
    private readonly bool _isDeleteTempDir;

    private string? _sourceFile;
    private string? _resultFile;
    private string? _channelsFile;

    private bool _isSourceDownloaded;
    private bool _isResultUploadNeeded;
    private bool _isResultPackingNeeded;

    // all this through DI, so such big number of params is acceptable, i guess
    public Repacker(
        TempDirConfig tempDirConfig,
        FileDownloader webScraper,
        XmlParser xmlParser,
        GzipPacker arhivator,
        Hasher hasher,
        FtpEngine ftpEngine)
    {
        tempDirConfig.Bind();
        _tempDir = tempDirConfig.Path!;
        _isDeleteTempDir = tempDirConfig.Delete;

        _fileDownloader = webScraper;
        _gzipPacker = arhivator;
        _xmlParser = xmlParser;

        _hasher = hasher;
        _ftpEngine = ftpEngine;
    }

    public void Repack(RepackerOptions options)
    {
        Init(options);

        if (IsAbortDueToMatchingHash(options.IgnoreHash))
        {
            return;
        }

        string parsedFile = Parse();
        string? packedFile = null;

        if (_isResultPackingNeeded)
        {
            packedFile = Pack(parsedFile);
        }

        if (_isResultUploadNeeded)
        {
            Upload(parsed: parsedFile, packed: packedFile);
        }

        TrySaveHash();
        TryCleanupTemp(parsed: parsedFile, packed: packedFile);
    }

    private void Init(RepackerOptions options)
    {
        EnsureValidOptions(options);

        _sourceFile = options.SourcePath;
        _resultFile = options.ResultPath;
        _channelsFile = options.ChannelsPath;

        (bool downloaded, string source) =
            TryDownloadSourceAsync(options.SourcePath!).Result;

        _sourceFile = source;
        _isSourceDownloaded = downloaded;

        _isResultPackingNeeded = IsGzipArchive(_resultFile!);
        _isResultUploadNeeded = options.FtpConfig != null;

        _hasher.Init(options.SourceHashPath!);
        _ftpEngine.Init(options.FtpConfig);
    }

    private bool IsAbortDueToMatchingHash(bool ignoreHash)
    {
        if (!ignoreHash && _hasher.IsHashMatchesSaved(_sourceFile!))
        {
            Log.Information("Processing of file '{0}' is canceled as it's hash is known.", _sourceFile);

            TryCleanupTemp(null, null);

            return true;
        }

        return false;
    }

    private static void EnsureValidOptions(RepackerOptions options)
    {
        options.SourcePath.ThrowIfNull();
        options.ResultPath.ThrowIfNull();
        options.ChannelsPath.ThrowIfNull();
    }

    private async Task<(bool downloaded, string source)> TryDownloadSourceAsync(
        string source)
    {
        bool downloaded = false;

        if (PathHelpers.IsHttpUrl(_sourceFile))
        {
            source = await _fileDownloader.DownloadAsync(_sourceFile!);

            Log.Information("Downloaded file from '{0}' and saved it to file '{1}'.", _sourceFile, source);
            downloaded = true;
        }

        return (downloaded, source);
    }

    private static bool IsGzipArchive(string filePath)
    {
        return Path.GetExtension(filePath) switch
        {
            ".gz" or ".gzip" => true,
            _ => false
        };
    }

    private string Parse()
    {
        // if uploading to FTP server is needed, then after upload temp file should be
        // deleted; if packing is needed, then it is done through temp file;
        // in other cases parsing is done directly to specified (in settings) result file
        string resultFile = _isResultUploadNeeded || _isResultPackingNeeded
            ? PathHelpers.PickRandomFilePath(_tempDir)
            : _resultFile!;

        Log.Information("Started to process file '{0}'.", _sourceFile);

        if (IsGzipArchive(_sourceFile!))
        {
            _xmlParser.ParseGzip(
                sourceFile: _sourceFile!,
                resultFile: resultFile,
                channelsFile: _channelsFile!);
        }
        else
        {
            _xmlParser.Parse(
                sourceFile: _sourceFile!,
                resultFile: resultFile,
                channelsFile: _channelsFile!);
        }

        Log.Information("Done processing file '{0}'.", _sourceFile);
        Log.Information("Results saved to file '{0}'.", resultFile);

        return resultFile;
    }

    private string Pack(string sourceFile)
    {
        // if uploading to FTP server is needed, then uploading file should have
        // same name as set in settings for result file AND should be temporary
        // as it will be deleted after upload; in other cases packing is done
        // to specified (in settings) result file
        string resultFile = _isResultUploadNeeded
            ? Path.Combine(_tempDir, Path.GetFileName(_resultFile!))
            : _resultFile!;

        _gzipPacker.Pack(sourceFile, resultFile);

        Log.Information("File '{0}' packed to file '{1}'.", sourceFile, resultFile);

        return resultFile;
    }

    private void Upload(string parsed, string? packed)
    {
        string upload = packed ?? parsed;

        if (_ftpEngine.TryUpload(upload, _resultFile!))
        {
            Log.Information("File '{0}' uploaded by FTP on destination '{1}'.", upload, _resultFile);
        }
        else
        {
            Log.Fatal("Processing failed as successful file upload was expected.");
            throw new IOException();
        }
    }

    private void TryCleanupTemp(string? parsed, string? packed)
    {
        if ((_isResultUploadNeeded || _isResultPackingNeeded) && parsed != null)
        {
            TryDeleteTempFile(parsed);
        }

        if (_isResultUploadNeeded && packed != null)
        {
            TryDeleteTempFile(packed);
        }

        if (_isSourceDownloaded)
        {
            TryDeleteTempFile(_sourceFile);
        }

        TryDeleteTempDir();
    }

    private static void TryDeleteTempFile(string? path)
    {
        if (File.Exists(path) && PathHelpers.TryDeleteFile(path))
        {
            Log.Debug("Deleted temp file '{0}'.", path);
        }
    }

    private void TryDeleteTempDir()
    {
        if (_isDeleteTempDir)
        {
            if (PathHelpers.TryDeleteDir(_tempDir))
            {
                Log.Debug("Deleted temp folder '{0}'.", _tempDir);
            }
        }
    }

    private bool TrySaveHash()
    {
        return _hasher.TryCalcSaveHash(_sourceFile!);
    }
}
