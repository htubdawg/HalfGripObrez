#!/usr/bin/env python3
"""Deterministic Obrez pixel art. Python 3 standard library only; no source images."""
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
PALETTE = {
    'edge': (26, 24, 25, 255), 'steel': (63, 69, 70, 255),
    'light': (113, 122, 119, 255), 'shine': (174, 178, 157, 255),
    'wood': (107, 61, 37, 255), 'grain': (143, 86, 45, 255),
    'darkwood': (66, 40, 29, 255), 'brass': (163, 121, 52, 255),
    'gold': (217, 175, 87, 255), 'copper': (155, 82, 47, 255),
}


class Canvas:
    def __init__(self, width, height):
        self.width, self.height = width, height
        self.pixels = [(0, 0, 0, 0)] * (width * height)

    def rect(self, x, y, width, height, color):
        rgba = PALETTE.get(color, color)
        for row in range(max(0, y), min(self.height, y + height)):
            for col in range(max(0, x), min(self.width, x + width)):
                self.pixels[row * self.width + col] = rgba

    def paste(self, source, x, y, scale=1):
        for row in range(source.height):
            for col in range(source.width):
                color = source.pixels[row * source.width + col]
                if color[3]:
                    self.rect(x + col * scale, y + row * scale, scale, scale, color)

    def save(self, path):
        def chunk(kind, data):
            return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
        raw = b''.join(b'\0' + bytes(c for pixel in self.pixels[y*self.width:(y+1)*self.width] for c in pixel)
                       for y in range(self.height))
        png = b'\x89PNG\r\n\x1a\n'
        png += chunk(b'IHDR', struct.pack('>IIBBBBB', self.width, self.height, 8, 6, 0, 0, 0))
        png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(png)


def rifle(with_bolt=False):
    c = Canvas(128, 40)
    # Shortened M1891/30: slim barrel, hex receiver ring, exposed bolt cylinder,
    # round cocking piece, fixed angled magazine, and a continuous chopped rifle wrist.
    c.rect(0, 11, 76, 5, 'edge')
    c.rect(1, 12, 72, 2, 'steel')
    c.rect(2, 12, 66, 1, 'light')
    c.rect(0, 12, 2, 3, 'shine')
    c.rect(9, 16, 76, 7, 'edge')
    c.rect(11, 17, 72, 4, 'wood')
    c.rect(11, 17, 68, 1, 'grain')
    c.rect(14, 21, 66, 1, 'darkwood')
    for x in (15, 33, 48, 65):
        c.rect(x, 19, 10, 1, 'grain')
    c.rect(23, 15, 3, 8, 'steel')  # Remaining barrel band.
    c.rect(24, 15, 1, 7, 'light')
    c.rect(57, 8, 13, 4, 'edge')  # Leaf rear sight, no scope rail.
    c.rect(58, 8, 10, 1, 'light')
    c.rect(69, 10, 8, 7, 'edge')  # Faceted receiver ring.
    c.rect(70, 10, 6, 1, 'light')
    c.rect(70, 11, 6, 4, 'steel')
    c.rect(72, 11, 3, 1, 'shine')
    c.rect(77, 10, 26, 7, 'edge')
    c.rect(77, 11, 25, 4, 'steel')
    c.rect(78, 10, 22, 1, 'light')
    c.rect(80, 12, 13, 3, 'edge')  # Small open receiver/loading port.
    c.rect(80, 15, 15, 1, 'light')
    # Stock continues under the receiver into a shallow, tapered cut wrist.
    for y in range(17, 30):
        left = 80 + max(0, y - 22)
        right = 122 + min(3, (y - 17) // 4)
        c.rect(left, y, right - left, 1, 'edge')
        c.rect(left + 1, y, right - left - 2, 1, 'wood')
    c.rect(83, 17, 35, 1, 'grain')
    c.rect(95, 20, 25, 1, 'grain')
    c.rect(109, 25, 12, 1, 'grain')
    c.rect(122, 20, 3, 10, 'darkwood')
    c.rect(122, 21, 1, 7, 'grain')  # Exposed cut end, no separate pistol grip.
    # Mosin's integral magazine slopes back at the front, unlike a detachable box.
    for y in range(23, 32):
        x = 74 + (y - 23) // 2
        c.rect(x, y, 95 - x, 1, 'edge')
        if y < 30:
            c.rect(x + 2, y, 92 - x, 1, 'steel')
    c.rect(80, 30, 14, 1, 'light')
    c.rect(95, 23, 15, 9, 'edge')
    c.rect(97, 24, 11, 6, (0, 0, 0, 0))
    c.rect(99, 23, 2, 5, 'steel')
    if with_bolt:
        c.paste(bolt_art(), 78, 8)
    return c


def bolt_art():
    # Complete moving bolt, not just its handle. Origin corresponds to body pixel (78, 8).
    c = Canvas(36, 20)
    c.rect(0, 2, 27, 6, 'edge')
    c.rect(1, 2, 25, 4, 'steel')
    c.rect(1, 2, 24, 1, 'shine')
    c.rect(3, 4, 21, 1, 'light')
    c.rect(26, 1, 5, 8, 'edge')
    c.rect(27, 2, 3, 5, 'light')
    c.rect(18, 4, 3, 11, 'edge')
    c.rect(19, 4, 1, 11, 'light')
    c.rect(16, 13, 7, 6, 'edge')
    c.rect(17, 13, 5, 4, 'steel')
    c.rect(17, 13, 3, 2, 'shine')
    return c


def generate():
    sprites = ROOT / 'Obrez' / 'sprites'
    body, held = rifle(), rifle(True)
    body.save(sprites / 'obrez_body.png')
    held.save(sprites / 'obrez_held.png')
    bolt = bolt_art()
    bolt.save(sprites / 'obrez_bolt.png')
    cartridge = Canvas(6, 16)
    cartridge.rect(2, 0, 1, 3, 'copper')
    cartridge.rect(2, 3, 2, 3, 'copper')
    cartridge.rect(1, 6, 4, 8, 'brass')
    cartridge.rect(2, 6, 1, 8, 'gold')
    cartridge.rect(0, 14, 6, 2, 'brass')  # Prominent rim of 7.62x54R.
    cartridge.rect(1, 14, 4, 1, 'gold')
    cartridge.save(sprites / 'obrez_round.png')
    for obsolete in ('obrez_pack.png', 'obrez_pack_empty.png'):
        (sprites / obsolete).unlink(missing_ok=True)
    Canvas(2, 2).save(sprites / 'clear.png')
    icon = Canvas(160, 80)
    icon.paste(held, 16, 20)
    icon.save(sprites / 'obrez_icon.png')
    square = Canvas(160, 160)
    square.rect(0, 0, 160, 160, (27, 34, 36, 255))
    square.rect(5, 5, 150, 150, (45, 58, 57, 255))
    square.paste(held, 16, 58)
    square.save(ROOT / 'Obrez' / 'icon.png')
    preview = Canvas(896, 288)
    preview.rect(0, 0, 896, 288, (32, 40, 42, 255))
    preview.paste(held, 32, 16, 6)
    # Same physical ratio as the runtime: cartridge is 12% of rifle length.
    preview.paste(cartridge, 830, 80, 5)
    preview.save(ROOT / 'docs' / 'artwork.png')


if __name__ == '__main__':
    generate()
    print('Generated Mosin artwork and individual rimmed cartridges.')
