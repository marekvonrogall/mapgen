import os
import uuid

from flask import Flask, jsonify, request
from PIL import Image, ImageColor, ImageDraw

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)
IMG_SIZE = 128
BASE_ASSET_WIDTH = 32
DEFAULT_TEAM_NAMES = ["team1", "team2", "team3", "team4"]
DEFAULT_TEAM_COLORS = [
    "#64FF64",
    "#64FFFF",
    "#FFFF64",
    "#FF6464",
]


def detect_bingo(grid_size, items, draw, grid_params, team_info):
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
        x = int(
            column * grid_params["cell_width"]
            + grid_params["border_width"]
            + (grid_params["line_width"] * column)
        )
        y = int(
            row * grid_params["cell_width"]
            + grid_params["border_width"]
            + (grid_params["line_width"] * row)
        )
        return x, y

    # Check for bingo
    for team_name in team_info.keys():
        team_grid = grid[team_name]
        team_color = team_info[team_name]["color"]

        # Check rows and columns
        for i in range(grid_size):
            if all(team_grid[i]):
                # Row bingo
                start_x, start_y = calculate_cell_coordinates(i, 0)
                end_x, end_y = calculate_cell_coordinates(i, grid_size - 1)
                if grid_size > 1:
                    draw_bingo_line(
                        draw,
                        start_x,
                        start_y,
                        end_x,
                        end_y,
                        grid_params["cell_width"],
                        team_color,
                        grid_params["padding"],
                    )
                return team_name

            if all(row[i] for row in team_grid):
                # Column bingo
                start_x, start_y = calculate_cell_coordinates(0, i)
                end_x, end_y = calculate_cell_coordinates(grid_size - 1, i)
                draw_bingo_line(
                    draw,
                    start_x,
                    start_y,
                    end_x,
                    end_y,
                    grid_params["cell_width"],
                    team_color,
                    grid_params["padding"],
                )
                return team_name

        # Check diagonals
        if all(team_grid[i][i] for i in range(grid_size)):
            # Top-left to bottom-right
            start_x, start_y = calculate_cell_coordinates(0, 0)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, grid_size - 1)
            draw_bingo_line(
                draw,
                start_x,
                start_y,
                end_x,
                end_y,
                grid_params["cell_width"],
                team_color,
                grid_params["padding"],
            )
            return team_name

        if all(team_grid[i][grid_size - i - 1] for i in range(grid_size)):
            # Top-right to bottom-left
            start_x, start_y = calculate_cell_coordinates(0, grid_size - 1)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, 0)
            draw_bingo_line(
                draw,
                start_x,
                start_y,
                end_x,
                end_y,
                grid_params["cell_width"],
                team_color,
                grid_params["padding"],
            )
            return team_name

    return None  # No bingo detected


def draw_bingo_line(
    draw, start_cell_x, start_cell_y, end_cell_x, end_cell_y, cell_width, color, padding
):
    cx1 = start_cell_x + cell_width // 2
    cy1 = start_cell_y + cell_width // 2
    cx2 = end_cell_x + cell_width // 2
    cy2 = end_cell_y + cell_width // 2

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


