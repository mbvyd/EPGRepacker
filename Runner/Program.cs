using System;
using BootstraperRoot;
using Configuration.Loader;
using Configuration.Models.Epg;
using Configuration.Models.Ftp;
using ConfigValidatorRoot;
using RepackerRoot;
using Serilog;
using Shared.Logger;
using SimpleInjector.Lifestyles;

namespace Runner;

internal class Program
{
    private static void Main()
    {
        Bootstrapper.Bootstrap();

        SetLogger();

        using (AsyncScopedLifestyle.BeginScope(Bootstrapper.Container))
        {
            EnsureConfigValid();
        }

        using (AsyncScopedLifestyle.BeginScope(Bootstrapper.Container))
        {
            RepackEpg();
        }

        ReleaseLogger();
    }

    private static void RepackEpg()
    {
        EpgConfig epgConfig = Bootstrapper.GetInstance<EpgConfig>();
        epgConfig.Bind();

        FtpConfiguration ftpConfig = Bootstrapper.GetInstance<FtpConfiguration>();
        ftpConfig.Bind();

        Repacker repacker = Bootstrapper.GetInstance<Repacker>();

        foreach (EpgItemConfig epgItem in epgConfig.Items!)
        {
            RepackerOptions options = Bootstrapper.GetInstance<RepackerOptions>();

            options.ChannelsPath = epgItem.Channels;
            options.SourcePath = epgItem.Source?.Path;
            options.SourceHashPath = epgItem.Source?.HashPath;
            options.ResultPath = epgItem.Result?.Path;
            options.FtpConfig = ftpConfig.Get(epgItem.Result?.Ftp);

            repacker.Repack(options);
        }
    }

    private static void EnsureConfigValid()
    {
        ConfigValidator validator = Bootstrapper.GetInstance<ConfigValidator>();

        try
        {
            validator.EnsureValid();
            Log.Debug("Config checked and valid.");
        }
        catch (Exception ex)
        {
            Log.Fatal("Config is invalid, see next message for service details.", ex.Message);
            LogHelpers.LogMessage(ex, LogKind.Fatal);
            ReleaseLogger();
            throw;
        }
    }

    private static void SetLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(ConfigLoader.Load())
            .CreateLogger();
    }

    private static void ReleaseLogger()
    {
        Log.CloseAndFlush();
    }
}
