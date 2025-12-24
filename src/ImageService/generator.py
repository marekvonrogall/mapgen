from flask import Flask, jsonify, request
from PIL import Image, ImageDraw
import os
import uuid

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)
IMG_SIZE = 128
BASE_ASSET_WIDTH = 32
DEFAULT_TEAM_NAMES = ["team1", "team2", "team3", "team4"]
DEFAULT_TEAM_COLORS = [
    (100, 255, 100),
    (100, 255, 255),
    (255, 255, 100),
    (255, 100, 100),
]


def detect_bingo(
    grid_size, items, draw, cell_width, border_width, line_width, team_info, padding
):
    # Grid for each team
    grid = {
        team: [[False] * grid_size for _ in range(grid_size)]
        for team in team_info.keys()
    }

    # Check for item/block completion
    for item in items:
        if item["row"] + 1 > grid_size or item["column"] + 1 > grid_size:
            continue
        row = item["row"]
        column = item["column"]

        if "completed" in item:
            for team, value in item.get("completed", {}).items():
                if value and team in grid:
                    grid[team][row][column] = True

    def calculate_cell_coordinates(row, column):
        x = int(column * cell_width + border_width + (line_width * column))
        y = int(row * cell_width + border_width + (line_width * row))
        return x, y

    # Check for bingo
    for team_name in team_info.keys():
        team_grid = grid[team_name]
        team_color = team_info[team_name]["color"]

        # Check rows and columns
        for i in range(grid_size):
            if all(team_grid[i]):  # Row bingo
                start_x, start_y = calculate_cell_coordinates(i, 0)
                end_x, end_y = calculate_cell_coordinates(i, grid_size - 1)
                if grid_size > 1:
                    draw_bingo_line(
                        draw, start_x, start_y, end_x, end_y, cell_width, team_color, padding
                    )
                return team_name

            if all(row[i] for row in team_grid): # Column bingo
                start_x, start_y = calculate_cell_coordinates(0, i)
                end_x, end_y = calculate_cell_coordinates(grid_size - 1, i)
                draw_bingo_line(
                    draw, start_x, start_y, end_x, end_y, cell_width, team_color, padding
                )
                return team_name

        # Check diagonals
        if all(team_grid[i][i] for i in range(grid_size)): # Top-left to bottom-right
            start_x, start_y = calculate_cell_coordinates(0, 0)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, grid_size - 1)
            draw_bingo_line(
                draw, start_x, start_y, end_x, end_y, cell_width, team_color, padding
            )
            return team_name

        if all(team_grid[i][grid_size - i - 1] for i in range(grid_size)): # Top-right to bottom-left
            start_x, start_y = calculate_cell_coordinates(0, grid_size - 1)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, 0)
            draw_bingo_line(
                draw, start_x, start_y, end_x, end_y, cell_width, team_color, padding
            )
            return team_name

    return None  # No bingo detected


def draw_bingo_line(
    draw, start_cell_x, start_cell_y, end_cell_x, end_cell_y, cell_width, color, padding
):
    cx1 = start_cell_x + cell_width / 2
    cy1 = start_cell_y + cell_width / 2
    cx2 = end_cell_x + cell_width / 2
    cy2 = end_cell_y + cell_width / 2

    # main line
    draw.line(
        [(cx1, cy1), (cx2, cy2)],
        fill=color,
        width=padding,
    )

    # round caps
    r = padding // 2

    draw.ellipse(
        [cx1 - r, cy1 - r, cx1 + r, cy1 + r],
        fill=color,
    )
    draw.ellipse(
        [cx2 - r, cy2 - r, cx2 + r, cy2 + r],
        fill=color,
    )


def draw_line(
    draw, types, cell_x, cell_y, cell_width, color, padding
):
    for type in types:
        match type:
            case "top-left":
                draw.line(
                    [(cell_x, cell_y + 1), (cell_x + (cell_width / 2) - 1, cell_y + 1)],
                    fill=color,
                    width=padding,
                )
                draw.line(
                    [(cell_x + 1, cell_y), (cell_x + 1, cell_y + (cell_width / 2) - 1)],
                    fill=color,
                    width=padding,
                )
            case "top-right":
                draw.line(
                    [
                        (cell_x + (cell_width / 2), cell_y + 1),
                        (cell_x + cell_width - 1, cell_y + 1),
                    ],
                    fill=color,
                    width=padding,
                )
                draw.line(
                    [
                        (cell_x + cell_width - 2, cell_y),
                        (cell_x + cell_width - 2, cell_y + (cell_width / 2) - 1),
                    ],
                    fill=color,
                    width=padding,
                )
            case "bottom-left":
                draw.line(
                    [
                        (cell_x, cell_y + cell_width - 2),
                        (cell_x + (cell_width / 2) - 1, cell_y + cell_width - 2),
                    ],
                    fill=color,
                    width=padding,
                )
                draw.line(
                    [
                        (cell_x + 1, cell_y + (cell_width / 2)),
                        (cell_x + 1, cell_y + cell_width - 1),
                    ],
                    fill=color,
                    width=padding,
                )
            case "bottom-right":
                draw.line(
                    [
                        (cell_x + (cell_width / 2), cell_y + cell_width - 2),
                        (cell_x + cell_width - 1, cell_y + cell_width - 2),
                    ],
                    fill=color,
                    width=padding,
                )
                draw.line(
                    [
                        (cell_x + cell_width - 2, cell_y + (cell_width / 2)),
                        (cell_x + cell_width - 2, cell_y + cell_width - 2),
                    ],
                    fill=color,
                    width=padding,
                )
            case _:
                continue


