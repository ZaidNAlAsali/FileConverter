"""Generate the ZFileConverter application and repository brand assets.

Requires Pillow. Run from the repository root with:
    python Tools/generate-brand-assets.py
"""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SIZE = 1024
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def lerp(a, b, t):
    return int(round(a + (b - a) * t))


def gradient_image(size, top, bottom):
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    for y in range(size):
        t = y / (size - 1)
        color = tuple(lerp(top[i], bottom[i], t) for i in range(4))
        draw.line((0, y, size, y), fill=color)
    return image


def build_icon():
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(mask).rounded_rectangle((56, 56, 968, 968), radius=220, fill=255)
    background = gradient_image(SIZE, (9, 18, 34, 255), (17, 40, 68, 255))

    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((240, 90, 930, 790), fill=(35, 111, 232, 70))
    background.alpha_composite(glow.filter(ImageFilter.GaussianBlur(120)))
    background.putalpha(mask)
    canvas.alpha_composite(background)

    border = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(border).rounded_rectangle(
        (72, 72, 952, 952),
        radius=205,
        outline=(113, 164, 240, 70),
        width=8,
    )
    canvas.alpha_composite(border)

    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle(
        (262, 248, 758, 824), radius=60, fill=(0, 0, 0, 125)
    )
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(42)))

    mark = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(mark)

    rear_sheet = [(254, 278), (566, 278), (684, 396), (684, 770), (254, 770)]
    draw.polygon(rear_sheet, fill=(43, 100, 181, 255))
    draw.polygon([(566, 278), (684, 396), (566, 396)], fill=(95, 156, 255, 255))

    front_sheet = [(330, 220), (610, 220), (752, 362), (752, 802), (330, 802)]
    draw.polygon(front_sheet, fill=(240, 246, 255, 255))
    draw.line(front_sheet + [front_sheet[0]], fill=(255, 255, 255, 255), width=18, joint="curve")
    draw.polygon([(610, 220), (752, 362), (610, 362)], fill=(255, 143, 105, 255))
    draw.line(
        [(610, 222), (610, 362), (750, 362)],
        fill=(255, 255, 255, 220),
        width=14,
        joint="curve",
    )

    z_arrow = [
        (390, 410),
        (630, 410),
        (630, 474),
        (486, 610),
        (632, 610),
        (632, 568),
        (724, 646),
        (632, 724),
        (632, 682),
        (390, 682),
        (390, 618),
        (534, 474),
        (390, 474),
    ]
    draw.polygon(z_arrow, fill=(44, 118, 242, 255))
    draw.line([(405, 426), (610, 426)], fill=(118, 174, 255, 180), width=8)
    draw.line([(408, 666), (610, 666)], fill=(20, 82, 189, 120), width=7)

    canvas.alpha_composite(mark)
    return canvas


def vertical_gradient(width, height, top, bottom):
    image = Image.new("RGBA", (width, height))
    draw = ImageDraw.Draw(image)
    for y in range(height):
        t = y / (height - 1)
        color = tuple(lerp(top[i], bottom[i], t) for i in range(4))
        draw.line((0, y, width, y), fill=color)
    return image


def load_font(name, size):
    windows_font = Path("C:/Windows/Fonts") / name
    if windows_font.exists():
        return ImageFont.truetype(str(windows_font), size)
    return ImageFont.load_default()


