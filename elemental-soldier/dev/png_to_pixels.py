"""
Converte sprites PNG para arrays de pixels procedurais (pixel art).
Reduz para tamanho e paleta de cores limitada, ideal para desenho procedural.

Uso: python tools/png_to_pixels.py
"""
from PIL import Image
import os
import json

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(SCRIPT_DIR)
OUTPUT_DIR = os.path.join(PROJECT_DIR, "data", "sprites")

SPRITE_FILES = {
    "idle": "idle.png",
    "run": "run.png",
    "jump": "jump.png",
    "fall": "fall.png",
    "attack": "attack.png",
    "death": "death.png",
}

# Tamanho alvo para pixel art
TARGET_HEIGHT = 64


def detect_frames(img):
    """Detecta frames pelas colunas vazias."""
    w, h = img.size
    pixels = img.load()
    frame_ranges = []
    in_frame = False
    start_x = 0

    for x in range(w):
        col_empty = all(pixels[x, y][3] == 0 for y in range(h))
        if not col_empty and not in_frame:
            start_x = x
            in_frame = True
        elif col_empty and in_frame:
            frame_ranges.append((start_x, x))
            in_frame = False
    if in_frame:
        frame_ranges.append((start_x, w))
    return frame_ranges


def reduce_to_pixel_art(img, target_h):
    """Reduz imagem para pixel art com paleta limitada."""
    w, h = img.size
    scale = target_h / h
    new_w = max(1, int(w * scale))
    new_h = target_h

    # Primeiro reduz
    small = img.resize((new_w, new_h), Image.NEAREST)

    # Depois quantiza para poucas cores
    quantized = small.quantize(colors=32, method=Image.Quantize.FASTOCTREE)
    return quantized.convert("RGBA")


def extract_pixels(img):
    """Extrai pixels nao-transparentes."""
    w, h = img.size
    pixels = img.load()
    result = []
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a > 10:
                result.append([x, y, r, g, b, a])
    return result, w, h


def process_image(path, target_h):
    """Processa uma imagem: detecta frames, reduz, extrai pixels."""
    img = Image.open(path).convert("RGBA")
    frame_ranges = detect_frames(img)

    if not frame_ranges:
        frame_ranges = [(0, img.size[0])]

    frames = []
    for xs, xe in frame_ranges:
        frame_img = img.crop((xs, 0, xe, img.size[1]))
        reduced = reduce_to_pixel_art(frame_img, target_h)
        px_list, fw, fh = extract_pixels(reduced)
        frames.append({
            "w": fw,
            "h": fh,
            "px": px_list,
        })
    return frames


def collect_colors(frames_by_name):
    """Coleta todas as cores unicas."""
    color_map = {}
    for frames in frames_by_name.values():
        for frame in frames:
            for px in frame["px"]:
                r, g, b, a = px[2], px[3], px[4], px[5]
                key = (r, g, b, a)
                if key not in color_map:
                    color_map[key] = len(color_map) + 1
    return color_map


def main():
    print("=== PNG -> Pixel Art Procedural ===\n")

    sprites = {}
    for name, filename in SPRITE_FILES.items():
        path = os.path.join(PROJECT_DIR, filename)
        if not os.path.exists(path):
            print("[SKIP] {}".format(filename))
            continue
        frames = process_image(path, TARGET_HEIGHT)
        sprites[name] = frames
        for i, f in enumerate(frames):
            print("[OK] {}[{}]: {}x{}, {} pixels".format(
                name, i, f["w"], f["h"], len(f["px"])))

    if not sprites:
        print("Nenhum sprite encontrado!")
        return

    color_map = collect_colors(sprites)
    print("\nCores unicas: {}".format(len(color_map)))

    # Converter cores para lista indexada
    colors = [None] * (len(color_map) + 1)
    for rgba, idx in color_map.items():
        colors[idx] = [rgba[0], rgba[1], rgba[2], rgba[3]]

    # Converter pixels para formato compacto (x, y, color_id)
    for name in sprites:
        for frame in sprites[name]:
            new_px = []
            for px in frame["px"]:
                x, y, r, g, b, a = px[0], px[1], px[2], px[3], px[4], px[5]
                cid = color_map[(r, g, b, a)]
                new_px.append([x, y, cid])
            frame["px"] = new_px

    data = {
        "colors": colors,
        "sprites": sprites,
    }

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    output = os.path.join(OUTPUT_DIR, "soldier.json")
    with open(output, "w") as f:
        json.dump(data, f)

    size_kb = os.path.getsize(output) / 1024
    print("\nSalvo: {} ({:.1f} KB)".format(output, size_kb))
    print("Cores: {}".format(len(colors) - 1))

    total_px = sum(len(f["px"]) for frames in sprites.values() for f in frames)
    print("Total de pixels: {}".format(total_px))


if __name__ == "__main__":
    main()
