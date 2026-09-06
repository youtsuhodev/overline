# Overline

**Your VRChat chatbox, rebuilt.** One line above your head, assembled from
independent modules — music, stats, radar, heart rate — with privacy consent
first and a hard 144-character budget.

This is the ground-up rewrite of MagicChatBox (v2). The complete legacy
codebase is preserved untouched in [`OLD/`](OLD/) for reference.

## Layout

```
Overline.slnx            Solution (.NET 10, modern XML solution format)
├── Overline.Core/        Contracts & pure logic — IModule, settings store,
│                         OSC encoder, line builder, privacy consent, clock
├── Overline.Modules.Time/  Reference module (local time + emoji clock face)
├── Overline.App/         WPF shell: DI wiring, UDP OSC sender, main window
└── Overline.Tests/       xUnit tests (Core + modules)
```

`OLD/` contains the previous MagicChatBox solution — do not build or edit it;
it is kept only to mine it for porting features into Overline.

## Principles

- **Core has zero platform dependencies.** Everything in `Overline.Core` is
  pure .NET: testable on any OS, no WPF, no Windows-only APIs.
- **Modules are isolated.** Each module implements `IModule` (start/stop,
  state machine) and contributes `OscSegment`s with a priority used to drop
  content gracefully when the 144-character budget overflows.
- **Privacy consent is a Core primitive.** Modules must check
  `IPrivacyConsentService.IsApproved(hook)` before touching sensitive data.
- **Settings are typed and per-section.** `ISettingsStore` persists small POCOs
  per key into `%APPDATA%/Overline/settings/*.json`.
- **Warnings are errors.** `Directory.Build.props` enforces
  `TreatWarningsAsErrors`; package versions are centralized in
  `Directory.Packages.props`; the SDK is pinned in `global.json`.

## Build & test

```bash
dotnet build Overline.slnx
dotnet test Overline.slnx
dotnet run --project Overline.App
```

Requires the .NET 10 SDK (see `global.json`).

## Build the installer (.exe)

`build-installer.bat` publishes a self-contained `Overline.exe` (no .NET install
needed) and compiles it into an Inno Setup installer:

```
build-installer.bat
```

Outputs under `artifacts/` (gitignored):

- `artifacts\publish\win-x64\Overline.exe` — portable single-file build
- `artifacts\installer\Overline-Setup-<version>.exe` — installer with Start-menu
  shortcut, desktop shortcut (optional), uninstaller, FR/EN languages

The batch delegates to `build-installer.ps1` (a `.bat` alone can't parse `<`/`>`
for the version tag). The version is read from `<Version>` in
`Directory.Build.props`. Requires **Inno Setup 6** installed
(`winget install JRSoftware.InnoSetup`).

## Roadmap (validated audit, not yet started)

- CI test gate (the legacy repo never ran its 167 test files in CI)
- OSC Mapping module (bind any data to avatar parameters, no code)
- OBS integration (scene, recording status via obs-websocket)
- Kick / YouTube Live alongside Twitch / TikTok
- Local AI (Ollama-compatible) for IntelliChat-style features
- Session analytics from VRChat log radar
- MagicChatboxAPI as a real SDK: community plugin loader
- Profile manager UI (SFW/NSFW, FR/EN switching)
