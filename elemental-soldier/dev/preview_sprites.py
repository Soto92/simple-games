"""
Generates a visual preview of the converted pixel-art sprites.
Saves a PNG image with all frames side by side.
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

SPRITE_NAMES = ["idle", "run", "jump", "attack", "dead", "absorb", "hit", "squat"]

# Calculate total preview size
SPACING = 4
LABEL_HEIGHT = 16
total_w = SPACING
max_h = 64

for name in SPRITE_NAMES:
    frames = sprites.get(name, [])
    for frame in frames:
        total_w += frame["w"] + SPACING

total_h = max_h + LABEL_HEIGHT + SPACING * 2

# Create image
img = Image.new("RGBA", (total_w + SPACING, total_h), (30, 30, 40, 255))
draw = ImageDraw.Draw(img)

# Ground line
GROUND_LINE = LABEL_HEIGHT + max_h - 1

x_offset = SPACING
for name in SPRITE_NAMES:
    frames = sprites.get(name, [])
    for i, frame in enumerate(frames):
        label = "{}[{}]".format(name, i)
        draw.text((x_offset, 2), label, fill=(200, 200, 200))

        # Feet-aligned: bottom of each frame touches the ground line
        draw_y = GROUND_LINE - frame["h"] + 1
        for px in frame["px"]:
            px_x, px_y, color_id = px[0], px[1], px[2]
            c = colors[color_id]
            draw.point((x_offset + px_x, draw_y + px_y),
                       fill=(c[0], c[1], c[2], c[3]))

        x_offset += frame["w"] + SPACING

os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
img.save(OUTPUT_PATH)
print("Preview saved: {}".format(OUTPUT_PATH))
print("Size: {}x{} pixels".format(img.size[0], img.size[1]))

# Convert to global unique palette
all_colors = set()
for name, frames in sprites.items():
    for frame in frames:
        for px in frame["px"]:
            all_colors.add(px[2])

print("Unique colors in JSON: {}".format(len(all_colors)))
print("Total colors in table: {}".format(len(colors) - 1))