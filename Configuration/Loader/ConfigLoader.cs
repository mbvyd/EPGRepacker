using Microsoft.Extensions.Configuration;

namespace Configuration.Loader;

// https://github.com/ericpopivker/entech-blog-share-configs-demo
public static class ConfigLoader
{
    public static IConfiguration Load(Env? env = null)
    {
        ConfigurationBuilder configBuilder = new();

        AddJsonFiles(configBuilder, env);

        return configBuilder.Build();
    }

    private static void AddJsonFiles(
        IConfigurationBuilder configurationBuilder, Env? env = null)
    {
        if (!env.HasValue)
        {
            env = EnvHelper.GetEnvironment();
        }

        configurationBuilder
            .AddJsonFile(
                $"Config/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"Config/appsettings.{env}.json", optional: true, reloadOnChange: true);
    }
}
