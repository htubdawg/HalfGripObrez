#!/usr/bin/env python3
"""Deterministic Obrez pixel art. Python 3 standard library only; no source images."""
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
PALETTE = {
    'edge': (26, 24, 25, 255), 'steel': (63, 69, 70, 255),
    'light': (113, 122, 119, 255), 'shine': (174, 178, 157, 255),
    'wood': (126, 79, 49, 255), 'grain': (157, 102, 67, 255),
    'darkwood': (89, 55, 36, 255), 'brass': (163, 121, 52, 255),
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

    def polygon(self, points, color):
        # Pixel-centre scan conversion keeps edges hard and deterministic.
        for y in range(self.height):
            intersections = []
            for (x0, y0), (x1, y1) in zip(points, points[1:] + points[:1]):
                if (y0 <= y + 0.5 < y1) or (y1 <= y + 0.5 < y0):
                    intersections.append(x0 + (y + 0.5 - y0) * (x1 - x0) / (y1 - y0))
            intersections.sort()
            for left, right in zip(intersections[::2], intersections[1::2]):
                for x in range(max(0, int(left)), min(self.width, int(right) + 1)):
                    if left <= x + 0.5 < right:
                        self.rect(x, y, 1, 1, color)

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
    c = Canvas(128, 48)
    # Supplied reference, muzzle left: the wood is most of the silhouette.
    # A thick fore-end sweeps down through the wrist and flares into the cut butt.
    c.rect(0, 11, 81, 5, 'edge')
    c.rect(1, 12, 78, 2, 'steel')
    c.rect(3, 12, 70, 1, 'light')
    c.rect(0, 9, 5, 7, 'edge')
    c.rect(1, 10, 3, 4, 'steel')
    c.rect(1, 5, 3, 6, 'edge')
    c.rect(2, 5, 1, 5, 'light')  # Reference's front sight.
    c.rect(28, 8, 16, 4, 'edge')
    c.rect(30, 7, 3, 2, 'steel')
    c.rect(32, 9, 9, 1, 'light')  # Leaf rear sight, ahead of the action.
    c.rect(47, 9, 7, 7, 'edge')
    c.rect(48, 10, 5, 4, 'steel')
    c.rect(48, 10, 4, 1, 'light')
    c.rect(54, 10, 31, 7, 'edge')
    c.rect(55, 11, 28, 4, 'steel')
    c.rect(57, 12, 18, 3, 'edge')  # Open receiver, covered by moving bolt when closed.
    c.rect(56, 15, 26, 1, 'light')
    c.polygon([(14,16),(75,16),(85,17),(94,20),(107,25),(116,26),
               (126,24),(128,26),(128,37),(119,35),(108,31),(98,29),
               (88,27),(74,26),(55,25),(32,24),(14,22)], 'edge')
    c.polygon([(15,17),(75,17),(84,18),(93,21),(106,26),(116,27),
               (126,25),(127,27),(127,35),(119,33),(108,29),(98,27),
               (88,25),(74,24),(55,23),(32,22),(15,21)], 'wood')
    c.polygon([(16,17),(74,17),(84,18),(93,21),(105,26),(96,25),
               (83,20),(73,19),(16,19)], 'grain')
    c.polygon([(16,21),(55,23),(75,24),(88,25),(98,27),(108,29),
               (121,34),(122,32),(108,27),(98,25),(88,23),(75,23),(55,22)], 'darkwood')
    # Long shallow finger groove and a receiver screw, both visible in the photograph.
    c.rect(20, 20, 45, 1, 'darkwood')
    c.rect(22, 21, 41, 1, 'grain')
    c.rect(71, 20, 3, 3, 'edge')
    c.rect(72, 20, 1, 1, 'light')
    c.rect(101, 26, 9, 1, 'grain')
    c.rect(112, 30, 8, 1, 'grain')
    c.polygon([(125,26),(128,27),(128,37),(125,35)], 'darkwood')
    c.rect(126, 28, 1, 6, 'grain')  # Cut end grain, not a separate pistol grip.
    # Thin, sloped integral magazine under the stock; no detachable box.
    c.polygon([(57,26),(82,28),(82,34),(76,34),(64,31)], 'edge')
    c.polygon([(59,27),(80,29),(80,32),(76,32),(65,30)], 'steel')
    c.rect(67, 30, 9, 1, 'light')
    # Rounded trigger guard follows the sloping wrist.
    c.polygon([(82,28),(92,30),(97,34),(96,38),(89,38),(82,35)], 'edge')
    c.polygon([(84,30),(91,32),(94,34),(94,36),(89,36),(84,34)], (0,0,0,0))
    c.rect(86, 29, 2, 5, 'steel')
    if with_bolt:
        c.paste(bolt_art(), 51, 8)
    return c


def bolt_art():
    # Same side and travel as 0.2.1; origin is now body pixel (51, 8).
    c = Canvas(36, 20)
    c.rect(0, 2, 28, 6, 'edge')
    c.rect(1, 2, 26, 4, 'steel')
    c.rect(1, 2, 24, 1, 'shine')
    c.rect(3, 4, 21, 1, 'light')
    c.rect(27, 1, 5, 8, 'edge')
    c.rect(28, 2, 3, 5, 'light')
    c.rect(18, 4, 3, 11, 'edge')
    c.rect(19, 4, 1, 11, 'light')
    c.rect(16, 13, 7, 6, 'edge')
    c.rect(17, 13, 5, 4, 'steel')
    c.rect(17, 13, 3, 2, 'shine')
    return c


def garage_icon():
    # Independently drawn 64px inventory art: broad shapes and seven colours.
    # No cockpit resizing, wood-grain texture, or tiny bolt/receiver details.
    c = Canvas(64, 24)
    c.rect(1, 5, 40, 3, 'edge')
    c.rect(2, 5, 38, 1, 'light')
    c.rect(1, 2, 2, 4, 'edge')
    c.rect(15, 3, 6, 2, 'edge')
    c.rect(25, 4, 17, 4, 'steel')
    c.rect(26, 4, 14, 1, 'shine')
    c.polygon([(8,8),(40,8),(47,10),(54,14),(62,12),(63,14),
               (63,22),(57,20),(49,16),(41,14),(27,12),(8,11)], 'edge')
    c.polygon([(9,9),(40,9),(46,11),(54,15),(61,14),(62,20),
               (57,18),(49,14),(41,12),(27,11),(9,10)], 'wood')
    c.rect(10, 9, 26, 1, 'grain')
    c.polygon([(28,12),(40,14),(40,17),(35,16),(31,15)], 'steel')
    c.rect(41, 14, 7, 5, 'edge')
    c.rect(42, 15, 4, 2, (0,0,0,0))
    c.rect(35, 6, 1, 6, 'light')
    c.rect(34, 11, 3, 2, 'steel')
    return c


def cartridge_art():
    # Long rifle cartridge: pointed copper projectile, narrow neck, shoulder,
    # long brass case, and a restrained rim (not a wide pedestal).
    c = Canvas(8, 28)
    c.polygon([(3,0),(5,4),(5,8),(2,8),(2,4)], 'edge')
    c.rect(3, 1, 1, 2, 'copper')
    c.rect(3, 3, 2, 5, 'copper')
    c.rect(3, 4, 1, 3, 'grain')
    c.rect(2, 8, 4, 3, 'edge')
    c.rect(3, 8, 2, 3, 'brass')
    c.polygon([(2,10),(6,10),(7,13),(7,25),(1,25),(1,13)], 'edge')
    c.polygon([(3,11),(5,11),(6,14),(6,24),(2,24),(2,14)], 'brass')
    c.rect(3, 13, 1, 11, 'gold')
    c.rect(5, 15, 1, 8, 'wood')
    c.rect(0, 25, 8, 3, 'edge')
    c.rect(1, 25, 6, 2, 'brass')
    c.rect(2, 25, 4, 1, 'gold')
    return c


def held_art():
    # A dedicated close-view texture with face highlights, sharing the photo's profile.
    c = rifle(True)
    c.rect(16, 18, 36, 1, 'grain')
    c.rect(51, 18, 15, 1, 'grain')
    c.rect(3, 13, 23, 1, 'light')
    c.rect(112, 33, 8, 1, 'darkwood')
    return c


def generate():
    sprites = ROOT / 'Obrez' / 'sprites'
    body, held, bolt = rifle(), held_art(), bolt_art()
    body.save(sprites / 'obrez_body.png')
    held.save(sprites / 'obrez_held.png')
    bolt.save(sprites / 'obrez_bolt.png')
    cartridge = cartridge_art()
    cartridge.save(sprites / 'obrez_round.png')
    for obsolete in ('obrez_pack.png', 'obrez_pack_empty.png'):
        (sprites / obsolete).unlink(missing_ok=True)
    Canvas(2, 2).save(sprites / 'clear.png')
    icon = garage_icon()
    icon.save(sprites / 'obrez_icon.png')
    square = Canvas(128, 128)
    square.rect(0, 0, 128, 128, (27, 34, 36, 255))
    square.rect(5, 5, 118, 118, (45, 58, 57, 255))
    square.paste(icon, 0, 36, 2)
    square.save(ROOT / 'Obrez' / 'icon.png')
    preview = Canvas(896, 560)
    preview.rect(0, 0, 896, 560, (32, 40, 42, 255))
    preview.paste(rifle(True), 32, 10, 6)
    # Inventory is separately drawn; show it at its intended 160x60 UI footprint (2x pixel preview).
    preview.paste(icon, 40, 330, 4)
    preview.paste(cartridge, 400, 335, 5)
    preview.save(ROOT / 'docs' / 'artwork.png')


if __name__ == '__main__':
    generate()
    print('Generated photo-based cockpit/held art, independent garage icon, and rimmed cartridge.')
