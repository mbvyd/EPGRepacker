using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration.Models;

public class ChannelsConfig : IBindable
{
    public bool TrimStart { get; set; }
    public bool TrimEnd { get; set; }
    public bool IgnoreCase { get; set; }

    public void Bind()
    {
        IConfigurationSection section = ConfigLoader.Load()
            .GetSection(nameof(ConfigSection.Channels));

        section.Bind(this);
    }
}
