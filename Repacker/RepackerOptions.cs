using Configuration.Models.Ftp;

namespace RepackerRoot;

public class RepackerOptions
{
    public string? ChannelsPath { get; set; }
    public string? SourcePath { get; set; }
    public string? SourceHashPath { get; set; }
    public string? ResultPath { get; set; }
    public FtpItemConfig? FtpConfig { get; set; }
    public bool IgnoreHash { get; set; }
}
