# ZFileConverter smoke tests

Run after building the app:

```bash
python Tests/SmokeTests/run-smoke-tests.py --configuration Debug
python Tests/SmokeTests/run-smoke-tests.py --configuration Release
```

What it checks:

- Generated PNG to JPG through `--headless`.
- Generated MP4 to MP3, OGG, AAC, and Opus through `--headless`.
- Generated MP4 to mono transcription MP3, OGG, and Opus presets.
- If LibreOffice is installed, generated DOCX to PDF through the Office fallback path.
- If LibreOffice is installed, generated XLSX to PDF through the Office fallback path.
- Optional PPTX fixtures: place `.pptx` files in `Tests/SmokeTests/fixtures/` and the script converts them to PDF too.

The script has no external Python dependencies.
