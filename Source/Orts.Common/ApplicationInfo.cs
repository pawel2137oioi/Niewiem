// COPYRIGHT 2009 - 2024 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ORTS.Common
{
    public static class ApplicationInfo
    {
        public static string ProcessFile => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule.FileName;
        public static string ProcessDirectory => Path.GetDirectoryName(ProcessFile);
        static FileVersionInfo VersionInfo => FileVersionInfo.GetVersionInfo(ProcessFile);
        // Apphosts built on a non-Windows system can lack native version resources.
        // Keep settings paths valid using the metadata in the managed entry assembly.
        public static string ProductName => NonEmpty(VersionInfo.ProductName,
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product,
            "Open Rails");
        public static string ApplicationName => NonEmpty(VersionInfo.FileDescription,
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title,
            ProductName);

        static string NonEmpty(string nativeValue, string managedValue, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(nativeValue)) return nativeValue;
            if (!string.IsNullOrWhiteSpace(managedValue)) return managedValue;
            return fallback;
        }
    }
}
