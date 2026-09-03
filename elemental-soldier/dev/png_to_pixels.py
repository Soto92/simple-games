"""
Converts PNG sprites to procedural pixel arrays (pixel art).
Reduces to limited size and color palette, ideal for procedural drawing.

Improvements:
- Cuts anti-aliasing (semi-transparent edge pixels)
- Forces solid alpha (255) on all visible pixels
- Removes isolated pixels (noise)
- Quantizes colors only on visible pixels

Usage: python dev/png_to_pixels.py
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
    "attack": "attack.png",
    "dead": "dead.png",
    "absorb": "absortion.png",
    "hit": "reactingToDamage.png",
    "squat": "squat.png",
}

TARGET_HEIGHT = 64

# Pixels with alpha below this are discarded (anti-aliasing)
VISIBLE_ALPHA = 140
# Number of colors per frame
PALETTE_SIZE = 32


def column_density(img):
    """Count of visible pixels per column (0..w-1)."""
    w, h = img.size
    px = img.load()
    return [sum(1 for y in range(h) if px[x, y][3] >= VISIBLE_ALPHA)
            for x in range(w)]


def find_period(dens, min_period=40):
    """Cell (frame) width via density autocorrelation."""
    n = len(dens)
    if n <= min_period * 2:
        return n, 0.0
    mean = sum(dens) / n
    var = sum((v - mean) ** 2 for v in dens)
    if var == 0:
        return n, 0.0
    scores = []
    for lag in range(min_period, n // 2):
        num = sum((dens[i] - mean) * (dens[i + lag] - mean)
                  for i in range(n - lag))
        scores.append((num / var, lag))
    scores.sort(reverse=True)
    return scores[0][1], scores[0][0]


def best_offset(dens, w_cell):
    """Align the grid: offset that leaves boundaries at emptier columns."""
    n = len(dens)
    best_cost, best_o = None, 0
    for o in range(w_cell):
        cost = 0.0
        cnt = 0
        x = o
        while x < n:
            lo = max(0, x - 1)
            hi = min(n, x + 2)
            cost += sum(dens[lo:hi]) / (hi - lo)
            cnt += 1
            x += w_cell
        cost /= max(1, cnt)
        if best_cost is None or cost < best_cost:
            best_cost, best_o = cost, o
    return best_o


def detect_frames_by_empty_columns(img, w, h):
    """Frames separated by wide bands without visible pixels.

    Very thin bands (pose inner holes, e.g. between the legs)
    do not count as a frame boundary.
    """
    MIN_GAP = 10
    px = img.load()
    ranges = []
    in_frame = False
    start_x = 0
    x = 0
    while x < w:
        if all(px[x, y][3] < VISIBLE_ALPHA for y in range(h)):
            xe = x
            while xe < w and all(px[xe, y][3] < VISIBLE_ALPHA for y in range(h)):
                xe += 1
            if xe - x >= MIN_GAP:
                if in_frame:
                    ranges.append((start_x, x))
                    in_frame = False
            x = xe
        else:
            if not in_frame:
                start_x = x
                in_frame = True
            x += 1
    if in_frame:
        ranges.append((start_x, w))
    if len(ranges) >= 2:
        return ranges
    return None


def detect_separator_bands(dens, max_dens, thresh=0.08, min_band=2):
    """Bands of sparse columns (likely frame boundaries)."""
    bands = []
    in_band = False
    for x, d in enumerate(dens):
        empty = d < max_dens * thresh
        if empty and not in_band:
            start = x
            in_band = True
        elif not empty and in_band:
            if x - start >= min_band:
                bands.append((start, x - 1))
            in_band = False
    if in_band and len(dens) - start >= min_band:
        bands.append((start, len(dens) - 1))
    return bands


def detect_frames_by_regions(dens, w):
    """Well-separated frames: crop regions between sparse bands."""
    max_dens = max(dens)
    bands = detect_separator_bands(dens, max_dens)
    interior = [b for b in bands if b[0] > 0 and b[1] < w - 1]
    if len(interior) < 2:
        return None

    ranges = []
    last_end = 0
    for b in bands:
        ranges.append((last_end, b[0]))
        last_end = b[1] + 1
    ranges.append((last_end, w))

    ranges = [r for r in ranges if r[1] - r[0] >= 2]
    if len(ranges) < 2:
        return None
    # no region may dominate the sheet (avoids touching-frame sheets)
    largest = max(r[1] - r[0] for r in ranges)
    if largest > w * 0.6:
        return None
    return ranges


def detect_frames_by_grid(dens, w):
    """Touching frames: uniform grid via period autocorrelation."""
    w_cell, score = find_period(dens)
    if w_cell >= w or score < 0.45:
        return None

    off = best_offset(dens, w_cell)
    max_dens = max(dens)

    ranges = []
    x = off
    while x < w:
        ranges.append((x, min(w, x + w_cell)))
        x += w_cell

    # drop extreme or absurdly minor cells
    while ranges and sum(dens[ranges[0][0]:ranges[0][1]]) < max_dens * 0.01:
        ranges.pop(0)
    while ranges and sum(dens[ranges[-1][0]:ranges[-1][1]]) < max_dens * 0.01:
        ranges.pop()

    if len(ranges) < 2:
        return None
    return ranges


def detect_frames(img):
    """
    Detect frame cells.

    Priority:
    1. Fully transparent columns (sheets separated by columns)
    2. Sparse column bands (well-separated frames, varied widths)
    3. Uniform grid via autocorrelation (touching frames)
    """
    w, h = img.size

    ranges = detect_frames_by_empty_columns(img, w, h)
    if ranges is not None:
        return ranges

    dens = column_density(img)
    if not any(dens):
        return []

    ranges = detect_frames_by_regions(dens, w)
    if ranges is None:
        ranges = detect_frames_by_grid(dens, w)
    if ranges is None:
        return [(0, w)]
    return ranges


def reduce_to_pixel_art(img, scale):
    """
    Reduce to pixel art by a uniform scale factor.
    1. NEAREST resize
    2. Build solid mask (cut anti-aliasing)
    3. Composite over neutral background and quantize colors
    4. Reapply mask (solid alpha)
    """
    w, h = img.size
    new_w = max(1, int(w * scale))
    new_h = max(1, int(h * scale))

    small = img.resize((new_w, new_h), Image.NEAREST).convert("RGBA")

    # Mask: visible pixel if alpha >= threshold
    alpha = small.getchannel("A")
    mask = alpha.point(lambda a: 255 if a >= VISIBLE_ALPHA else 0)

    # Composite visible RGB over dark neutral bg (prevents white bleed into palette)
    rgb = small.convert("RGB")
    bg = Image.new("RGB", rgb.size, (8, 8, 8))
    composited = Image.composite(rgb, bg, mask)

    # Quantize colors (MEDIANCUT works with RGB images)
    quantized = composited.quantize(colors=PALETTE_SIZE, method=Image.Quantize.MEDIANCUT)
    quantized = quantized.convert("RGBA")

    # Reapply solid mask (no semi-transparency)
    quantized.putalpha(mask)
    return quantized


def remove_isolated_pixels(pixel_map):
    """Remove pixels without a visible neighbor (1-pixel noise)."""
    def has_neighbor(x, y):
        return any((x + dx, y + dy) in pixel_map for dx, dy in
                   ((-1, 0), (1, 0), (0, -1), (0, 1)))

    removed = []
    for (x, y) in list(pixel_map.keys()):
        if not has_neighbor(x, y):
            removed.append((x, y))
    for pos in removed:
        del pixel_map[pos]
    return removed


def extract_pixels(img):
    """Extract visible pixels with solid alpha, already noise-cleaned."""
    w, h = img.size
    pixels = img.load()

    pixel_map = {}
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a >= VISIBLE_ALPHA:
                pixel_map[(x, y)] = (r, g, b, 255)

    removed = remove_isolated_pixels(pixel_map)

    result = [[x, y, r, g, b, a] for (x, y), (r, g, b, a) in pixel_map.items()]
    return result, w, h, len(removed)


def crop_visible(frame):
    """Crop the cell to its visible bounds box."""
    w, h = frame.size
    px = frame.load()
    min_x, max_x = w, -1
    min_y, max_y = h, -1
    for y in range(h):
        for x in range(w):
            if px[x, y][3] >= VISIBLE_ALPHA:
                if x < min_x: min_x = x
                if x > max_x: max_x = x
                if y < min_y: min_y = y
                if y > max_y: max_y = y
    if max_x < 0:
        return None
    return frame.crop((min_x, min_y, max_x + 1, max_y + 1))


def prepare_frames(path):
    """Crop each detected column to its visible bounds box."""
    img = Image.open(path).convert("RGBA")
    frame_ranges = detect_frames(img)

    if not frame_ranges:
        frame_ranges = [(0, img.size[0])]

    visibles = []
    for xs, xe in frame_ranges:
        cell = img.crop((xs, 0, xe, img.size[1]))
        visible = crop_visible(cell)
        if visible is None:
            continue
        visibles.append(visible)
    return visibles


def reduce_frames(visibles, scale):
    """Reduce all frames with a shared uniform scale."""
    frames = []
    for visible in visibles:
        reduced = reduce_to_pixel_art(visible, scale)
        px_list, fw, fh, removed = extract_pixels(reduced)
        if len(px_list) < 12 and len(frames) > 0:
            continue  # noise on grid margins
        frames.append({
            "w": fw,
            "h": fh,
            "px": px_list,
        })
    return frames


def collect_colors(frames_by_name):
    """Collect all unique colors."""
    color_map = {}
    for frames in frames_by_name.values():
        for frame in frames:
            for px in frame["px"]:
                key = tuple(px[2:6])
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

        visibles = prepare_frames(path)
        if not visibles:
            continue

        # Reference scale per sheet, robust to outlier frames.
        # The tallest frame is normally the standing pose; but if it's a clear
        # outlier (e.g. a legs-extended leap > 20% taller than the next frame),
        # use the second-tallest so the body keeps idle proportions.
        heights = sorted(v.height for v in visibles)
        max_h = heights[-1]
        if len(heights) >= 2 and max_h > heights[-2] * 1.20:
            max_h = heights[-2]
        scale = TARGET_HEIGHT / max_h

        frames = reduce_frames(visibles, scale)
        sprites[name] = frames
        for i, f in enumerate(frames):
            print("[OK] {}[{}]: {}x{}, {} pixels".format(
                name, i, f["w"], f["h"], len(f["px"])))

    if not sprites:
        print("No sprites found!")
        return

    color_map = collect_colors(sprites)
    print("\nUnique colors: {}".format(len(color_map)))

    colors = [None] * (len(color_map) + 1)
    for rgba, idx in color_map.items():
        colors[idx] = list(rgba)

    for name in sprites:
        for frame in sprites[name]:
            new_px = []
            for px in frame["px"]:
                cid = color_map[tuple(px[2:6])]
                new_px.append([px[0], px[1], cid])
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
    print("\nSaved: {} ({:.1f} KB)".format(output, size_kb))
    print("Colors: {}".format(len(colors) - 1))
    total_px = sum(len(f["px"]) for frames in sprites.values() for f in frames)
    print("Total pixels: {}".format(total_px))


if __name__ == "__main__":
    main()