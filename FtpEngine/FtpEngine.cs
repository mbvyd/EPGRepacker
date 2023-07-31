using System;
using System.Collections.Generic;
using System.Linq;
using Configuration.Models.Ftp;
using FluentFTP;
using FluentFTP.Helpers;
using Serilog;
using Shared.Logger;

namespace FtpEngineRoot;

public class FtpEngine : IDisposable
{
    // if file permissions on FTP server start on this numbers, it means it is writable
    private readonly IEnumerable<int> _writeChmodStartNumbers =
        new List<int> { 2, 3, 6, 7 };

    private string? _server;
    private int _port;
    private string? _user;
    private string? _password;

    private FtpClient? _ftpClient;

    private bool _disposed;

    public void Init(FtpItemConfig? ftp)
    {
        _server = ftp?.Server;
        _user = ftp?.User;
        _password = ftp?.Password;
        _port = ftp != null ? ftp.Port : 0;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No unmanaged resources, no sense to call from finalizer")]
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ftpClient != null)
            {
                _ftpClient.Dispose();
                _ftpClient = null;
            }

            _disposed = true;
        }
    }

    public bool Exists(string remoteFile, bool disconnect = true)
    {
        EstablichConnection();

        bool exists = false;

        try
        {
            exists = _ftpClient!.FileExists(remoteFile);
        }
        catch (Exception ex)
        {
            Log.Information("Failed to determine existance of file '{0}' on FTP server.", remoteFile);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        DisconnectIfRequested(disconnect);

        return exists;
    }

    public bool IsWritable(string remoteFile, bool disconnect = true)
    {
        EstablichConnection();

        bool writable = false;

        try
        {
            FtpListItem permissions = _ftpClient!.GetFilePermissions(remoteFile);

            int firstNumber = GetFirstInteger(permissions.Chmod);

            if (_writeChmodStartNumbers.Any(e => e == firstNumber))
            {
                writable = true;
            }
        }
        catch (Exception ex)
        {
            Log.Information("Faile to get FTP server's filesystem permissions for '{0}'.", remoteFile);
            LogHelpers.LogMessage(ex, LogKind.Information);
        }

        DisconnectIfRequested(disconnect);

        return writable;
    }

    public bool TryDownload(string localFile, string remoteFile, bool disconnect = true)
    {
        EstablichConnection();

        bool success = true;

        try
        {
            FtpStatus status = _ftpClient!.DownloadFile(localFile, remoteFile);

            if (status.IsFailure())
            {
                ProcessError(exception: null);
            }
        }
        catch (Exception ex)
        {
            ProcessError(ex);
        }

        DisconnectIfRequested(disconnect);

        return success;

        void ProcessError(Exception? exception)
        {
            Log.Information("Failed to download file '{0}' by FTP.", remoteFile);
            LogHelpers.LogMessageIfException(exception, LogKind.Information);
            success = false;
        }
    }

    public bool TryUpload(string localFile, string remoteFile, bool disconnect = true)
    {
        EstablichConnection();

        bool success = true;

        try
        {
            FtpStatus status = _ftpClient!.UploadFile(localFile, remoteFile);

            if (status.IsFailure())
            {
                ProcessError(exception: null);
            }
        }
        catch (Exception ex)
        {
            ProcessError(ex);
        }

        DisconnectIfRequested(disconnect);

        return success;

        void ProcessError(Exception? exception)
        {
            Log.Information("Failed to upload by FTP file '{0}'.", localFile);
            LogHelpers.LogMessageIfException(exception, LogKind.Information);
            success = false;
        }
    }

    public bool TryDelete(string remoteFile, bool disconnect = true)
    {
        EstablichConnection();

        bool deleted = false;

        try
        {
            _ftpClient!.DeleteFile(remoteFile);
            deleted = true;
        }
        catch (Exception ex)
        {
            Log.Information("Failed to delete file '{0}' by FTP.", remoteFile);
            LogHelpers.LogMessageIfException(ex, LogKind.Information);
        }

        DisconnectIfRequested(disconnect);

        return deleted;
    }

    private void EstablichConnection()
    {
        if (_ftpClient == null)
        {
            _ftpClient = new FtpClient(_server, _user, _password, _port);

            try
            {
                _ftpClient.Connect();
            }
            catch (Exception ex)
            {
                Log.Fatal("FTP client failed to connect.", ex.Message);
                LogHelpers.LogMessage(ex, LogKind.Fatal);
                throw;
            }
        }
    }

    private void DisconnectIfRequested(bool disconnect)
    {
        if (disconnect)
        {
            _ftpClient?.Dispose();
            _ftpClient = null;
        }
    }

    private static int GetFirstInteger(int sourceInteger)
    {
        // Since the character represents a digit, we subtract the character '0'
        // (ASCII value 48) to get the actual integer value of the digit
        return sourceInteger.ToString()[0] - '0';
    }
}
