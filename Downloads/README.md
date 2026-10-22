# Testowa wersja Open Rails dla Windows x64

[Pobierz ZIP](https://github.com/pawel2137oioi/Niewiem/raw/refs/heads/codex/graphics-test-download/Downloads/OpenRails-Windows-test.zip).

Poprawiona paczka (v2): aplikacje EXE maja teraz typ Windows GUI oraz zasoby
wersji, ikony i manifestu. Naprawia to blad launchera `WaitForInputIdle` i brak
nazwy produktu prowadzacy do `Value cannot be null (Parameter 'path2')` podczas
tworzenia katalogu ustawien. Kod ma tez zapasowy odczyt nazwy z biblioteki .NET.
Rozpakuj te wersje do nowego katalogu, zeby nie mieszac jej ze starymi plikami.

1. Zainstaluj [.NET Desktop Runtime 6.0.36 dla Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
2. Rozpakuj caly ZIP do nowego katalogu i uruchom OpenRails.exe.
3. W glownym oknie kliknij Content > Manually Installed > Add i dodaj katalog poprawnie zainstalowanej trasy i taboru PKOR. Paczka ich nie zawiera.
4. W Options > Environment wybierz Balanced lub Apply 1080p / 60 FPS target, potem OK.
5. Wybierz trase, pociag i jazde, nastepnie Start.

Do porownania wydajnosci uzyj tej samej jazdy, kamery i pogody kolejno w Original, Balanced i High. Wylacz VSync i PerformanceTuner na czas pomiaru. Domyslnie Alt+F5 otwiera HUD, a Shift+Alt+F5 zmienia jego strone.

To wersja eksperymentalna: Release/win-x64 zbudowana z Linux, z efektami skompilowanymi natywnym kompilatorem Microsoft przez Wine. Sprawdzono kompilacje, komplet plikow oraz otwieranie menu i Options > Environment przez Wine z Windows Desktop Runtime 6.0.36. Rozgrywka na Windows i wydajnosc GPU nie sa zweryfikowane. Odbicia otoczenia w wodzie i chmury objetosciowe pozostaja niezaimplementowane.

Zrodla odpowiadajace tej paczce sa na tej samej galezi. Szczegoly w [raporcie zmian](../Docs/Graphics-Performance.md). Suma SHA-256 jest w pliku obok ZIP-a.
