using System.Collections.Generic;
using Configuration.Loader;
using Microsoft.Extensions.Configuration;

namespace Configuration.Models;

public class WebConfig : IBindable
{
    public string? UserAgent { get; set; }
    public IEnumerable<string>? Mime { get; set; }

    public void Bind()
    {
        IConfigurationSection section = ConfigLoader.Load()
            .GetSection(nameof(ConfigSection.Web));

        section.Bind(this);
    }
}
