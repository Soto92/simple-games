"""
Gera preview visual dos sprites convertidos para pixel art.
Salva uma imagem PNG com todos os frames lado a lado.
"""
from PIL import Image, ImageDraw
import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(SCRIPT_DIR)
JSON_PATH = os.path.join(PROJECT_DIR, "data", "sprites", "soldier.json")
OUTPUT_PATH = os.path.join(PROJECT_DIR, "data", "sprites", "preview.png")

with open(JSON_PATH) as f:
    data = json.load(f)

colors = data["colors"]
sprites = data["sprites"]

# Calcular tamanho total da preview
SPACING = 4
LABEL_HEIGHT = 16
total_w = SPACING
max_h = 0

for name in ["idle", "run", "jump", "fall", "attack", "death"]:
    frames = sprites.get(name, [])
    for frame in frames:
        total_w += frame["w"] + SPACING
        max_h = max(max_h, frame["h"])

total_h = max_h + LABEL_HEIGHT + SPACING * 2

# Criar imagem
img = Image.new("RGBA", (total_w, total_h), (30, 30, 40, 255))
draw = ImageDraw.Draw(img)

x_offset = SPACING
for name in ["idle", "run", "jump", "fall", "attack", "death"]:
    frames = sprites.get(name, [])
    for i, frame in enumerate(frames):
        label = "{}[{}]".format(name, i)
        draw.text((x_offset, 2), label, fill=(200, 200, 200))

        for px in frame["px"]:
            px_x, px_y, color_id = px[0], px[1], px[2]
            c = colors[color_id]
            draw.point((x_offset + px_x, LABEL_HEIGHT + px_y),
                       fill=(c[0], c[1], c[2], c[3]))

        x_offset += frame["w"] + SPACING

os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
img.save(OUTPUT_PATH)
print("Preview salvo: {}".format(OUTPUT_PATH))
print("Tamanho: {}x{} pixels".format(img.size[0], img.size[1]))

# Converter para paleta global unica
all_colors = set()
for name, frames in sprites.items():
    for frame in frames:
        for px in frame["px"]:
            all_colors.add(px[2])

print("Cores unicas no JSON: {}".format(len(all_colors)))
print("Cores totais na tabela: {}".format(len(colors) - 1))