def build_hero(icon):
    width, height = 1600, 640
    hero = vertical_gradient(width, height, (6, 13, 26, 255), (14, 34, 59, 255))

    atmosphere = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    atmosphere_draw = ImageDraw.Draw(atmosphere)
    atmosphere_draw.ellipse((30, -260, 800, 700), fill=(34, 111, 232, 72))
    atmosphere_draw.ellipse((1030, 160, 1770, 900), fill=(255, 111, 74, 28))
    hero.alpha_composite(atmosphere.filter(ImageFilter.GaussianBlur(145)))

    grid = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    grid_draw = ImageDraw.Draw(grid)
    for x in range(0, width, 80):
        grid_draw.line((x, 0, x, height), fill=(116, 161, 225, 12), width=1)
    for y in range(0, height, 80):
        grid_draw.line((0, y, width, y), fill=(116, 161, 225, 12), width=1)
    hero.alpha_composite(grid)

    icon_hero = icon.resize((370, 370), Image.Resampling.LANCZOS)
    hero.alpha_composite(icon_hero, (105, 135))

    draw = ImageDraw.Draw(hero)
    label_font = load_font("seguisb.ttf", 23)
    title_font = load_font("segoeuib.ttf", 78)
    tagline_font = load_font("seguisb.ttf", 42)
    body_font = load_font("segoeui.ttf", 25)
    chip_font = load_font("seguisb.ttf", 19)

    text_x = 560
    draw.text((text_x, 116), "WINDOWS FILE CONVERSION", font=label_font, fill=(103, 164, 255, 255))
    draw.text((text_x, 153), "ZFileConverter", font=title_font, fill=(245, 248, 255, 255))
    draw.text((text_x, 258), "Right-click. Convert. Done.", font=tagline_font, fill=(255, 255, 255, 255))
    draw.text(
        (text_x, 332),
        "Fast, private file conversion that lives in File Explorer.",
        font=body_font,
        fill=(171, 187, 210, 255),
    )

    chips = [
        ("EXPLORER-NATIVE", (61, 132, 239, 255)),
        ("DARK + LIGHT", (50, 103, 184, 255)),
        ("LOCAL BY DESIGN", (157, 77, 58, 255)),
    ]
    chip_x = text_x
    chip_y = 414
    for text, color in chips:
        box = draw.textbbox((0, 0), text, font=chip_font)
        chip_width = box[2] - box[0] + 42
        draw.rounded_rectangle(
            (chip_x, chip_y, chip_x + chip_width, chip_y + 48),
            radius=18,
            fill=color,
            outline=(140, 182, 242, 68),
            width=1,
        )
        draw.text((chip_x + 21, chip_y + 11), text, font=chip_font, fill=(242, 247, 255, 255))
        chip_x += chip_width + 14

    draw.line((text_x, 524, 1450, 524), fill=(83, 129, 194, 90), width=2)
    draw.text(
        (text_x, 548),
        "VIDEO  •  AUDIO  •  IMAGES  •  DOCUMENTS",
        font=label_font,
        fill=(137, 158, 188, 255),
    )
    return hero


def build_installer_banner(icon):
    width, height = 493, 58
    banner = vertical_gradient(width, height, (249, 251, 255, 255), (232, 239, 248, 255))
    draw = ImageDraw.Draw(banner)

    draw.rectangle((0, 0, width, 3), fill=(44, 118, 242, 255))
    draw.polygon([(405, 3), (493, 3), (493, 58), (428, 58)], fill=(9, 22, 39, 255))
    draw.polygon([(405, 3), (421, 3), (444, 58), (428, 58)], fill=(35, 94, 180, 255))
    draw.line((0, 57, width, 57), fill=(195, 208, 226, 255), width=1)

    icon_small = icon.resize((44, 44), Image.Resampling.LANCZOS)
    banner.alpha_composite(icon_small, (442, 8))
    return banner.convert("RGB")


