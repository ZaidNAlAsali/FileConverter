# Contributing to ZFileConverter

Thanks for helping improve an Explorer-first Windows utility. Focused fixes and well-scoped enhancements are easiest to review and safest to ship.

## Before opening a change

- Search existing issues and pull requests.
- Open an issue first for large behavior, preset-default, installer, or shell-extension changes.
- Preserve the concise Explorer menu. New capabilities do not automatically need a new first-run preset.
- Never include private documents, media, settings, diagnostics, credentials, or signing material.

## Development setup

Requirements:

- Visual Studio 2022 or Visual Studio Build Tools 2022
- .NET Framework 4.8 targeting pack
- Git

WiX Toolset 5 and project dependencies restore through NuGet.

From Git Bash or MSYS:

```bash
"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" \
  FileConverter.sln \
  -t:Restore,Build \
  -p:Configuration=Release \
  -p:Platform=x64 \
  -v:minimal
```

## Pull-request expectations

1. Explain the user-visible problem and the chosen fix.
2. Keep conversion, Explorer, installer, and settings behavior backward-compatible unless the change explicitly requires otherwise.
3. Build the complete x64 Release solution after the final edit.
4. Exercise the behavior relevant to the change and report exactly what was verified.
5. For WPF work, inspect both themes and the affected compact layout.
6. Run `git diff --check` and remove accidental generated files or personal paths.
7. Update the README or changelog when public behavior changes.

Do not commit `bin`, `obj`, installers, certificates, settings, diagnostics, or private signing files.

## Branding assets

The application icon and README hero are generated with Pillow:

```bash
python Tools/generate-brand-assets.py
```

Commit both the source script and generated public assets when changing the identity.

## License

Contributions are accepted under the repository's GNU GPL v3 license.
