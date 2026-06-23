# ZFileConverter

## Description

**ZFileConverter** is a modernized fork/twist of the abandoned File Converter project. It keeps the simple Windows Explorer context-menu workflow, while hardening conversion reliability, build reproducibility, and Office document conversion.

The current focus is making document conversion dependable, especially Word/Excel/PowerPoint to PDF.

## What is improved in this fork

- Microsoft Office detection now checks both App Paths and COM ProgIDs.
- Word, Excel, and PowerPoint conversions can fall back to LibreOffice for PDF output when Microsoft Office is missing or export fails.
- LibreOffice fallback runs headless, captures stdout/stderr, uses an isolated temp folder, times out safely, and validates the generated PDF.
- Output files are validated before a conversion is marked successful.
- `--headless`, `--exit-when-finished`, and `--fail-on-error` make CLI smoke tests and automation deterministic.
- Local Release installer builds no longer require a private signing file.
- Package references have been refreshed and vulnerability checks pass with current NuGet sources.

## Optional LibreOffice fallback

Install LibreOffice normally, or set:

```bash
LIBREOFFICE_PATH="C:\\Program Files\\LibreOffice\\program\\soffice.exe"
```

ZFileConverter searches:

- `LIBREOFFICE_PATH`
- `C:\Program Files\LibreOffice\program\soffice.exe`
- `C:\Program Files (x86)\LibreOffice\program\soffice.exe`
- `PATH`

## Headless CLI examples

```bash
FileConverter.exe --headless --conversion-preset "To Jpg" "C:\path\image.png"
FileConverter.exe --headless --conversion-preset "To Pdf" "C:\path\document.docx"
FileConverter.exe --exit-when-finished --fail-on-error --conversion-preset "To Pdf" "C:\path\workbook.xlsx"
```

`--headless` implies `--exit-when-finished` and `--fail-on-error`.

## Smoke tests

After building:

```bash
python Tests/SmokeTests/run-smoke-tests.py --configuration Debug
python Tests/SmokeTests/run-smoke-tests.py --configuration Release
```

The smoke tests always verify PNG to JPG. If LibreOffice is installed, they also verify generated DOCX/XLSX to PDF through the Office fallback path. Drop `.pptx` files in `Tests/SmokeTests/fixtures/` to include PowerPoint fixtures.

## Setup development environment

### Requirements

For ZFileConverter and its Explorer extension:

- Visual Studio 2022 / Visual Studio Build Tools 2022
- .NET Framework 4.8 targeting pack

For the installer:

- WiX 5, restored by NuGet
- Windows SDK signing tools if you want signed installers

Recommended build command from Git Bash/MSYS:

```bash
"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" FileConverter.sln -t:Restore,Build -p:Configuration=Debug -p:Platform=x64 -v:minimal
```

## Upstream credit

ZFileConverter is based on the original GPLv3 File Converter project by Adrien Allard and contributors.

## License

ZFileConverter remains licensed under GPL version 3.
