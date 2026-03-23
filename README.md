# LaserGunGame

Gra tworzona w **Godot 4.6.1** (C#) w ramach TFNiS.

## Wymagania

| Narzędzie | Wersja | Link |
|-----------|--------|------|
| Godot Engine (.NET) | **4.6.1** | [godotengine.org/download](https://godotengine.org/download/) |
| .NET SDK | **8.0+** | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Git LFS | najnowsza | [git-lfs.com](https://git-lfs.com/) |

> **Ważne:** Pobierz wersję Godot oznaczoną jako **Godot Engine - .NET** (nie standardową), ponieważ projekt korzysta z C#.

## Uruchomienie lokalne

1. **Zainstaluj Git LFS** (jeśli jeszcze nie masz):

   ```bash
   git lfs install
   ```

2. **Sklonuj repozytorium:**

   ```bash
   git clone https://github.com/Kolo-Naukowe-Informatykow/TFNiS-laser-gun-game.git
   cd TFNiS-laser-gun-game
   ```

3. **Otwórz projekt w Godocie:**

   - Uruchom **Godot Engine (.NET)**
   - Kliknij **Import** → wskaż folder z projektem → **Import & Edit**

4. **Zbuduj solucję C#:**

   - W edytorze Godot: **MSBuild** → **Build** (lub `Ctrl+Shift+B` / przycisk młotka)
   - Alternatywnie z terminala:
     ```bash
     dotnet build
     ```

5. **Uruchom grę:**

   - Kliknij przycisk ▶ **Play** (lub `F5`) w edytorze Godot

## Struktura projektu

Projekt jest tworzony z architekturą arkady — zawiera wiele niezależnych gier ze wspólnym systemem UI, sterowania i managerów.

```
├── Assets/                     # Zasoby graficzne, audio, modele 3D, fonty itp.
│   ├── Textures/
│   ├── Models/
│   ├── Audio/
│   └── Fonts/
├── Source/                     # Cały kod źródłowy i sceny
│   ├── Common/                 # Wspólne dla wszystkich gier
│   │   ├── UI/                # Menu główne, HUD framework, wspólne elementy UI
│   │   ├── Managers/          # Globalne singletony (GameManager, AudioManager, etc.)
│   │   ├── Components/        # Generyczne komponenty (HealthBar, Hitbox)
│   ├── Games/                 # Poszczególne gry
│   │   ├── SpaceShooter/
│   │   │   ├── Scenes/       # Sceny specyficzne dla gry
│   │   │   ├── Enemies/      # Wrogowie specyficzni dla tej gry
│   │   └── [NextGame]/       # Kolejne gry
│   │       ├── Scenes/
│   │       └── ...
│   └── Shaders/               # Globalne shadery
├── project.godot
├── LaserGunGame.sln
└── LaserGunGame.csproj
```

### Zasady nazewnictwa

- **PascalCase** — dotyczy: nazw folderów, plików `.cs`, plików `.tscn` i klas C#.

  | Element | Przykład |
  |---------|----------|
  | Folder | `SpaceShooter/`, `Common/`, `UI/` |
  | Scena | `SpaceShooter.tscn`, `MainMenu.tscn` |
  | Skrypt C# | `SpaceShooter.cs`, `MainMenu.cs` |
  | Klasa C# | `public partial class SpaceShooter` |

- **Skrypt przy scenie** — jeżeli kod należy do jednej sceny, plik `.cs` leży **w tym samym folderze** co scena `.tscn`, do której jest przypisany.

  ```
  Source/Games/SpaceShooter/Scenes/
  ├── SpaceShooter.tscn       ← scena główna gry
  └── SpaceShooter.cs         ← skrypt przypisany do sceny
  ```

### Architektura gier

Każda gra to **niezależny moduł**:

- **Gry** — umieszczane w `Source/Games/[GameName]/`
- **Wspólne elementy** — tylko rzeczywiście uniwersalne trafiają do `Source/Common/`
- **Wrogowie, shadery, scenki** — specyficzne dla danej gry, w jej folderze
- **GameManager** — obsługuje zmianę między grami i załadowanie scen