def build_installer_dialog(icon):
    width, height = 493, 312
    dialog = vertical_gradient(width, height, (250, 252, 255, 255), (241, 246, 252, 255))
    draw = ImageDraw.Draw(dialog)

    left_panel = vertical_gradient(170, height, (7, 16, 30, 255), (16, 39, 67, 255))
    glow = Image.new("RGBA", (170, height), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((-90, 40, 250, 330), fill=(37, 112, 235, 70))
    left_panel.alpha_composite(glow.filter(ImageFilter.GaussianBlur(65)))
    dialog.alpha_composite(left_panel, (0, 0))

    grid = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    grid_draw = ImageDraw.Draw(grid)
    for x in range(10, 170, 32):
        grid_draw.line((x, 0, x, height), fill=(112, 162, 229, 28), width=1)
    for y in range(10, height, 32):
        grid_draw.line((0, y, 170, y), fill=(112, 162, 229, 28), width=1)
    dialog.alpha_composite(grid)

    draw = ImageDraw.Draw(dialog)
    draw.rectangle((0, 0, width, 4), fill=(44, 118, 242, 255))
    draw.rectangle((166, 4, 170, height), fill=(44, 118, 242, 255))
    draw.rectangle((24, 230, 68, 234), fill=(255, 143, 105, 255))

    icon_large = icon.resize((112, 112), Image.Resampling.LANCZOS)
    dialog.alpha_composite(icon_large, (29, 55))

    brand_font = load_font("seguisb.ttf", 16)
    tagline_font = load_font("seguisb.ttf", 11)
    draw.text((23, 181), "ZFILECONVERTER", font=brand_font, fill=(239, 246, 255, 255))
    draw.text((23, 244), "RIGHT-CLICK.", font=tagline_font, fill=(167, 188, 216, 255))
    draw.text((23, 261), "CONVERT. DONE.", font=tagline_font, fill=(167, 188, 216, 255))

    return dialog.convert("RGB")


SVG = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" role="img" aria-labelledby="title desc">
  <title id="title">ZFileConverter icon</title>
  <desc id="desc">A folded document with a blue Z-shaped conversion arrow on a midnight rounded square.</desc>
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#091222"/>
      <stop offset="1" stop-color="#112844"/>
    </linearGradient>
    <radialGradient id="glow" cx="65%" cy="35%" r="55%">
      <stop offset="0" stop-color="#236fe8" stop-opacity=".34"/>
      <stop offset="1" stop-color="#236fe8" stop-opacity="0"/>
    </radialGradient>
    <filter id="shadow" x="-30%" y="-30%" width="160%" height="170%">
      <feDropShadow dx="0" dy="24" stdDeviation="25" flood-color="#000" flood-opacity=".45"/>
    </filter>
  </defs>
  <rect x="56" y="56" width="912" height="912" rx="220" fill="url(#bg)"/>
  <rect x="56" y="56" width="912" height="912" rx="220" fill="url(#glow)"/>
  <rect x="72" y="72" width="880" height="880" rx="205" fill="none" stroke="#71a4f0" stroke-opacity=".28" stroke-width="8"/>
  <g filter="url(#shadow)">
    <path d="M254 278h312l118 118v374H254z" fill="#2b64b5"/>
    <path d="M566 278l118 118H566z" fill="#5f9cff"/>
    <path d="M330 220h280l142 142v440H330z" fill="#f0f6ff" stroke="#fff" stroke-width="18" stroke-linejoin="round"/>
    <path d="M610 220l142 142H610z" fill="#ff8f69"/>
    <path d="M610 222v140h140" fill="none" stroke="#fff" stroke-opacity=".86" stroke-width="14" stroke-linejoin="round"/>
    <path d="M390 410h240v64L486 610h146v-42l92 78-92 78v-42H390v-64l144-144H390z" fill="#2c76f2"/>
    <path d="M405 426h205" stroke="#76aeff" stroke-opacity=".7" stroke-width="8"/>
    <path d="M408 666h202" stroke="#1452bd" stroke-opacity=".5" stroke-width="7"/>
  </g>
</svg>
"""


def write_ico(icon, path, sizes=ICO_SIZES):
    path.parent.mkdir(parents=True, exist_ok=True)
    # Use DIB/BMP-backed frames rather than PNG-compressed frames. Explorer's
    # System.Drawing.Icon path corrupts PNG-compressed multi-frame ICO data.
    icon.save(
        path,
        format="ICO",
        sizes=[(size, size) for size in sizes],
        bitmap_format="bmp",
    )


def main():
    icon = build_icon()

    shared = ROOT / "Resources" / "Icons"
    docs = ROOT / "docs" / "brand"
    shared.mkdir(parents=True, exist_ok=True)
    docs.mkdir(parents=True, exist_ok=True)

    write_ico(icon, shared / "ApplicationIcon.ico")
    write_ico(icon, shared / "ApplicationIcon-256x256.ico")
    app_resources = ROOT / "Application" / "FileConverter" / "Resources"
    write_ico(icon, app_resources / "ApplicationIcon.ico")
    icon.resize((256, 256), Image.Resampling.LANCZOS).save(app_resources / "ApplicationIcon.png")
    write_ico(icon, ROOT / "Application" / "FileConverterExtension" / "Resources" / "ApplicationIcon.ico")

    icon.resize((48, 48), Image.Resampling.LANCZOS).save(shared / "ApplicationIcon.png")
    (shared / "ApplicationIcon.svg").write_text(SVG, encoding="utf-8")

    icon.resize((256, 256), Image.Resampling.LANCZOS).save(docs / "zfileconverter-icon.png")
    icon.save(docs / "zfileconverter-icon-1024.png")
    (docs / "zfileconverter-icon.svg").write_text(SVG, encoding="utf-8")

    images = ROOT / "docs" / "images"
    images.mkdir(parents=True, exist_ok=True)
    build_hero(icon).convert("RGB").save(images / "zfileconverter-hero.png", quality=94)

    installer = ROOT / "Resources" / "Installer"
    installer.mkdir(parents=True, exist_ok=True)
    build_installer_banner(icon).save(installer / "Banner.bmp")
    build_installer_dialog(icon).save(installer / "UI.bmp")

    print("Generated ZFileConverter icon assets.")


if __name__ == "__main__":
    main()
