# Modern renderer preview ZIP

After Release builds of Launcher, Menu and RunActivity, compile all production
effects using the repository's pinned mgfxc tool. Verify/recreate the three
Windows GUI apphosts as described in ../WindowsAppHost/README.md when building
outside Windows. Then run `python Tools/WindowsPackage/package-preview.py`.

The script collects declared Release outputs and runtime dependencies, checks
GUI subsystem/version resources and compiled effect signatures, includes the
preview INI/instructions and an empty log folder, verifies ZIP integrity and
writes Downloads/OpenRails-ModernRenderer-preview.zip plus its SHA-256 file.
It does not package locally installed routes/trains or modify the older v2 ZIP.
