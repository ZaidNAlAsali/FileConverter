#!/usr/bin/env python
"""Smoke-test ZFileConverter builds without external Python packages.

The script always tests generated PNG -> JPG and generated MP4 -> audio
conversions through the app's headless CLI. If LibreOffice is available, it also
tests generated DOCX and XLSX files through the Office-to-PDF path. PPTX support
is covered by the same app fallback code, but real PPTX fixture testing is opt-in:
drop a .pptx file in Tests/SmokeTests/fixtures and the script will convert it too.
"""

from __future__ import annotations

import argparse
import os
import shutil
import struct
import subprocess
import sys
import tempfile
import zipfile
import zlib
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
FIXTURES_DIR = Path(__file__).resolve().parent / "fixtures"


def chunk(chunk_type: bytes, data: bytes) -> bytes:
    return (
        struct.pack(">I", len(data))
        + chunk_type
        + data
        + struct.pack(">I", zlib.crc32(chunk_type + data) & 0xFFFFFFFF)
    )


def write_png(path: Path) -> None:
    raw = b"\x00\x00\x7f\xff"  # filter byte + one RGB pixel
    png = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 2, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw))
        + chunk(b"IEND", b"")
    )
    path.write_bytes(png)


def write_zip(path: Path, files: dict[str, str]) -> None:
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, content in files.items():
            archive.writestr(name, content)


