# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PoeTradeSearch** — WPF/.NET 4.6 Windows desktop application (Korean/English) for searching Path of Exile trade site items. The user presses Ctrl+C on an in-game item, and the app parses the clipboard text and opens a trade search window.

## Build & Run

- **IDE**: Visual Studio (solution file `PoeTradeSearch.sln`)
- **Build via MSBuild**:
  ```
  msbuild PoeTradeSearch.sln /p:Configuration=Release
  ```
- **Debug path**: In `#if DEBUG` blocks, data files are read from `..\..\\_POE_Data\\` (project root). In Release, they're read from the directory next to the `.exe`.
- **Run**: Launch `bin\Release\PoeTradeSearch.exe` (or Debug). Must run as Administrator for hotkey/shortcut features.
- **Target framework**: .NET Framework 4.6 — do not upgrade without testing Windows 7 compatibility.

## Architecture

The codebase uses a single `WinMain` partial class split across multiple files:

| File | Role |
|------|------|
| `Configure.cs` | Static config (`RS` class) holding URLs, stat ID dictionaries; `WinMain.Setting()` / `LoadData()` — reads JSON config and filter data files from `_POE_Data\` |
| `Contracts.cs` | All `[DataContract]` model classes: `ItemOption`, `ItemBaseName`, `ConfigData`, `ConfigOption`, etc. |
| `Functions.cs` | `Native` P/Invoke declarations (user32.dll); low-level helpers (HTTP, JSON serialization, clipboard) |
| `Methods.cs` | UI control reset/population logic (`ResetControls`, filter list building) |
| `Updates.cs` | Version check against GitHub `VERSIONS` file; self-update via zip download |
| `WinMain.xaml.cs` | Window lifecycle, hotkey registration, clipboard chain hooks, auto-search timer |
| `WinPopup.xaml.cs` | Popup window for showing price results |
| `App.xaml.cs` | Application entry point, tray icon, system tray right-click exit |

## Data Files (`_POE_Data\`)

| File | Purpose |
|------|---------|
| `Config.txt` | JSON user config (league, server, shortcuts, options) |
| `Parser.txt` | JSON definitions for rarity/currency/influence text parsing |
| `FiltersKO.txt` / `FiltersEN.txt` | Korean/English item stat filter data (auto-downloaded if missing) |

- Deleting `FiltersKO.txt` forces re-download on next launch.
- Server index: `0` = Korean (`poe.game.daum.net`), `1` = English (`pathofexile.com`), stored in `RS.ServerLang`.

## Key Dictionaries in `RS` (Configure.cs)

- `lFilterType` — maps API filter type strings to Korean labels
- `lDefaultPosition` — stat IDs checked by default
- `lDisable` — stat IDs always disabled
- `lParticular` — stat IDs needing special handling (value 1 or 2)
- `lResistance` — resistance stat IDs (for auto total-resistance grouping)
- `lPseudo` — maps explicit stat IDs → pseudo stat IDs for auto pseudo-selection

## Config.txt Options

Important editable options in `_POE_Data\Config.txt`:
- `league` — trade league name (must match PoE trade site exactly)
- `server` — `"ko"` / `"en"` / `"auto"`
- `search_price_count` — max 80, must be multiple of 20
- `auto_search_delay` — seconds between auto price refreshes (0 = disabled); setting too low risks trade site IP block
- Shortcuts use Windows virtual key codes; special values: `{Pause}`, `{Close}`, `{Link}URL{Link}`, `{Wiki}`, `{Run}`, `{grid:stash}`, `{grid:quad}`

## Important Conventions

- All stat filters are identified by `stat_XXXXXXXXXX` ID strings — never by display name.
- Korean server = index `0`, English = index `1` throughout all array pairs (`TradeUrl[0/1]`, `mFilterData[0/1]`, etc.).
- The clipboard watcher uses Win32 `SetClipboardViewer` chain (legacy API), not `AddClipboardFormatListener`.
- HTTP calls go through `SendHTTP()` in `Functions.cs` — it sets `UserAgent` and handles `server_redirect` option.
- `Json` helper class (in `Functions.cs`) wraps `DataContractJsonSerializer` — all JSON models must use `[DataContract]` / `[DataMember]`.
