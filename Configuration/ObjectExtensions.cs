using System;
using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration;

internal static class ObjectExtensions
{
    public static bool TryBindConfigSection<T>(
        this object? instance, T enumItem) where T : Enum
    {
        string? sectionName = Enum.GetName(typeof(T), enumItem);

        bool bind = false;

        if (sectionName != null)
        {
            try
            {
                IConfigurationSection section = ConfigLoader.Load()
                    .GetSection(sectionName);

                section.Bind(instance);

                bind = true;
            }
            catch (Exception)
            {
            }
        }

        return bind;
    }
}
