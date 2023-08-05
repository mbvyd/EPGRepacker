namespace Configuration.Models;

public class ChannelsConfig : IBindable
{
    public bool TrimStart { get; set; }
    public bool TrimEnd { get; set; }
    public bool IgnoreCase { get; set; }

    public void Bind()
    {
        this.TryBindConfigSection(ConfigSection.Channels);
    }
}
