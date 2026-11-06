#!/usr/bin/env python3
"""Deterministic Obrez pixel art. Python 3 standard library only; no source images."""
from pathlib import Path
import struct
import zlib
import math

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


def curve_path(start, segments):
    points = [start]
    current = start
    for segment in segments:
        if len(segment) == 2:
            points.append(segment)
            current = segment
            continue
        a, b, end = segment
        for step in range(1, 17):
            t = step / 16
            q = 1 - t
            points.append((q*q*q*current[0] + 3*q*q*t*a[0] + 3*q*t*t*b[0] + t*t*t*end[0],
                           q*q*q*current[1] + 3*q*q*t*a[1] + 3*q*t*t*b[1] + t*t*t*end[1]))
        current = end
    return points


def walnut(canvas, outline, close_view=False):
    mask = Canvas(canvas.width, canvas.height)
    mask.polygon(outline, (255,255,255,255))
    shades = [(74,49,34,255),(90,58,38,255),(108,70,44,255),(123,81,50,255),
              (140,94,58,255),(155,106,67,255),(169,119,77,255),(181,132,90,255)]
    for x in range(canvas.width):
        ys = [y for y in range(canvas.height) if mask.pixels[y*canvas.width+x][3]]
        if not ys:
            continue
        top, bottom = min(ys), max(ys)
        for y in ys:
            edge = any(xx < 0 or yy < 0 or xx >= canvas.width or yy >= canvas.height or
                       not mask.pixels[yy*canvas.width+xx][3] for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
            if edge:
                color = shades[0 if y > top+2 else 2]
            else:
                depth = (y-top)/max(1,bottom-top)
                grain = math.sin(y*1.15 + math.sin(x*.05)*1.2)
                clustered = ((x//3 * 17 + y//2 * 11) % 7 - 3) * .10
                value = 4.9 - depth*2.2 + grain*.22 + clustered*.12 + (.25 if close_view else 0)
                color = shades[max(1,min(7,round(value)))]
            canvas.rect(x,y,1,1,color)


def rifle(with_bolt=False, close_view=False):
    c = Canvas(160,48)
    # Photo trace in proportions: only the first 18 pixels are bare muzzle;
    # the continuous wooden fore-end is deep, and its wrist curves into the cut butt.
    c.polygon([(0,7),(68,7),(73,6),(112,6),(115,9),(113,12),(67,11),(0,10)], 'edge')
    c.rect(2,7,65,1,(111,114,109,255))
    c.rect(2,8,67,1,(61,67,65,255))
    c.rect(0,6,5,5,(45,49,48,255))
    c.rect(0,7,1,3,'edge')
    c.rect(1,1,2,6,(43,47,46,255))
    c.rect(2,2,1,4,(130,133,123,255))
    c.polygon([(35,5),(48,5),(52,7),(35,7)],(37,41,40,255))
    c.rect(38,3,2,3,(39,43,42,255))
    c.rect(40,5,9,1,(104,109,101,255))
    c.rect(67,7,46,4,(56,61,59,255))
    c.rect(71,6,38,1,(104,110,102,255))
    c.rect(79,7,24,4,(26,30,30,255))
    c.rect(78,11,29,1,(99,105,98,255))
    stock = curve_path((19,10), [((50,9),(83,10),(105,10)),
        ((115,10),(121,15),(130,19)), ((138,23),(145,21),(157,18)),
        ((159,18),(160,19),(160,21)), (160,38),
        ((147,35),(136,30),(128,28)), ((119,25),(112,24),(104,24)),
        ((78,23),(46,21),(19,17))])
    walnut(c,stock,close_view)
    # Recessed finger groove: a curved one-pixel shadow with a soft amber lip.
    c.polygon(curve_path((25,15), [((40,15),(58,16),(82,16)), (82,18),
        ((58,18),(40,17),(25,16))]), (105,68,43,255))
    c.polygon(curve_path((28,16), [((46,17),(65,18),(80,17)),
        ((64,19),(44,18),(28,17))]), (152,102,62,255))
    c.rect(91,15,3,3,(54,45,35,255))
    c.rect(92,15,1,1,(139,132,111,255))
    # Cut end is a narrow darker edge, not a black triangular stock silhouette.
    c.polygon([(157,19),(160,20),(160,38),(157,37)],(99,65,43,255))
    c.rect(158,23,1,10,(139,94,62,255))
    # Shallow tapered fixed magazine and genuinely rounded trigger guard.
    c.polygon(curve_path((71,23), [(104,25),(104,31),
        ((92,31),(82,28),(76,26))]),(31,36,35,255))
    c.polygon([(75,24),(102,26),(102,29),(91,28),(80,26)],(62,67,64,255))
    c.rect(87,28,10,1,(87,91,82,255))
    c.polygon(curve_path((106,24), [((115,24),(123,26),(124,29)),
        ((124,35),(115,35),(109,32)), ((107,30),(106,26),(106,24))]),(52,58,54,255))
    c.polygon(curve_path((109,26), [((115,26),(121,27),(121,30)),
        ((121,33),(115,33),(111,31)), ((110,30),(109,28),(109,26))]),(0,0,0,0))
    c.polygon(curve_path((113,25), [((111,28),(112,30),(114,31)),(115,30),
        ((114,28),(114,26),(115,25))]),(58,62,58,255))
    if with_bolt:
        c.paste(bolt_art(),69,3)
    return c


def bolt_art():
    c = Canvas(48,22)
    c.polygon([(0,4),(3,2),(37,2),(39,4),(38,8),(2,8),(0,6)],(37,42,41,255))
    c.rect(3,3,32,1,(196,199,182,255))
    c.rect(2,4,35,2,(139,148,138,255))
    c.rect(3,6,33,1,(83,93,88,255))
    c.polygon([(38,1),(43,1),(45,3),(45,9),(43,11),(38,10)],(58,65,61,255))
    c.rect(39,2,4,6,(161,169,154,255))
    c.rect(43,3,1,6,(95,106,98,255))
    # Keep the accepted bolt side. Stem and ball are round, not a squared lever.
    c.polygon([(25,5),(28,5),(29,14),(27,17),(24,16)],(72,83,78,255))
    c.rect(26,7,1,8,(172,180,162,255))
    c.polygon([(24,14),(28,14),(31,16),(31,19),(28,21),(24,20),(22,18),(22,16)],(49,59,55,255))
    c.polygon([(24,15),(28,15),(29,17),(27,19),(24,18),(23,17)],(138,151,134,255))
    c.rect(25,15,2,1,(205,208,185,255))
    return c


def garage_icon():
    # Independent coarse inventory drawing: smooth silhouette, only seven colours.
    c = Canvas(64,24)
    c.rect(1,6,43,2,(47,51,48,255))
    c.rect(2,6,25,1,(125,131,115,255))
    c.rect(1,3,1,4,(47,51,48,255))
    c.rect(15,4,5,2,(47,51,48,255))
    c.polygon(curve_path((8,8), [((22,7),(34,8),(42,8)),
        ((47,8),(49,11),(53,12)), ((57,14),(60,12),(63,11)),(64,18),
        ((57,17),(54,15),(50,14)),((39,12),(23,12),(8,10))]),(106,70,43,255))
    c.polygon(curve_path((9,8), [((23,8),(34,9),(42,9)),
        ((47,9),(49,12),(53,13)), ((46,12),(40,10),(32,10)),(9,9)]),(152,105,65,255))
    c.polygon([(29,12),(41,13),(41,16),(35,15),(31,14)],(47,51,48,255))
    c.polygon([(43,13),(48,14),(49,17),(46,18),(43,16)],(47,51,48,255))
    c.rect(45,15,2,1,(0,0,0,0))
    c.rect(28,5,17,2,(125,131,115,255))
    c.rect(29,5,14,1,(191,195,165,255))
    c.rect(38,7,1,5,(125,131,115,255))
    c.rect(37,11,3,2,(47,51,48,255))
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
    return rifle(True,close_view=True)


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
    preview = Canvas(1088, 560)
    preview.rect(0, 0, 1088, 560, (32, 40, 42, 255))
    preview.paste(rifle(True), 32, 10, 6)
    # Independent inventory art is enlarged 4x here for inspection; runtime UI sizing is separate.
    preview.paste(icon, 40, 330, 4)
    preview.paste(cartridge, 400, 335, 5)
    preview.save(ROOT / 'docs' / 'artwork.png')


if __name__ == '__main__':
    generate()
    print('Generated photo-based cockpit/held art, independent garage icon, and rimmed cartridge.')
