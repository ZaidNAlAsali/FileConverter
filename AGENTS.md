# AGENTS.md

## Product contract

ZFileConverter is a lightweight, Explorer-first Windows file converter. The primary interaction is:

1. Select one or more files in File Explorer.
2. Open the classic context menu.
3. Choose `ZFileConverter` and a compatible preset.
4. Watch a native conversion window only while work is running.

Protect that shape. Do not turn the product into a library manager, cloud workflow, tray application, background service, Electron shell, or always-running process.

The quality priorities, in order, are:

1. Conversion correctness and source-file safety.
2. Explorer stability and fast menu creation.
3. Settings preservation and upgrade compatibility.
4. Responsive native UI on ordinary and older laptops.
5. Small, understandable changes over architectural churn.
6. Visual polish consistent with the existing product identity.

## Repository map

- `FileConverter.sln`: build this solution, not an individual project.
- `Application/FileConverter/`: .NET Framework 4.8 x64 WPF application and conversion engine.
  - `Application.xaml.cs`: startup, command-line parsing, window routing, shutdown, updater launch.
  - `Application.xaml`: global resources and Markdown converters.
  - `Services/SettingsService.cs`: user/default settings load, merge, save, and revert.
  - `Services/SettingsService.Migration.cs`: serialization migrations.
  - `Services/NavigationService.cs`: window registration, show/close accounting, shutdown decisions.
  - `Services/ConversionService.cs`: bounded conversion queue and worker threads.
  - `Services/UpgradeService.cs`: update feed, MSI download, checksum validation.
  - `ViewModels/SettingsViewModel.cs`: preset editor and Save/Close semantics.
  - `Views/SettingsWindow.xaml`: Settings UI.
  - `Views/HelpWindow.xaml`: Explorer onboarding/tutorial.
  - `Views/Resources/ConversionPresetTemplates.xaml`: output-specific preset editors.
  - `Settings.default.xml`: fresh-install defaults and the 16-preset first-run library.
  - `ConversionJobs/`: FFmpeg, ImageMagick, Office, LibreOffice, PDF, GIF, ICO, and CDA conversion paths.
- `Application/FileConverterExtension/`: x64 .NET Framework 4.8 SharpShell Explorer extension.
  - `FileConverterExtension.cs`: selection filtering, context-menu creation, settings reload, process launch.
  - Keep this code defensive and extremely small because it runs inside Explorer.
- `Installer/`: WiX v4 MSI.
  - `Product.wxs`: version, files, shell registration, shortcuts, upgrade behavior, artwork.
- `Middleware/`: intentionally bundled native/legacy conversion dependencies.
- `Resources/`: generated brand, icon, and installer assets.
- `Tools/generate-brand-assets.py`: deterministic source for icon and installer artwork.
- `docs/`: public screenshots and focused reference documentation.
- `.github/workflows/windows-release.yml`: authoritative CI Release build.
- `version.xml`: live x64 updater feed.

## Supported runtime modes

Normal launch with no arguments shows the Explorer tutorial and then continues into Settings.

Important arguments include:

- `--settings`: open Settings directly.
- `--conversion-preset "<full preset name>" <files...>`: run a preset.
- `--input-files <path>`: read input paths from a temporary list file.
- `--headless`: no message boxes or conversion window; implies exit-on-finish and fail-on-error.
- `--exit-when-finished`
- `--fail-on-error`
- `--verbose`: show Diagnostics.
- `--version`
- Installer-only maintenance arguments exist for shell registration and post-install initialization. Trace `Application.Initialize()` before changing argument behavior.

Preserve quoting and the file-list fallback used when Windows' command-line length would be exceeded.

## Settings invariants

User settings live under:

```text
%LOCALAPPDATA%\ZFileConverter\Settings.user.xml
```

Related lightweight application state is stored in `Registry.xml` in the same folder. This is an internal XML store, not the Windows registry.

Rules:

- Never delete, replace, or reset a user's settings during testing without making a byte-for-byte backup first.
- Existing customized preset libraries must survive upgrades.
- Fresh installs receive the concise 16-preset default library.
- Dark is the fresh-install default. Light must remain immediately selectable and persistent.
- Save persists while keeping Settings open.
- Close discards only changes made since the last successful Save.
- `SettingsViewModel` is transient so a reopened Settings window binds to the current `SettingsService.Settings` object after a revert.
- Explorer reloads preset references when the settings file timestamp changes. Keep the last known-good menu if a save is briefly in progress.
- The destructive `Delete` post-conversion action runs only after conversion success and expected-output validation.

## UI and branding invariants

Visible product branding is `ZFileConverter`.

Visual language:

- Midnight/navy foundation
- Layered mineral surfaces
- Cobalt primary accent
- Restrained coral support accent
- Purpose-built Dark and Light themes

Do not replace this with a generic corporate dashboard or an unrelated design system.

The application mark is a midnight rounded square with a layered document, coral fold, and cobalt Z/conversion arrow.

Windows icons must remain legible at:

```text
16, 20, 24, 32, 40, 48, 64, 128, 256 px
```

Critical compatibility rule: ICO frames consumed by legacy `System.Drawing.Icon` must be DIB/BMP-backed. PNG-compressed ICO entries produced rainbow-noise corruption in Explorer. After icon changes, test the exact `System.Drawing.Icon(...).ToBitmap()` path and the installed native context menu.

WiX artwork dimensions:

- Banner: `493x58`
- Dialog/sidebar: `493x312`

Always inspect the real MSI pages. Source bitmaps and a successful build do not prove WiX overlays remain readable.

## Performance and footprint policy

