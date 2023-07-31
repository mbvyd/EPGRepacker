namespace Configuration.Models.Epg;

public class EpgItemConfig
{
    public string? Channels { get; set; }
    public EpgSourceConfig? Source { get; set; }
    public EpgResultConfig? Result { get; set; }
}
