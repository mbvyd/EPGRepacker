# EPG Repacker
Console app which takes EPG source file -- XML in [XMLTV](https://wiki.xmltv.org/index.php/XMLTVFormat) format (may be gzipped), extract data for set of channels and make new XMLTV file. 
Multiple EPG sources can be processed in one run.

# Build
One of variants:
```
dotnet build EPGRepacker.sln -p:MyRuntimeIdentifier=win-x64 -o:"path\to\build" --configuration:Release --no-self-contained
```

## Configuration
Rename `appsettings.Example.json` (under _Config_ folder) to `appsettings.json`.
For each EPG "entity" set:
- source path (can be local file or file residing on web URL);
- result path (can be local file or remote - on FTP server); 
- channels path - local plain text file with channels IDs (used in `id` attributes for `channel` tags in XML) - one id per line in file.

Set other options (see comments in `appsettings.Example.json`).

# 3rd Party dependencies
- [Serilog](https://github.com/serilog/serilog);
- [Throw](https://github.com/amantinband/throw);
- [SimpleInjector](https://github.com/simpleinjector/SimpleInjector);
- [FluentFTP](https://github.com/robinrodricks/FluentFTP);
