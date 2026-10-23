from pathlib import Path
import hashlib
import json
import struct
import zipfile

repo = Path(__file__).resolve().parents[2]
out = repo / "Downloads"
out.mkdir(exist_ok=True)
program = repo / "Program"

readme = (repo / "Downloads/ModernRenderer-URUCHOMIENIE.txt").read_text()

# Collect declared Release outputs from the main projects and their dependencies.
# This excludes unrelated Debug/xUnit output left in the shared Program folder.
files = set()
manifests = list((repo / "Source").glob("**/obj/Release/*.FileListAbsolute.txt"))
assert len(manifests) >= 3
for manifest in manifests:
    for raw in manifest.read_text().splitlines():
        p = Path(raw)
        try:
            p.relative_to(program)
        except ValueError:
            continue
        if p.is_file() and p.suffix.lower() not in (".pdb", ".xml"):
            files.add(p)

# MSBuild can omit unchanged, shared copy-local files from an individual
# project's incremental file list. Include actual runtime assets from deps.json.
for name in ("OpenRails", "Menu", "RunActivity"):
    deps = json.loads((program / (name + ".deps.json")).read_text())
    target = deps["targets"][deps["runtimeTarget"]["name"]]
    for library in target.values():
        for asset in (*library.get("runtime", {}), *library.get("native", {})):
            candidate = program / asset
            if not candidate.is_file():
                candidate = program / Path(asset).name
            assert candidate.is_file(), (name, asset)
            files.add(candidate)
for p in (program / "Content").rglob("*"):
    if p.is_file():
        files.add(p)
files.discard(program / "Updater.ini")

required = [
    "OpenRails.exe", "OpenRails.dll", "OpenRails.runtimeconfig.json",
    "Menu.exe", "Menu.dll", "Menu.deps.json", "Menu.runtimeconfig.json",
    "RunActivity.exe", "RunActivity.dll", "RunActivity.deps.json",
    "RunActivity.runtimeconfig.json", "GNU.Gettext.dll",
    "GNU.Gettext.WinForms.dll", "Native/X86/OpenAL32.dll",
    "Native/X64/OpenAL32.dll", "MonoGame.Framework.dll",
]
for name in required:
    assert program / name in files, name
for name in ("OpenRails", "Menu", "RunActivity"):
    data = (program / (name + ".exe")).read_bytes()
    assert data[:2] == b"MZ" and (name + ".dll\0").encode() in data, name
    pe = struct.unpack_from("<I", data, 0x3c)[0]
    optional = pe + 24
    assert struct.unpack_from("<H", data, optional + 68)[0] == 2, (name, "not Windows GUI")
    assert struct.unpack_from("<II", data, optional + 112 + 16)[1] > 0, (name, "no resources")
    assert "Open Rails".encode("utf-16-le") in data, (name, "no product metadata")
    runtime = json.loads((program / (name + ".runtimeconfig.json")).read_text())
    assert any(x["name"] == "Microsoft.WindowsDesktop.App"
               for x in runtime["runtimeOptions"]["frameworks"])
for source in (repo / "Source/RunActivity/Content").glob("*.fx"):
    binary = program / "Content" / (source.stem + ".mgfx")
    assert binary in files and binary.read_bytes()[:4] == b"MGFX", binary

with zipfile.ZipFile(out / "OpenRails-ModernRenderer-preview.zip", "w",
                     compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for p in sorted(files):
        z.write(p, "OpenRails-ModernRenderer/" + p.relative_to(program).as_posix())
    z.writestr("OpenRails-ModernRenderer/URUCHOMIENIE.txt",
               readme.replace("\n", "\r\n").encode("utf-8-sig"))
    z.writestr("OpenRails-ModernRenderer/OpenRails.ini", (repo / "Downloads/ModernRenderer.ini").read_text().replace("\n", "\r\n"))
    z.writestr("OpenRails-ModernRenderer/Logs/README.txt", "Log z jazdy: OpenRailsLog.txt w tym katalogu.\r\n")
    z.write(repo / "Docs/Graphics-Quality.md", "OpenRails-ModernRenderer/USTAWIENIA-GRAFIKI.md")
    z.write(repo / "LICENSE", "OpenRails-ModernRenderer/LICENSE")


p = out / "OpenRails-ModernRenderer-preview.zip"
with zipfile.ZipFile(p) as z:
    assert z.testzip() is None
    for name in ("OpenRails", "Menu", "RunActivity"):
        assert z.read("OpenRails-ModernRenderer/" + name + ".exe") == (program / (name + ".exe")).read_bytes()
digest = hashlib.sha256(p.read_bytes()).hexdigest()
(out / (p.name + ".sha256")).write_text(digest + "  " + p.name + "\n")
print(p.name, p.stat().st_size, "bytes; ZIP integrity, GUI hosts, resources, runtime dependencies and compiled shaders verified.")