The application must remain suitable for weak laptops:

- No tray process, daemon, telemetry runtime, embedded browser, or idle polling loop.
- Keep conversion concurrency bounded. Fresh defaults use two simultaneous conversions.
- Hardware acceleration remains opt-in because support depends on the machine.
- Do not perform network work synchronously on the UI thread.
- Do not load conversion engines merely to show Settings or Help.
- The shell extension may check a file timestamp and parse settings, but must not load WPF, FFmpeg, ImageMagick, Office, or other conversion engines.
- Native engines account for most package size. Do not call FFmpeg, Ghostscript, or ImageMagick “bloat” while their supported conversions remain product features.
- Do remove dependencies that are genuinely unused.
- Raw build output may contain extra ImageMagick architectures from the AnyCPU package. The public x64 MSI must package only the x64 native library.
- Measure before optimizing. Retain an optimization only when it is simple, verified, and materially faster without behavior or visual regressions.

Observed 2.3-era baseline on a normal development machine:

- Settings working set: about 95 to 100 MiB.
- Settings native-window readiness: about 6 seconds.
- Settings XML and ViewModel work account for well under one second; legacy WPF XAML creation/rendering dominates.
- MSI size: about 61 MiB, mostly compressed conversion engines.

A framework rewrite is not justified solely by those numbers. Investigate a concrete regression, not an abstract desire for newer technology.

## Build

Host requirements:

- Windows x64
- Visual Studio 2022 Build Tools with .NET desktop and .NET Framework 4.8 targeting packs
- WiX v4 available through the project
- NuGet access for restore

From a Git Bash/Hermes shell:

```bash
"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" \
  FileConverter.sln \
  -t:Restore,Rebuild \
  -p:Configuration=Release \
  -p:Platform=x64 \
  -v:minimal
```

Build the solution because project post-build steps depend on `$(SolutionDir)`.

Required artifacts:

```text
Application/FileConverter/bin/x64/Release/FileConverter.exe
Application/FileConverterExtension/bin/x64/Release/FileConverterExtension.dll
Installer/bin/x64/Release/ZFileConverter-setup.msi
```

Do not treat compilation as final verification.

## Verification matrix

For behavior or release changes, verify the affected paths directly:

- Normal launch: tutorial, then Settings.
- `--settings`: Settings directly.
- Tutorial OK and title-bar close behavior.
- Save persists and leaves Settings open.
- Unsaved Close reverts to the last saved state.
- Close/reopen in a live conversion process receives fresh state.
- Dark and Light theme persistence.
- Settings Save is reflected in a fresh native Explorer menu without restarting Explorer.
- Mixed-selection preset compatibility.
- Representative FFmpeg and ImageMagick conversions using disposable files.
- Document conversions only when their Office/LibreOffice prerequisites are available.
- Headless exit codes and error paths when relevant.
- Real MSI welcome/banner pages.
- Installed app, Add/Remove Programs metadata, shortcuts, and shell extension.
- User settings hash before and after MSI upgrade.

Do not add a broad smoke-test suite unless the maintainer explicitly asks. Small deterministic regression checks are acceptable when they directly protect changed parsing, hashing, or quoting logic.

## Security and privacy

- File contents remain local. Do not add uploads, analytics, accounts, or telemetry.
- Never print or commit private file paths, settings contents, credentials, or user filenames from verification screenshots.
- Update downloads must use HTTPS and pass the adjacent `.sha256` asset check before execution.
- A checksum protects integrity, not publisher identity. Do not claim Authenticode signing unless a real trusted certificate signs the artifact.
- XML readers must prohibit DTD resolution.
- Preserve argument quoting for shell-selected paths and preset names.
- Test destructive post-conversion actions only on disposable files.

## Versioning

A public corrective release must get a new patch version. Never silently replace an existing stable asset.

Update all of these together:

1. `Application/FileConverter/Application.xaml.cs`
2. `Application/FileConverter/Properties/AssemblyInfo.cs`
3. `Application/FileConverterExtension/Properties/AssemblyInfo.cs`
4. `Installer/Product.wxs`
5. `version.xml`
6. `README.md` download labels and URLs
7. `CHANGELOG.md`

The retired `version (x86).xml` feed is historical. Do not publish new x86 builds unless the product intentionally restores x86 support.

## Release workflow

1. Back up `%LOCALAPPDATA%\ZFileConverter\Settings.user.xml` and relevant installer state.
2. Run the final x64 Release rebuild.
3. Run `git diff --check`, inspect the entire diff, and scan changed text for secrets/private paths.
4. Run NuGet's vulnerable-package audit.
5. Exercise the changed runtime paths.
6. Install the locally built MSI and verify the real installed product and Explorer extension.
7. Commit and push `integration` only after local verification.
8. Wait for `.github/workflows/windows-release.yml` to pass.
9. Download the CI-produced MSI artifact.
10. Compute SHA-256 from that exact MSI.
11. Publish the MSI and `ZFileConverter-setup.msi.sha256` in a stable GitHub release.
12. Re-download the public asset and verify the hash.
13. Confirm `version.xml` resolves to the published asset.

Do not commit, push, tag, or publish unless the user requested the release or the active task explicitly includes publication.

## Stopping rule

Fix confirmed defects and measurable regressions. Then stop.

Do not extend a release merely to:

- modernize syntax,
- replace WPF or SharpShell,
- add animations,
- add more default presets,
- perform broad naming cleanup,
- chase tiny binary reductions,
- update packages with no security or compatibility reason,
- redesign already-approved screens.

Record optional ideas separately. A finished release with a known, acceptable legacy constraint is better than an endless cleanup branch.
