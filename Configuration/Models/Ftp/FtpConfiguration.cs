using System.Collections.Generic;
using System.Linq;
using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration.Models.Ftp;

public class FtpConfiguration
{
    public IEnumerable<FtpItemConfig>? Items { get; set; }

    public void Bind()
    {
        Items = ConfigLoader.Load()
            .GetSection(nameof(ConfigSection.Ftp))
            .Get<IEnumerable<FtpItemConfig>>();
    }

    public FtpItemConfig? Get(string? name)
    {
        return Get(Items, name);
    }

    public static FtpItemConfig? Get(IEnumerable<FtpItemConfig>? items, string? name)
    {
        return !string.IsNullOrWhiteSpace(name)
            ? items?.Where(f => f.Name == name).FirstOrDefault()
            : null;
    }
}
