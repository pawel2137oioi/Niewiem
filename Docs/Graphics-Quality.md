# Ustawienia grafiki dla różnych komputerów

W **Options → Environment** wybierz renderer **Modern**, a następnie ustaw
efekty osobno. **Off / Low / Medium / High / Ultra** oznaczają odpowiednio
wyłączone, niskie, średnie, wysokie i najwyższe ustawienia. Wymagany jest
DirectX 10+. Renderer Original oraz środowiska MSTS omijają te nowe efekty.

**Starting preset → Apply preset** jest punktem wyjścia. Możesz potem dowolnie
łączyć poziomy, np. High dla chmur, Low dla cieni i Off dla odbić. Profile nie
zmieniają rozdzielczości ekranu, VSync, celu FPS ani działania Performance Tuner.
Te opcje pozostają w zakładce Graphics. Nie ma profilu gwarantującego daną
liczbę FPS na każdym komputerze i każdej trasie.

| Profil | Chmury 3D / skala | Odbicia | Bloom | Cienie | Zasięg |
|---|---|---|---|---|---|
| Low | Low / 25% | Off | Low | Low | 1000 m |
| Medium | Medium / 33% | Low | Medium | Medium | 2000 m |
| High | High / 50% | High | High | High | 2500 m |
| Ultra | Ultra / 75% | Ultra | Ultra | Ultra | 4000 m |

Nowa paczka startuje od ustawień Medium, w oknie 1280×720. Wybierz własną
rozdzielczość w Graphics. Starsze konfiguracje zachowują dotychczasowe
włączenia/wyłączenia efektów, dopóki nie zapiszesz nowych poziomów.

## Co regulują poszczególne opcje

- **Volumetric clouds**: Low, Medium, High i Ultra zmieniają dokładność całkowania
  objętości oraz sugerowaną skalę obrazu. Cienie chmur działają od Medium.
  Off używa tańszej warstwy chmur 2D. Chmury nadal zależą od pogody; przy
  bezchmurnym niebie podniesienie jakości ich nie dodaje.
- **Cloud resolution (%)**: można zmienić niezależnie od poziomu chmur. Wybór
  poziomu podpowiada skalę 25/33/50/75%; możesz ją później poprawić ręcznie.
- **Water reflections**: Off używa przybliżenia nieba; kolejne poziomy zwiększają
  rozdzielczość i częstotliwość odświeżania odbicia sceny.
- **Bloom**: jakość poświaty jasnych obszarów. Off usuwa efekt; wyższe poziomy
  zwiększają rozdzielczość jego buforów, nie jasność całego obrazu.
- **Shadows**: osobna jakość map cieni. Custom zachowuje wcześniej zapisane
  niestandardowe parametry. W Graphics nadal możesz regulować dynamiczne cienie
  i cienie wszystkich obiektów.
- **Color grading, Sun halo, Ground fog**: niezależne przełączniki lub suwaki.
  Ground fog = 0 wyłącza dodatkową mgłę przy ziemi; mgła wynikająca z pogody trasy
  nadal działa.

Kliknij **OK** i rozpocznij nową jazdę. **Cancel** odrzuca zmiany, również po
użyciu profilu. Opcje nie przełączają renderera w trakcie aktywnej jazdy.

## Koszt renderowania

| Poziom | Maks. przedziałów promienia chmur | Limit obrazu chmur | Limit odbicia / odświeżanie | Rozdzielczość bloomu | Mapy cieni |
|---|---:|---|---|---|---|
| Low | 16 | 1280×720 | 320×180 / co 4 klatki | 1/8 wymiarów sceny | 1×1024 |
| Medium | 32 | 1280×720 | 480×270 / co 3 klatki | 1/4 | 2×1024 |
| High | 48 | 1280×720 | 640×360 / co 2 klatki | 1/2 | 2×2048 |
| Ultra | 80 | 1536×864 | 960×540 / każda klatka | pełna, maks. 1920×1080 | 4×2048 |

Obrazy efektów zachowują proporcje ekranu. Skala procentowa oraz limity działają
jednocześnie; Ultra nie oznacza chmur w pełnym 4K. Odbicia mają też limit połowy
wymiarów ekranu. Zmiana wysokości wody lub duży ruch kamery może wymusić wcześniejsze
odświeżenie. Cienie chmur i odbicia sceny są dodatkowymi kosztami względem samego
nieba. Wyższe poziomy potrzebują więcej GPU i pamięci.

Jeśli FPS jest niski, zacznij od Medium albo Low, wyłącz odbicia i zmniejsz skalę
chmur. W gęstej zabudowie lub przy wielu pociągach sprawdź też zasięg, cienie
wszystkich obiektów i złożoność taboru. Wyniki porównuj w tej samej scenie, kamerze,
porze dnia i pogodzie. Zakładka diagnostyczna HUD pokazuje poziomy efektów.

To nadal prototyp opisany w [raporcie renderera](Modern-Renderer.md). Poziomy
jakości nie zastępują nowych modeli, tekstur ani materiałów tras. Testy poziomów,
zapis/odczyt INI i zachowanie strony opcji sprawdzono; FPS na różnych modelach
kart nie został zmierzony.
