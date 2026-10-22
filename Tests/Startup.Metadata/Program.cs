using System;
using System.Diagnostics;
using System.IO;
using ORTS.Common;

// Run on Linux, where the native host has no version resources. This reproduces the metadata missing from the
// original cross-built Windows download without needing a graphics device.
if (!string.IsNullOrEmpty(FileVersionInfo.GetVersionInfo(ApplicationInfo.ProcessFile).ProductName))
    throw new Exception("Run using a native host without product version resources.");
if (ApplicationInfo.ProductName != args[0])
    throw new Exception("Wrong product: " + ApplicationInfo.ProductName);
if (ApplicationInfo.ApplicationName != args[1])
    throw new Exception("Wrong application name: " + ApplicationInfo.ApplicationName);
var settingsPath = Path.Combine(Path.GetTempPath(), ApplicationInfo.ProductName);
if (Path.GetFileName(settingsPath) != args[0])
    throw new Exception("Cannot form the user settings directory.");
Console.WriteLine("PASS: missing native metadata, product/title fallback and settings path.");
