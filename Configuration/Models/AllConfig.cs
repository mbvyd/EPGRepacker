using System.Collections.Generic;
using Configuration.Models.Epg;
using Configuration.Models.Ftp;

namespace Configuration.Models;

// for config validation before actual run
public class AllConfig
{
    public IEnumerable<EpgItemConfig>? Epg { get; set; }
    public ChannelsConfig? Channels { get; set; }
    public WebConfig? Web { get; set; }
    public IEnumerable<FtpItemConfig>? Ftp { get; set; }
    public TempDirConfig? TempDir { get; set; }
    public ArchivingConfig? Archiving { get; set; }
}
