using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.NET.HostModel;
using Microsoft.NET.HostModel.AppHost;

// Run on Windows (or its .NET runtime under Wine) after cross-building.
// The Linux SDK cannot copy Win32 version/icon/manifest resources to apphosts.
if (args.Length != 2)
    throw new ArgumentException("Usage: WindowsAppHost <unmodified win-x64 apphost.exe> <Program directory>");
foreach (var name in new[] { "OpenRails", "Menu", "RunActivity" })
{
    var assembly = Path.Combine(args[1], name + ".dll");
    var executable = Path.Combine(args[1], name + ".exe");
    HostWriter.CreateAppHost(args[0], executable, name + ".dll",
        windowsGraphicalUserInterface: true);
    // Use the real resource update and validate its result. Wine's response to
    // the SDK's empty-filename capability probe can incorrectly say unsupported.
    using (var resources = new ResourceUpdater(executable))
        resources.AddResourcesFromPEImage(assembly).Update();
    using (var stream = File.OpenRead(executable))
    using (var pe = new PEReader(stream))
    {
        if (pe.PEHeaders.PEHeader.Subsystem != Subsystem.WindowsGui)
            throw new InvalidDataException(name + " is not a Windows GUI apphost.");
        if (pe.PEHeaders.PEHeader.ResourceTableDirectory.Size == 0)
            throw new InvalidDataException(name + " has no native resources.");
    }
    var version = FileVersionInfo.GetVersionInfo(executable);
    var sourceVersion = FileVersionInfo.GetVersionInfo(assembly);
    if (version.ProductName != "Open Rails" ||
        string.IsNullOrWhiteSpace(version.FileDescription) ||
        string.IsNullOrWhiteSpace(version.FileVersion) ||
        version.FileVersion != sourceVersion.FileVersion)
        throw new InvalidDataException(name + " has missing or incorrect version metadata.");
    Console.WriteLine($"Verified GUI apphost: {name}, {version.ProductName}, {version.FileVersion}");
}
