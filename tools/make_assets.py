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
    c = Canvas(96, 32)
    # Left is the cut muzzle. A short fore-end and cut wrist replace a full rifle stock.
    c.rect(0, 8, 45, 7, 'edge')
    c.rect(1, 9, 43, 4, 'steel')
    c.rect(1, 9, 41, 1, 'light')
    c.rect(0, 10, 2, 3, 'shine')
    c.rect(10, 13, 58, 8, 'edge')
    c.rect(12, 14, 54, 5, 'wood')
    c.rect(13, 14, 49, 1, 'grain')
    c.rect(15, 18, 48, 1, 'darkwood')
    for x in (19, 32, 46, 57):
        c.rect(x, 16, 7, 1, 'grain')
    c.rect(24, 13, 3, 7, 'steel')  # Barrel band.
    c.rect(39, 7, 35, 9, 'edge')
    c.rect(40, 8, 32, 6, 'steel')
    c.rect(40, 8, 30, 1, 'light')
    c.rect(46, 10, 14, 3, 'edge')  # Receiver opening.
    c.rect(48, 12, 10, 1, 'light')
    c.rect(71, 9, 5, 5, 'light')  # Mosin cocking knob.
    c.rect(35, 5, 5, 3, 'edge')
    c.rect(36, 5, 3, 1, 'light')
    # Short fixed magazine silhouette and trigger guard.
    c.rect(48, 20, 14, 5, 'edge')
    c.rect(49, 20, 11, 3, 'steel')
    c.rect(63, 19, 14, 7, 'edge')
    c.rect(65, 20, 10, 4, (0, 0, 0, 0))
    c.rect(67, 19, 2, 4, 'steel')
    for row in range(14, 29):
        x = 72 + (row - 14) // 2
        c.rect(x, row, 14, 1, 'edge')
        c.rect(x + 1, row, 11, 1, 'wood')
        if row % 4 == 0:
            c.rect(x + 2, row, 7, 1, 'grain')
    c.rect(80, 28, 13, 2, 'darkwood')
    c.rect(91, 24, 3, 5, 'grain')  # Rough-cut end grain.
    if with_bolt:
        c.rect(62, 9, 8, 3, 'shine')
        c.rect(65, 11, 2, 7, 'steel')
        c.rect(63, 17, 6, 4, 'edge')
        c.rect(64, 17, 4, 2, 'light')
    return c


def generate():
    sprites = ROOT / 'Obrez' / 'sprites'
    body, held = rifle(), rifle(True)
    body.save(sprites / 'obrez_body.png')
    held.save(sprites / 'obrez_held.png')
    bolt = Canvas(12, 14)
    bolt.rect(1, 0, 9, 3, 'edge')
    bolt.rect(2, 0, 7, 2, 'shine')
    bolt.rect(5, 2, 2, 9, 'steel')
    bolt.rect(3, 9, 6, 5, 'edge')
    bolt.rect(4, 9, 4, 2, 'light')
    bolt.save(sprites / 'obrez_bolt.png')
    for empty in (False, True):
        pack = Canvas(24, 20)
        pack.rect(1, 14, 22, 5, 'edge')
        pack.rect(2, 15, 20, 2, 'light')
        for x in range(2, 22, 4):
            pack.rect(x, 10 if empty else 3, 3, 5 if empty else 12, 'steel' if empty else 'brass')
            if not empty:
                pack.rect(x + 1, 1, 1, 3, 'copper')
                pack.rect(x + 1, 5, 1, 8, 'gold')
        pack.save(sprites / ('obrez_pack_empty.png' if empty else 'obrez_pack.png'))
    Canvas(2, 2).save(sprites / 'clear.png')
    icon = Canvas(128, 64)
    icon.paste(held, 16, 16)
    icon.save(sprites / 'obrez_icon.png')
    # Installer icon has its own square canvas.
    square = Canvas(128, 128)
    square.rect(0, 0, 128, 128, (27, 34, 36, 255))
    square.rect(5, 5, 118, 118, (45, 58, 57, 255))
    square.paste(held, 16, 46)
    square.save(ROOT / 'Obrez' / 'icon.png')
    preview = Canvas(768, 256)
    preview.rect(0, 0, 768, 256, (32, 40, 42, 255))
    preview.paste(held, 48, 24, 7)
    preview.save(ROOT / 'docs' / 'artwork.png')


if __name__ == '__main__':
    generate()
    print('Generated Obrez sprites, installer icon and artwork preview.')
