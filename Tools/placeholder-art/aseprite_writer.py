"""Escritor mínimo de archivos .aseprite (RGBA, capas normales, tags, paleta).
Sigue la especificación oficial:
https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
"""
import struct
import zlib


def _string(s):
    b = s.encode("utf-8")
    return struct.pack("<H", len(b)) + b


def _chunk(ctype, data):
    return struct.pack("<IH", len(data) + 6, ctype) + data


def write_aseprite(path, width, height, layers, frames, tags, palette=None):
    """
    layers: lista de nombres (de abajo hacia arriba).
    frames: lista de dicts {"duration": ms, "cels": {layer_index: PIL.Image RGBA (w×h del canvas)}}
    tags:   lista de (nombre, desde, hasta)
    palette: lista de (r, g, b, a) opcional, guardada como paleta del archivo.
    """
    body = b""
    for fi, frame in enumerate(frames):
        chunks = []
        if fi == 0:
            # Perfil de color sRGB
            chunks.append(_chunk(0x2007, struct.pack("<HHI", 1, 0, 0) + b"\0" * 8))
            if palette:
                pal = struct.pack("<III", len(palette), 0, len(palette) - 1) + b"\0" * 8
                for (r, g, b, a) in palette:
                    pal += struct.pack("<HBBBB", 0, r, g, b, a)
                chunks.append(_chunk(0x2019, pal))
            for name in layers:
                data = struct.pack("<HHHHHHB", 3, 0, 0, 0, 0, 0, 255) + b"\0" * 3 + _string(name)
                chunks.append(_chunk(0x2004, data))
            if tags:
                tdata = struct.pack("<H", len(tags)) + b"\0" * 8
                for (name, start, end) in tags:
                    tdata += struct.pack("<HHBH", start, end, 0, 0) + b"\0" * 6
                    tdata += b"\0\0\0" + b"\0" + _string(name)
                chunks.append(_chunk(0x2018, tdata))
        for li in sorted(frame["cels"]):
            img = frame["cels"][li]
            bbox = img.getbbox()
            if bbox is None:
                continue
            crop = img.crop(bbox)
            w, h = crop.size
            pixels = zlib.compress(crop.tobytes(), 9)
            data = struct.pack("<HhhBHh", li, bbox[0], bbox[1], 255, 2, 0) + b"\0" * 5
            data += struct.pack("<HH", w, h) + pixels
            chunks.append(_chunk(0x2005, data))

        payload = b"".join(chunks)
        n = len(chunks)
        header = struct.pack("<IHHHBBI", 16 + len(payload), 0xF1FA, min(n, 0xFFFF), frame["duration"], 0, 0, n)
        body += header + payload

    ncolors = len(palette) if palette else 0
    header = struct.pack(
        "<IHHHHHIHIIB3sHBBhhHH",
        128 + len(body), 0xA5E0, len(frames), width, height, 32, 1, 100, 0, 0, 0, b"\0\0\0",
        ncolors, 1, 1, 0, 0, 16, 16,
    )
    header += b"\0" * (128 - len(header))
    assert len(header) == 128
    with open(path, "wb") as f:
        f.write(header + body)
