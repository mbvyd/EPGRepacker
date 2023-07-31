namespace Configuration.Models.Ftp;

public class FtpItemConfig
{
    public string? Name { get; set; }
    public string? Server { get; set; }
    public int Port { get; set; }
    public string? User { get; set; }
    // TODO: password is expected in plain text, need to rework this flaw
    public string? Password { get; set; }
}
