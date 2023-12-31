﻿namespace Configuration.Models;

public class TempDirConfig : IBindable
{
    public string? Path { get; set; }
    public bool Delete { get; set; }

    private const string _tempFolderName = "EPG_Repacker";

    public void Bind()
    {
        // prevent binding from config if instance has been already initialized
        // (e.g., while reading whole config)
        if (Path == null)
        {
            this.TryBindConfigSection(ConfigSection.TempDir);
        }

        if (string.IsNullOrWhiteSpace(Path))
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), _tempFolderName);
        }
    }
}
