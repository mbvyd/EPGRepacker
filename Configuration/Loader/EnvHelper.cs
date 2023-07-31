using System;

namespace Configuration.Loader;

public static class EnvHelper
{
    public static Env GetEnvironment()
    {
        string env = "DOTNET_ENVIRONMENT";

        string? environmentName = Environment.GetEnvironmentVariable(env);

        environmentName ??= AppContext.GetData(env)?.ToString();

        ArgumentNullException.ThrowIfNull(environmentName);

        return (Env)Enum.Parse(typeof(Env), environmentName);
    }
}