def compute_grid_params(
    grid_size: int,
    pixel_perfect: bool = True,
    constraints: dict = None,
):
    if constraints is None:
        constraints = {}

    min_padding = constraints.get("min_padding", 1)
    min_line_width = constraints.get("min_line_width", 1)
    min_border_width = constraints.get("min_border_width", 1)

    def is_pixel_perfect(asset_width: int) -> bool:
        return (
                asset_width % BASE_ASSET_WIDTH == 0 or BASE_ASSET_WIDTH % asset_width == 0
        )

    best = None

    if grid_size == 1:
        max_line_width = min_line_width + 1
        max_border_width = (IMG_SIZE - 1) // 2

    else:
        max_line_width = max(
            min_line_width,
            (IMG_SIZE - 2 * min_border_width - grid_size)
            // (grid_size - 1)
        )

        max_border_width = max(
            min_border_width,
            (IMG_SIZE - (grid_size - 1) * min_line_width - grid_size)
            // 2
        )

    for border_width in range(min_border_width, max_border_width):
        for line_width in range(min_line_width, max_line_width):
            total_lines = line_width * (grid_size - 1)
            total_borders = border_width * 2
            remaining = IMG_SIZE - total_lines - total_borders

            if remaining <= 0 or remaining % grid_size != 0:
                continue

            cell_width = remaining // grid_size

            for padding in range(min_padding, cell_width // 2 + 1):
                asset_width = cell_width - 2 * padding
                if asset_width <= 0:
                    continue

                if pixel_perfect and not is_pixel_perfect(asset_width):
                    continue

                # - Prefer larger assets
                # - Penalize thick padding (if above min)
                # - Penalize thick line and border width (if above min)
                score = (
                    asset_width * 1000
                    - (padding - min_padding) * 20
                    - (line_width - min_line_width) * 10
                    - (border_width - min_border_width) * 10
                )

                candidate = {
                    "cell_width": cell_width,
                    "asset_width": asset_width,
                    "padding": padding,
                    "line_width": line_width,
                    "border_width": border_width,
                    "score": score,
                }

                if best is None or candidate["score"] > best["score"]:
                    best = candidate

    if best is None:
        return jsonify(
            {
                "error": f"No valid grid configuration found under given constraints.)"
            }
        ), 400

    best.pop("score")
    return best

pre_computed_grid_params = {
    1: compute_grid_params(1, True, {"min_padding": 6, "min_line_width": 3, "min_border_width": 3}),
    2: compute_grid_params(2, True, {"min_padding": 3, "min_line_width": 7, "min_border_width": 9}),
    3: compute_grid_params(3, True, {"min_padding": 1, "min_line_width": 3, "min_border_width": 3}),
    4: compute_grid_params(4, True, {"min_padding": 1, "min_line_width": 1, "min_border_width": 3}),
    5: compute_grid_params(5, True, {"min_padding": 1, "min_line_width": 1, "min_border_width": 3}),
    6: compute_grid_params(6, True, {"min_padding": 1, "min_line_width": 1, "min_border_width": 1}),
    7: compute_grid_params(7, False, {"min_padding": 1, "min_line_width": 1, "min_border_width": 1}),
    8: compute_grid_params(8, False, {"min_padding": 1, "min_line_width": 1, "min_border_width": 1}),
    9: compute_grid_params(9, False, {"min_padding": 1, "min_line_width": 1, "min_border_width": 1}),
}

@app.route("/generate", methods=["POST"])
def generate_image():
    try:
        data = request.get_json()

        settings = data.get("settings", {})
        items = data.get("items", [])

        if not settings and not items:
            settings = data.get("mapRAW", {}).get("settings", {})
            items = data.get("mapRAW", {}).get("items", [])

        teams = settings.get("teams", [])
        grid_size = settings.get("grid_size", 5)

        if grid_size < 1 or grid_size > 9:
            return jsonify(
                {
                    "error": "Invalid grid size entered (grid_size in 'settings' section)."
                }
            ), 400

        team_info = {}

        for i, team in enumerate(teams):
            team_name = team.get("name", DEFAULT_TEAM_NAMES[i])
            team_placement = team.get("placement", None)
            team_color = team.get("color", DEFAULT_TEAM_COLORS[i])

            team_info[team_name] = {
                "name": team_name,
                "placement": team_placement,
                "color": team_color,
            }

        # Colors
        bg_color = (214, 190, 150)  # Light Beige
        line_color = (153, 135, 108)  # Dark Beige
        outline_color = (153, 135, 108)  # Dark Beige

        # Dimensions
        grid_params = pre_computed_grid_params.get(grid_size, None)
        if not grid_params:
            grid_params = compute_grid_params(
            grid_size=grid_size,
            pixel_perfect=True,
        )

        # Create the base image
        image = Image.new("RGBA", (IMG_SIZE, IMG_SIZE), bg_color)
        draw = ImageDraw.Draw(image)

        # Grid lines
        if grid_params["line_width"] > 0:
            for i in range(grid_size - 1):
                # Vertical lines
                x = (
                    grid_params["border_width"]
                    + (i + 1) * grid_params["cell_width"]
                    + i * grid_params["line_width"]
                )
                draw.polygon([(x,0),(x+grid_params["line_width"]-1,0),(x+grid_params["line_width"]-1,IMG_SIZE-1),(x,IMG_SIZE-1)], fill=line_color)

                # Horizontal lines
                y = (
                    grid_params["border_width"]
                    + (i + 1) * grid_params["cell_width"]
                    + i * grid_params["line_width"]
                )
                draw.polygon([(0,y),(IMG_SIZE-1,y),(IMG_SIZE-1,y+grid_params["line_width"]-1),(0,y+grid_params["line_width"]-1)], fill=line_color)

        # Border
        draw.rectangle(
            (0, 0, IMG_SIZE - 1, IMG_SIZE - 1),
            outline=outline_color,
            width=grid_params["border_width"],
        )

        # Add images from textures
        for item in items:
            if item["row"] + 1 > grid_size or item["column"] + 1 > grid_size:
                continue
            row = item["row"]
            column = item["column"]
            texture_name = item["sprite"]

            completed_teams = []

            if "completed" in item:
                completed_teams = [
                    team for team, value in item.get("completed", {}).items() if value
                ]

            # Path to texture
            texture_path = os.path.join(TEXTURES_DIR, f"{texture_name}")

            # Check if texture exists
            if not os.path.exists(texture_path):
                return jsonify(
                    {
                        "error": f"Invalid texture {texture_name} provided."
                    }
                ), 400

            # Open texture
            texture_image = Image.open(texture_path)

            # Convert to RGBA
            if texture_image.mode != "RGBA":
                texture_image = texture_image.convert("RGBA")

            # Calculate texture position on grid
            cell_x = grid_params["border_width"] + column * (
                grid_params["cell_width"] + grid_params["line_width"]
            )
            cell_y = grid_params["border_width"] + row * (
                grid_params["cell_width"] + grid_params["line_width"]
            )

            x0 = cell_x + grid_params["padding"]
            y0 = cell_y + grid_params["padding"]

            # Might stretch texture, but ensures good styling
            x1 = x0 + grid_params["asset_width"]
            y1 = y0 + grid_params["asset_width"]

            texture_image = texture_image.resize(
                (x1 - x0, y1 - y0), resample=Image.Resampling.NEAREST
            )

            # Paste texture on map image
            image.paste(texture_image, (x0, y0), texture_image)

            # Item / Block completion
            if completed_teams:
                for completed_team in completed_teams:
                    rectColor = team_info[completed_team]["color"]
                    placement = team_info[completed_team]["placement"]

                    if not rectColor or not placement:
                        return jsonify(
                            {
                                "error": f"Invalid team key entered ({completed_team} in 'completed' section of '{texture_name}' [row {row}, column {column}])."
                            }
                        ), 400

                    types: list[str] = []
                    match placement:
                        case "top":
                            types.append("top-left")
                            types.append("top-right")
                        case "bottom":
                            types.append("bottom-left")
                            types.append("bottom-right")
                        case "left":
                            types.append("top-left")
                            types.append("bottom-left")
                        case "right":
                            types.append("top-right")
                            types.append("bottom-right")
                        case "full":
                            draw.rectangle(
                                (cell_x,
                                    cell_y,
                                    cell_x + grid_params["cell_width"] - 1,
                                    cell_y + grid_params["cell_width"] - 1,
                                ),
                                outline=rectColor,
                                width=grid_params["padding"],
                            )
                        case _:
                            types.append(placement)

                    draw_line(
                        draw,
                        types,
                        cell_x,
                        cell_y,
                        grid_params["cell_width"],
                        rectColor,
                        grid_params["padding"],
                    )

        # Detect and draw bingo
        bingo_result = detect_bingo(
            grid_size,
            items,
            draw,
            grid_params["cell_width"],
            grid_params["border_width"],
            grid_params["line_width"],
            team_info,
            grid_params["padding"],
        )

        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"url": f"/public/{filename}", "bingo": bingo_result}), 201

    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5008)