def write_docx(path: Path) -> None:
    write_zip(
        path,
        {
            "[Content_Types].xml": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'>
  <Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/>
  <Default Extension='xml' ContentType='application/xml'/>
  <Override PartName='/word/document.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml'/>
</Types>""",
            "_rels/.rels": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>
  <Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='word/document.xml'/>
</Relationships>""",
            "word/document.xml": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
  <w:body>
    <w:p><w:r><w:t>ZFileConverter DOCX smoke test. Unicode: مرحبا ✓</w:t></w:r></w:p>
    <w:sectPr><w:pgSz w:w='12240' w:h='15840'/><w:pgMar w:top='1440' w:right='1440' w:bottom='1440' w:left='1440'/></w:sectPr>
  </w:body>
</w:document>""",
        },
    )


def write_xlsx(path: Path) -> None:
    write_zip(
        path,
        {
            "[Content_Types].xml": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'>
  <Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/>
  <Default Extension='xml' ContentType='application/xml'/>
  <Override PartName='/xl/workbook.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml'/>
  <Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/>
</Types>""",
            "_rels/.rels": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>
  <Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='xl/workbook.xml'/>
</Relationships>""",
            "xl/workbook.xml": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'>
  <sheets><sheet name='Smoke' sheetId='1' r:id='rId1'/></sheets>
</workbook>""",
            "xl/_rels/workbook.xml.rels": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>
  <Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet' Target='worksheets/sheet1.xml'/>
</Relationships>""",
            "xl/worksheets/sheet1.xml": """<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>
  <sheetData>
    <row r='1'><c r='A1' t='inlineStr'><is><t>ZFileConverter XLSX smoke test</t></is></c></row>
    <row r='2'><c r='A2'><v>42</v></c></row>
  </sheetData>
</worksheet>""",
        },
    )


def find_soffice() -> str | None:
    configured = os.environ.get("LIBREOFFICE_PATH")
    candidates = [configured] if configured else []
    candidates += [
        r"C:\Program Files\LibreOffice\program\soffice.exe",
        r"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
        shutil.which("soffice.exe"),
        shutil.which("libreoffice.exe"),
        shutil.which("soffice"),
        shutil.which("libreoffice"),
    ]
    for candidate in candidates:
        if candidate and Path(candidate).exists():
            return candidate
    return None


def run_command(command: list[str], timeout: int) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, text=True, capture_output=True, timeout=timeout)


def run_conversion(exe: Path, preset: str, input_path: Path, timeout: int) -> subprocess.CompletedProcess[str]:
    command = [str(exe), "--headless", "--conversion-preset", preset, str(input_path)]
    return run_command(command, timeout)


def write_test_video(ffmpeg: Path, path: Path, timeout: int) -> None:
    command = [
        str(ffmpeg),
        "-hide_banner",
        "-loglevel",
        "error",
        "-f",
        "lavfi",
        "-i",
        "testsrc=size=160x90:rate=15",
        "-f",
        "lavfi",
        "-i",
        "sine=frequency=440:sample_rate=44100",
        "-t",
        "3",
        "-pix_fmt",
        "yuv420p",
        "-c:v",
        "libx264",
        "-preset",
        "ultrafast",
        "-c:a",
        "aac",
        str(path),
    ]
    result = run_command(command, timeout)
    if result.returncode != 0:
        raise AssertionError(f"Failed to generate test video: {result.stderr or result.stdout}")


def validate_media(ffmpeg: Path, path: Path, timeout: int) -> None:
    command = [str(ffmpeg), "-hide_banner", "-v", "error", "-i", str(path), "-f", "null", "-"]
    result = run_command(command, timeout)
    if result.returncode != 0:
        raise AssertionError(f"FFmpeg could not decode output {path}: {result.stderr or result.stdout}")


def assert_output(path: Path) -> None:
    if not path.exists():
        raise AssertionError(f"Expected output was not created: {path}")
    if path.stat().st_size <= 0:
        raise AssertionError(f"Expected output is empty: {path}")


def run_case(exe: Path, preset: str, input_path: Path, output_path: Path, timeout: int, validator=None) -> None:
    result = run_conversion(exe, preset, input_path, timeout)
    print(f"[{preset}: {input_path.suffix} -> {output_path.suffix}] exit={result.returncode}")
    if result.stdout:
        print(result.stdout.strip())
    if result.stderr:
        print(result.stderr.strip(), file=sys.stderr)
    if result.returncode != 0:
        raise AssertionError(f"Conversion failed with exit code {result.returncode}: {input_path}")
    assert_output(output_path)
    if validator:
        validator(output_path)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--configuration", default="Debug", choices=["Debug", "Release"])
    parser.add_argument("--platform", default="x64")
    parser.add_argument("--timeout", type=int, default=180)
    args = parser.parse_args()

    build_dir = REPO_ROOT / "Application" / "FileConverter" / "bin" / args.platform / args.configuration
    exe = build_dir / "FileConverter.exe"
    ffmpeg = build_dir / "ffmpeg.exe"
    if not exe.exists():
        raise FileNotFoundError(f"Build output not found: {exe}")
    if not ffmpeg.exists():
        raise FileNotFoundError(f"Bundled FFmpeg not found in build output: {ffmpeg}")

    with tempfile.TemporaryDirectory(prefix="ZFileConverter-Smoke-") as temp_dir:
        temp = Path(temp_dir)

        png = temp / "sample image.png"
        write_png(png)
        run_case(exe, "To Jpg", png, temp / "sample image.jpg", args.timeout)

        video = temp / "transcription sample.mp4"
        write_test_video(ffmpeg, video, args.timeout)
        media_validator = lambda output: validate_media(ffmpeg, output, args.timeout)
        run_case(exe, "To Mp3", video, temp / "transcription sample.mp3", args.timeout, media_validator)
        run_case(exe, "To Ogg", video, temp / "transcription sample.ogg", args.timeout, media_validator)
        run_case(exe, "To Aac", video, temp / "transcription sample.aac", args.timeout, media_validator)
        run_case(exe, "To Opus", video, temp / "transcription sample.opus", args.timeout, media_validator)
        run_case(exe, "Transcription/To Mp3 (mono)", video, temp / "transcription sample (transcription).mp3", args.timeout, media_validator)
        run_case(exe, "Transcription/To Ogg (mono)", video, temp / "transcription sample (transcription).ogg", args.timeout, media_validator)
        run_case(exe, "Transcription/To Opus (mono)", video, temp / "transcription sample (transcription).opus", args.timeout, media_validator)

        soffice = find_soffice()
        if soffice:
            print(f"LibreOffice detected: {soffice}")
            docx = temp / "sample document.docx"
            write_docx(docx)
            run_case(exe, "To Pdf", docx, temp / "sample document.pdf", args.timeout)

            xlsx = temp / "sample workbook.xlsx"
            write_xlsx(xlsx)
            run_case(exe, "To Pdf", xlsx, temp / "sample workbook.pdf", args.timeout)
        else:
            print("LibreOffice not detected. Skipping generated Office-to-PDF fallback smoke cases.")

        if FIXTURES_DIR.exists():
            for pptx in sorted(FIXTURES_DIR.glob("*.pptx")):
                output = temp / (pptx.stem + ".pdf")
                copied = temp / pptx.name
                shutil.copy2(pptx, copied)
                run_case(exe, "To Pdf", copied, output, args.timeout)

    print("Smoke tests passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
