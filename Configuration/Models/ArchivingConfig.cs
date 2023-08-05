using System;
using System.IO.Compression;

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
        ArchivingConfigRaw archivingRaw = new();

        archivingRaw.TryBindConfigSection(ConfigSection.Archiving);

        GzipCompression = string.IsNullOrEmpty(archivingRaw.GzipCompression)
            ? CompressionLevel.Optimal
            : Enum.TryParse(
                archivingRaw.GzipCompression, out CompressionLevel compression)
                    ? compression : CompressionLevel.Optimal;
    }
}
