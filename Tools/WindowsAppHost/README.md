# Windows apphosts after a Linux cross-build

The .NET 6 Linux SDK reports NETSDK1074 and leaves Windows apphosts without
native version/icon/manifest resources and with the console subsystem. Do not
distribute those hosts as the finished GUI application.

Build this tool with the Windows .NET 6 SDK, then run its DLL using the Windows
.NET 6 runtime on Windows or under Wine. When building the tool on Linux, set
`-p:HostModelAssemblyPath=<Windows-SDK>/sdk/6.0.428/Microsoft.NET.HostModel.dll`;
the Linux SDK's own HostModel contains native Linux ReadyToRun code and cannot
run on Windows. Supply the **unmodified** apphost template
from `microsoft.netcore.app.host.win-x64/6.0.36/runtimes/win-x64/native/apphost.exe`
in the NuGet cache and the directory containing the built application DLLs:

```text
dotnet WindowsAppHost.dll <template-apphost.exe> <Program-directory>
```

This uses Microsoft's SDK HostWriter to bind each DLL, set the GUI subsystem
and copy its Win32 resources. All three hosts must pass the tool's subsystem,
resource and version checks before packaging. A normal build on Windows
already performs this step through the SDK.

The independent startup regression checks exercise the actual ApplicationInfo
source without native host metadata. From the repository root on Linux:

```sh
dotnet run --project Tests/Startup.Metadata -c Release -- "Open Rails startup check" "Startup metadata check"
dotnet run --project Tests/Startup.Metadata -c Release -p:EmptyMetadata=true -- "Open Rails" "Open Rails"
```

They check managed product/title fallback and the settings-directory path,
including the case where managed metadata is absent too.