def draw_line(draw, types, cell_x, cell_y, cell_width, color, padding):
    for type in types:
        match type:
            case "top-left":
                # Horizontal line
                draw.polygon(
                    [
                        (cell_x, cell_y),
                        (cell_x + (cell_width // 2) - 1, cell_y),
                        (cell_x + (cell_width // 2) - 1, cell_y + padding - 1),
                        (cell_x, cell_y + padding - 1),
                    ],
                    fill=color,
                )
                # Vertical line
                draw.polygon(
                    [
                        (cell_x, cell_y),
                        (cell_x + padding - 1, cell_y),
                        (cell_x + padding - 1, cell_y + (cell_width // 2) - 1),
                        (cell_x, cell_y + (cell_width // 2) - 1),
                    ],
                    fill=color,
                )
            case "top-right":
                # Horizontal line
                draw.polygon(
                    [
                        (cell_x + (cell_width // 2), cell_y),
                        (cell_x + cell_width - 1, cell_y),
                        (cell_x + cell_width - 1, cell_y + padding - 1),
                        (cell_x + (cell_width // 2), cell_y + padding - 1),
                    ],
                    fill=color,
                )
                # Vertical line
                draw.polygon(
                    [
                        (cell_x + cell_width - padding, cell_y),
                        (cell_x + cell_width - 1, cell_y),
                        (cell_x + cell_width - 1, cell_y + (cell_width // 2) - 1),
                        (cell_x + cell_width - padding, cell_y + (cell_width // 2) - 1),
                    ],
                    fill=color,
                )
            case "bottom-left":
                # Horizontal line
                draw.polygon(
                    [
                        (cell_x, cell_y + cell_width - padding),
                        (cell_x + (cell_width // 2) - 1, cell_y + cell_width - padding),
                        (cell_x + (cell_width // 2) - 1, cell_y + cell_width - 1),
                        (cell_x, cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
                # Vertical line
                draw.polygon(
                    [
                        (cell_x, cell_y + (cell_width // 2)),
                        (cell_x + padding - 1, cell_y + (cell_width // 2)),
                        (cell_x + padding - 1, cell_y + cell_width - 1),
                        (cell_x, cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
            case "bottom-right":
                # Horizontal line
                draw.polygon(
                    [
                        (cell_x + (cell_width // 2), cell_y + cell_width - padding),
                        (cell_x + cell_width - 1, cell_y + cell_width - padding),
                        (cell_x + cell_width - 1, cell_y + cell_width - 1),
                        (cell_x + (cell_width // 2), cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
                # Vertical line
                draw.polygon(
                    [
                        (cell_x + cell_width - padding, cell_y + (cell_width // 2)),
                        (cell_x + cell_width - 1, cell_y + (cell_width // 2)),
                        (cell_x + cell_width - 1, cell_y + cell_width - 1),
                        (cell_x + cell_width - padding, cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
            case _:
                continue


def compute_grid_params(grid_size: int, constraints: dict) -> dict[str, int]:
    errors = []

    min_padding = constraints.get("min_padding", 0)
    max_padding = constraints.get("max_padding", None)
    min_line_width = constraints.get("min_line_width", 0)
    max_line_width = constraints.get("max_line_width", None)
    min_border_width = constraints.get("min_border_width", 0)
    max_border_width = constraints.get("max_border_width", None)
    pixel_perfect = constraints.get("pixel_perfect", True)
    fill_board = constraints.get("fill_board", True)

    int_keys = [
        "min_padding",
        "max_padding",
        "min_line_width",
        "max_line_width",
        "min_border_width",
        "max_border_width",
    ]
    bool_keys = ["pixel_perfect", "fill_board", "center_board"]
    min_max_pairs = [
        ("min_padding", "max_padding"),
        ("min_line_width", "max_line_width"),
        ("min_border_width", "max_border_width"),
    ]

    for key in int_keys:
        value = constraints.get(key, None)
        if value is not None and not isinstance(value, int):
            errors.append(
                f"Constraints: '{key}': Expected integer, got {type(value).__name__}"
            )
        elif isinstance(value, int) and value < 0:
            errors.append(f"Constraints: '{key}': Must be >= 0, got {value}")

    for key in bool_keys:
        value = constraints.get(key, None)
        if value is not None and not isinstance(value, bool):
            errors.append(
                f"Constraints: '{key}': Expected boolean, got {type(value).__name__}"
            )

    for min_key, max_key in min_max_pairs:
        min_value = constraints.get(min_key, 0)
        max_value = constraints.get(max_key, None)
        if isinstance(min_value, int) and isinstance(max_value, int):
            if max_value is not None and min_value > max_value:
                errors.append(
                    f"Constraints: '{min_key}': Cannot be greater than '{max_key}' ({min_value} > {max_value})"
                )

    if errors:
        raise ValueError(errors)

    def is_pixel_perfect(asset_width: int) -> bool:
        return (
            asset_width % BASE_ASSET_WIDTH == 0 or BASE_ASSET_WIDTH % asset_width == 0
        )

    def evaluate() -> tuple[bool, dict[str, int]]:
        asset_width = cell_width - padding * 2

        if asset_width <= 0:
            return False, {}

        if pixel_perfect and not is_pixel_perfect(asset_width):
            return False, {}

        score = (
            asset_width * 1000
            - (padding - min_padding) * 20
            - (line_width - min_line_width) * 10
            - (border_width - min_border_width) * 10
        )

        return True, {
            "cell_width": cell_width,
            "asset_width": asset_width,
            "padding": padding,
            "line_width": line_width,
            "border_width": border_width,
            "score": score,
        }

    if max_line_width is None:
        max_line_width = IMG_SIZE
    if max_border_width is None:
        max_border_width = IMG_SIZE
    if max_padding is None:
        max_padding = IMG_SIZE // 2

    best = None

    for border_width in range(min_border_width, max_border_width + 1):
        if max_border_width == 0:
            border_width = 0
        for line_width in range(min_line_width, max_line_width + 1):
            if max_line_width == 0:
                line_width = 0
            total_lines = line_width * (grid_size - 1)
            total_borders = border_width * 2
            remaining = IMG_SIZE - total_lines - total_borders

            if remaining <= 0:
                continue

            cell_width_candidates = []

            if fill_board:
                if remaining % grid_size != 0:
                    continue
                cw = remaining // grid_size
                cell_width_candidates.append(cw)
            else:
                cell_width_candidates = list(range(1, remaining // grid_size + 1))

            for cell_width in cell_width_candidates:
                mp = min(max_padding, cell_width // 2)

                for padding in range(min_padding, mp + 1):
                    if mp == 0:
                        padding = 0

                    valid, candidate = evaluate()
                    if not valid:
                        continue

                    if best is None or candidate["score"] > best["score"]:
                        best = candidate

    if best is None:
        msg = "No valid grid configuration found under given constraints."
        raise ValueError(msg)

    best.pop("score")
    return best


pre_computed_grid_params = {
    1: compute_grid_params(
        1,
        {
            "min_padding": 8,
            "min_line_width": 0,
            "min_border_width": 8,
            "pixel_perfect": True,
        },
    ),
    2: compute_grid_params(
        2,
        {
            "min_padding": 3,
            "min_line_width": 7,
            "min_border_width": 9,
            "pixel_perfect": True,
        },
    ),
    3: compute_grid_params(
        3,
        {
            "min_padding": 1,
            "min_line_width": 3,
            "min_border_width": 3,
            "pixel_perfect": True,
        },
    ),
    4: compute_grid_params(
        4,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 3,
            "pixel_perfect": True,
        },
    ),
    5: compute_grid_params(
        5,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 3,
            "pixel_perfect": True,
        },
    ),
    6: compute_grid_params(
        6,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 1,
            "pixel_perfect": True,
        },
    ),
    7: compute_grid_params(
        7,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 1,
            "pixel_perfect": False,
        },
    ),
    8: compute_grid_params(
        8,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 1,
            "pixel_perfect": False,
        },
    ),
    9: compute_grid_params(
        9,
        {
            "min_padding": 1,
            "min_line_width": 1,
            "min_border_width": 1,
            "pixel_perfect": False,
        },
    ),
}


@app.route("/generate", methods=["POST"])
def generate_image():
    try:
        data = request.get_json()

        settings = data.get("settings", {})
        items = data.get("items", [])

        if not settings and not items:
            settings = data.get("map_raw", {}).get("settings", {})
            items = data.get("map_raw", {}).get("items", [])

        grid_size = settings.get("grid_size", 5)
        teams = settings.get("teams", [])
        constraints = settings.get("constraints", {})

        if grid_size < 1 or grid_size > 9:
            msg = "Invalid grid size entered (grid_size in 'settings' section)."
            raise ValueError(msg)

        team_info = {}
        invalid_colors = []

        for i, team in enumerate(teams):
            team_name = team.get("name", DEFAULT_TEAM_NAMES[i])
            team_placement = team.get("placement", None)
            team_color = str(team.get("color") or DEFAULT_TEAM_COLORS[i])
            try:
                ImageColor.getrgb(team_color)
            except (ValueError, TypeError):
                invalid_colors.append(team_color)

            team_info[team_name] = {
                "name": team_name,
                "placement": team_placement,
                "color": team_color,
            }

        # Colors
        custom_colors = settings.get("colors", {})
        bg_color = str(
            custom_colors.get("bg_color", None)
            or custom_colors.get("background_color", None)
            or "#D6BE96"
        )  # Light Beige
        fg_color = str(
            custom_colors.get("fg_color", None)
            or custom_colors.get("foreground_color", None)
            or "#99876C"
        )  # Dark Beige
        line_color = str(custom_colors.get("line_color", None) or fg_color)
        border_color = str(custom_colors.get("border_color", None) or fg_color)

        colors = [bg_color, fg_color, line_color, border_color]

        for color in colors:
            try:
                ImageColor.getrgb(color)
            except (ValueError, TypeError):
                invalid_colors.append(color)

        if invalid_colors:
            msg = f"Invalid colors provided: {', '.join(invalid_colors)}"
            raise ValueError(msg)

        # Dimensions
        recompute_keys = {
            "min_padding",
            "max_padding",
            "min_line_width",
            "max_line_width",
            "min_border_width",
            "max_border_width",
            "pixel_perfect",
            "fill_board",
        }

        should_recompute = (
            constraints
            and any(key in constraints for key in recompute_keys)
        )

        if should_recompute:
            grid_params = compute_grid_params(
                grid_size=grid_size,
                constraints=constraints,
            )
        else:
            grid_params = pre_computed_grid_params.get(grid_size, None)

        # Create the base image
        image = Image.new("RGBA", (IMG_SIZE, IMG_SIZE), bg_color)
        draw = ImageDraw.Draw(image)

        used_width = (
            grid_params["cell_width"] * grid_size
            + grid_params["line_width"] * (grid_size - 1)
            + grid_params["border_width"] * 2
        )

        # Grid lines
        if grid_params["line_width"] > 0:
            for i in range(grid_size - 1):
                # Vertical lines
                x = (
                    grid_params["border_width"]
                    + (i + 1) * grid_params["cell_width"]
                    + i * grid_params["line_width"]
                )
                draw.polygon(
                    [
                        (x, 0),
                        (x + grid_params["line_width"] - 1, 0),
                        (x + grid_params["line_width"] - 1, used_width - 1),
                        (x, used_width - 1),
                    ],
                    fill=line_color,
                )

                # Horizontal lines
                y = (
                    grid_params["border_width"]
                    + (i + 1) * grid_params["cell_width"]
                    + i * grid_params["line_width"]
                )
                draw.polygon(
                    [
                        (0, y),
                        (used_width - 1, y),
                        (used_width - 1, y + grid_params["line_width"] - 1),
                        (0, y + grid_params["line_width"] - 1),
                    ],
                    fill=line_color,
                )

        # Border
        if grid_params["border_width"] > 0:
            draw.rectangle(
                (0, 0, used_width - 1, used_width - 1),
                outline=border_color,
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
                msg = f"Invalid texture {texture_name} provided."
                raise ValueError(msg)

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
                        msg = f"Invalid team key entered ({completed_team} in 'completed' section of '{texture_name}' [row {row}, column {column}])."
                        raise ValueError(msg)

                    types: list[str] = []
                    if grid_params["padding"] > 0:
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
                                    (
                                        cell_x,
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
            grid_params,
            team_info,
        )

        center_board = constraints.get("center_board", True)
        if center_board and used_width != IMG_SIZE:
            offset = (IMG_SIZE - used_width) // 2
            new_canvas = Image.new("RGBA", (IMG_SIZE, IMG_SIZE), bg_color)
            new_canvas.paste(image, (offset, offset))
            image = new_canvas

        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"url": f"/public/{filename}", "bingo": bingo_result}), 201

    except Exception as e:
        return jsonify({"imggen": str(e)}), 500


if __name__ == "__main__":
    port = int(os.environ.get("IMGGEN_PORT", 5000))
    app.run(host="0.0.0.0", port=port)
