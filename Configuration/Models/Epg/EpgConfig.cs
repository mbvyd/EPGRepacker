using System.Collections.Generic;
using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration.Models.Epg;

public class EpgConfig : IBindable
{
    public IEnumerable<EpgItemConfig>? Items { get; set; }

    public void Bind()
    {
        Items = ConfigLoader.Load()
            .GetSection(nameof(ConfigSection.Epg))
            .Get<IEnumerable<EpgItemConfig>>();
    }
}
