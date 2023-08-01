using Configuration.Models;
using Configuration.Models.Epg;
using Configuration.Models.Ftp;
using ConfigValidatorRoot;
using FtpEngineRoot;
using RepackerRoot;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Container = SimpleInjector.Container;

namespace BootstraperRoot;

public static class Bootstrapper
{
    // for utilizing objects' lifestyle scopes
    public static Container Container { get; } = new();

    static Bootstrapper()
    {
        Container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
    }

    public static void Bootstrap()
    {
        Container.Register<EpgConfig>();
        Container.Register<ChannelsConfig>();
        Container.Register<WebConfig>();
        Container.Register<FtpConfiguration>();
        Container.Register<TempDirConfig>();
        Container.Register<ArchivingConfig>();

        Container.Register<ConfigValidator>(Lifestyle.Scoped);
        Container.Register<FtpEngine>(Lifestyle.Scoped);

        Container.Register<FileDownloader>();
        Container.Register<Hasher>();
        Container.Register<XmlParser>();
        Container.Register<GzipPacker>();
        Container.Register<RepackerOptions>();
        Container.Register<Repacker>();

        Container.Verify();
    }

    public static T GetInstance<T>() where T : class
    {
        return Container.GetInstance<T>();
    }
}
