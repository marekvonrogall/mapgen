import os
import uuid

from flask import Flask, jsonify, request
from PIL import Image, ImageColor, ImageDraw

from game_modes.base import GameModeContext
from game_modes.factory import create_game_mode

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)
IMG_SIZE = 128
BASE_ASSET_WIDTH = 32
DEFAULT_TEAM_COLORS = [
    "#64FF64",
    "#64FFFF",
    "#FFFF64",
    "#FF6464",
]


def parse_map_payload(data: dict) -> tuple[dict, list[dict]]:
    settings = data["settings"]
    items = data["items"]
    return settings, items


def normalize_team_colors(teams: list[dict]) -> dict[str, dict]:
    normalized_teams: dict[str, dict] = {}

    for index, team in enumerate(teams):
        default_color = (
            DEFAULT_TEAM_COLORS[index]
            if index < len(DEFAULT_TEAM_COLORS)
            else DEFAULT_TEAM_COLORS[index % len(DEFAULT_TEAM_COLORS)]
        )

        normalized_team = dict(team)
        normalized_team["color"] = team.get("color") or default_color
        normalized_teams[normalized_team["name"]] = normalized_team

    return normalized_teams


def build_color_palette(settings: dict) -> tuple[str, str, str, str, str]:
    custom_colors = settings.get("colors", {})

    bg_color = str(
        custom_colors.get("bg_color", None)
        or custom_colors.get("background_color", None)
        or "#D6BE96"
    )
    outer_bg_color = str(
        custom_colors.get("outer_bg_color", None)
        or custom_colors.get("outer_background_color", None)
        or bg_color
        or "#D6BE96"
    )
    fg_color = str(
        custom_colors.get("fg_color", None)
        or custom_colors.get("foreground_color", None)
        or "#99876C"
    )
    line_color = str(custom_colors.get("line_color", None) or fg_color)
    border_color = str(custom_colors.get("border_color", None) or fg_color)

    return bg_color, outer_bg_color, fg_color, line_color, border_color


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

        settings, items = parse_map_payload(data)

        grid_size = settings["grid_size"]
        game_mode_name = settings["game_mode"]
        teams = normalize_team_colors(settings["teams"])
        constraints = settings.get("constraints", {})

        game_mode = create_game_mode(game_mode_name)

        grid_params = None
        if game_mode.uses_grid:
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

            should_recompute = constraints and any(
                key in constraints for key in recompute_keys
            )

            if should_recompute:
                grid_params = compute_grid_params(
                    grid_size=grid_size, constraints=constraints
                )
            else:
                grid_params = pre_computed_grid_params.get(grid_size)
                if grid_params is None:
                    grid_params = compute_grid_params(
                        grid_size=grid_size, constraints=constraints
                    )

        bg_color, outer_bg_color, fg_color, line_color, border_color = (
            build_color_palette(settings)
        )

        invalid_colors = []
        colors = [bg_color, fg_color, line_color, border_color]

        for color in colors:
            try:
                ImageColor.getrgb(color)
            except (ValueError, TypeError):
                invalid_colors.append(color)

        if invalid_colors:
            msg = f"Invalid colors provided: {', '.join(invalid_colors)}"
            raise ValueError(msg)

        image = Image.new("RGBA", (IMG_SIZE, IMG_SIZE), bg_color)
        draw = ImageDraw.Draw(image)

        context = GameModeContext(
            items=items,
            grid_size=grid_size,
            grid_params=grid_params,
            teams=teams,
            textures_dir=TEXTURES_DIR,
            image=image,
            draw=draw,
            line_color=line_color,
            border_color=border_color,
        )

        render_result = game_mode.render(context)
        bingo = game_mode.check_win(context, render_result)

        center_board = constraints.get("center_board", True)
        if (
            center_board
            and render_result.used_width
            and render_result.used_width != IMG_SIZE
        ):
            offset = (IMG_SIZE - render_result.used_width) // 2
            image = image.crop(
                (0, 0, render_result.used_width, render_result.used_width)
            )
            new_canvas = Image.new("RGBA", (IMG_SIZE, IMG_SIZE), outer_bg_color)
            new_canvas.paste(image, (offset, offset))
            image = new_canvas

        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"map_url": f"/public/{filename}", "bingo": bingo}), 201

    except Exception as e:
        return jsonify({"imggen": str(e)}), 500


if __name__ == "__main__":
    port = int(os.environ.get("IMGGEN_PORT", 5000))
    app.run(host="0.0.0.0", port=port)
