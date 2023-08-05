using System.Collections.Generic;

namespace Configuration.Models;

public class WebConfig : IBindable
{
    public string? UserAgent { get; set; }
    public IEnumerable<string>? Mime { get; set; }

    public void Bind()
    {
        this.TryBindConfigSection(ConfigSection.Web);
    }
}
