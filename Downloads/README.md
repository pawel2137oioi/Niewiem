# Testowa wersja Open Rails dla Windows x64

## Modern Renderer preview

[Pobierz nowy prototyp renderera](https://github.com/pawel2137oioi/Niewiem/raw/refs/heads/codex/graphics-test-download/Downloads/OpenRails-ModernRenderer-preview.zip).

Osobna paczka dodaje HDR, bloom, FXAA, chmury objetosciowe 3D, cienie chmur
i odbicia sceny w wodzie. **To prototyp, nie gotowa wymiana calego silnika
ani potwierdzony wyglad 1:1 ze screenami TSRE.** Modele i tekstury trasy
pozostaja jej oryginalnymi zasobami. Wymaga Windows x64, DirectX 10+
i .NET Desktop Runtime 6.0.36 x64.

Aktualizacja chmur usuwa ekranowe ziarno podczas ruchu kamery, stosuje spojny
szum 3D i domyslnie 50% rozdzielczosci. W Options > Environment jest regulacja
Cloud resolution (%) 25–100; 25 obniza koszt, 50 jest ustawieniem domyslnym.
Rozjasniono dzienne chmury, a bloom obejmuje mocniejsze swiatla.

Rozpakuj do nowego folderu i uruchom OpenRails.exe. Dodaj zawartosc przez
Content > Manually Installed > Add > Browse. Paczka startuje z High i sila 100.
Obejrzyj widok 2/8; chmury wymagaja zachmurzenia (Ctrl i =/+ zwieksza je).
Log jest w `Logs/OpenRailsLog.txt` obok EXE. Dokladne kroki sa w
[instrukcji](ModernRenderer-URUCHOMIENIE.txt).

Sprawdzono skompilowane shadery testami pikseli oraz uruchomienie Północnej
Polski z EU07-465 przez Wine. Nie zmierzono 60 FPS na natywnym Windows/GPU.
High dodaje koszt renderowania; Balanced pomija najdrozsze efekty.
W testowej trasie pozostaja czarne powierzchnie i problemy wygladu wody;
wystapilo tez jednorazowe zakonczenie przez watchdog renderera pod Wine.
Zakres, testy i pozostale ograniczenia opisuje [raport](../Docs/Modern-Renderer.md).
Suma SHA-256 jest w pliku obok ZIP-a. Dane PKOR nie sa zawarte w paczce.

## Starsza paczka atmosfery v2

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
