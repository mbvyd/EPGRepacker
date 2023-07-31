using System;
using System.IO.Compression;
using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration.Models;

public class ArchivingConfig : IBindable
{
    private class ArchivingConfigRaw
    {
        public string? GzipCompression { get; set; }
    }

    public CompressionLevel GzipCompression { get; set; }

    public void Bind()
    {
        IConfigurationSection section = ConfigLoader.Load()
            .GetSection(nameof(ConfigSection.Archiving));

        ArchivingConfigRaw archivingRaw = new();

        section.Bind(archivingRaw);

        GzipCompression = string.IsNullOrEmpty(archivingRaw.GzipCompression)
            ? CompressionLevel.Optimal
            : Enum.TryParse(
                archivingRaw.GzipCompression, out CompressionLevel compression)
                    ? compression : CompressionLevel.Optimal;
    }
}
