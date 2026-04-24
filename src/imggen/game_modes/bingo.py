import os
from typing import Any

from PIL import Image, ImageDraw

from .base import GameMode, GameModeContext, RenderResult


def draw_bingo_line(
    draw: ImageDraw.ImageDraw,
    start_cell_x: int,
    start_cell_y: int,
    end_cell_x: int,
    end_cell_y: int,
    cell_width: int,
    color: str,
    padding: int,
) -> None:
    cx1 = start_cell_x + cell_width // 2
    cy1 = start_cell_y + cell_width // 2
    cx2 = end_cell_x + cell_width // 2
    cy2 = end_cell_y + cell_width // 2

    draw.line(
        [(cx1, cy1), (cx2, cy2)],
        fill=color,
        width=padding,
    )

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
    draw: ImageDraw.ImageDraw,
    types: list[str],
    cell_x: int,
    cell_y: int,
    cell_width: int,
    color: str,
    padding: int,
) -> None:
    for line_type in types:
        match line_type:
            case "top-left":
                draw.polygon(
                    [
                        (cell_x, cell_y),
                        (cell_x + (cell_width // 2) - 1, cell_y),
                        (cell_x + (cell_width // 2) - 1, cell_y + padding - 1),
                        (cell_x, cell_y + padding - 1),
                    ],
                    fill=color,
                )
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
                draw.polygon(
                    [
                        (cell_x + (cell_width // 2), cell_y),
                        (cell_x + cell_width - 1, cell_y),
                        (cell_x + cell_width - 1, cell_y + padding - 1),
                        (cell_x + (cell_width // 2), cell_y + padding - 1),
                    ],
                    fill=color,
                )
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
                draw.polygon(
                    [
                        (cell_x, cell_y + cell_width - padding),
                        (cell_x + (cell_width // 2) - 1, cell_y + cell_width - padding),
                        (cell_x + (cell_width // 2) - 1, cell_y + cell_width - 1),
                        (cell_x, cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
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
                draw.polygon(
                    [
                        (cell_x + (cell_width // 2), cell_y + cell_width - padding),
                        (cell_x + cell_width - 1, cell_y + cell_width - padding),
                        (cell_x + cell_width - 1, cell_y + cell_width - 1),
                        (cell_x + (cell_width // 2), cell_y + cell_width - 1),
                    ],
                    fill=color,
                )
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


def detect_bingo(
    grid_size: int,
    items: list[dict[str, Any]],
    draw: ImageDraw.ImageDraw,
    grid_params: dict[str, int],
    teams: dict[str, dict[str, Any]],
) -> str | None:
    team_names = list(teams.keys())

    grid = {
        team: [[False] * grid_size for _ in range(grid_size)] for team in team_names
    }

    for item in items:
        if item["row"] + 1 > grid_size or item["column"] + 1 > grid_size:
            continue

        row = item["row"]
        column = item["column"]

        if "completed" in item:
            for team, value in item.get("completed", {}).items():
                if value and team in grid:
                    grid[team][row][column] = True

    def calculate_cell_coordinates(row: int, column: int) -> tuple[int, int]:
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

    for team_name in team_names:
        team_grid = grid[team_name]
        team = teams[team_name]

        team_color = str(team["color"])

        for i in range(grid_size):
            if all(team_grid[i]):
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

        if all(team_grid[i][i] for i in range(grid_size)):
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

    return None


class BingoGameMode(GameMode):
    def render(self, context: GameModeContext) -> RenderResult:
        if context.grid_params is None:
            msg = "Grid parameters are required for bingo rendering."
            raise ValueError(msg)

        used_width = (
            context.grid_params["cell_width"] * context.grid_size
            + context.grid_params["line_width"] * (context.grid_size - 1)
            + context.grid_params["border_width"] * 2
        )

        if context.grid_params["line_width"] > 0:
            for i in range(context.grid_size - 1):
                x = (
                    context.grid_params["border_width"]
                    + (i + 1) * context.grid_params["cell_width"]
                    + i * context.grid_params["line_width"]
                )
                context.draw.polygon(
                    [
                        (x, 0),
                        (x + context.grid_params["line_width"] - 1, 0),
                        (x + context.grid_params["line_width"] - 1, used_width - 1),
                        (x, used_width - 1),
                    ],
                    fill=context.line_color,
                )

                y = (
                    context.grid_params["border_width"]
                    + (i + 1) * context.grid_params["cell_width"]
                    + i * context.grid_params["line_width"]
                )
                context.draw.polygon(
                    [
                        (0, y),
                        (used_width - 1, y),
                        (used_width - 1, y + context.grid_params["line_width"] - 1),
                        (0, y + context.grid_params["line_width"] - 1),
                    ],
                    fill=context.line_color,
                )

        if context.grid_params["border_width"] > 0:
            context.draw.rectangle(
                (0, 0, used_width - 1, used_width - 1),
                outline=context.border_color,
                width=context.grid_params["border_width"],
            )

        for item in context.items:
            if (
                item["row"] + 1 > context.grid_size
                or item["column"] + 1 > context.grid_size
            ):
                continue

            row = item["row"]
            column = item["column"]
            texture_name = item["sprite"]

            completed_teams = []
            if "completed" in item:
                completed_teams = [
                    team for team, value in item.get("completed", {}).items() if value
                ]

            texture_path = os.path.join(context.textures_dir, f"{texture_name}")
            if not os.path.exists(texture_path):
                msg = f"Invalid texture {texture_name} provided."
                raise ValueError(msg)

            texture_image = Image.open(texture_path)
            if texture_image.mode != "RGBA":
                texture_image = texture_image.convert("RGBA")

            cell_x = context.grid_params["border_width"] + column * (
                context.grid_params["cell_width"] + context.grid_params["line_width"]
            )
            cell_y = context.grid_params["border_width"] + row * (
                context.grid_params["cell_width"] + context.grid_params["line_width"]
            )

            x0 = cell_x + context.grid_params["padding"]
            y0 = cell_y + context.grid_params["padding"]
            x1 = x0 + context.grid_params["asset_width"]
            y1 = y0 + context.grid_params["asset_width"]

            texture_image = texture_image.resize(
                (x1 - x0, y1 - y0), resample=Image.Resampling.NEAREST
            )
            context.image.paste(texture_image, (x0, y0), texture_image)

            if completed_teams:
                for completed_team in completed_teams:
                    team = context.teams.get(completed_team)
                    if team is None:
                        msg = f"Invalid team key entered ({completed_team} in 'completed' section of '{texture_name}' [row {row}, column {column}])."
                        raise ValueError(msg)

                    rect_color = str(team["color"])
                    placement = team["placement"]

                    if not rect_color or not placement:
                        msg = f"Invalid team key entered ({completed_team} in 'completed' section of '{texture_name}' [row {row}, column {column}])."
                        raise ValueError(msg)

                    types: list[str] = []
                    if context.grid_params["padding"] > 0:
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
                                context.draw.rectangle(
                                    (
                                        cell_x,
                                        cell_y,
                                        cell_x + context.grid_params["cell_width"] - 1,
                                        cell_y + context.grid_params["cell_width"] - 1,
                                    ),
                                    outline=rect_color,
                                    width=context.grid_params["padding"],
                                )
                            case _:
                                types.append(placement)

                        draw_line(
                            context.draw,
                            types,
                            cell_x,
                            cell_y,
                            context.grid_params["cell_width"],
                            rect_color,
                            context.grid_params["padding"],
                        )

        return RenderResult(image=context.image, used_width=used_width)

    def check_win(
        self, context: GameModeContext, render_result: RenderResult
    ) -> list[str] | None:
        if context.grid_params is None:
            return None

        winner = detect_bingo(
            context.grid_size,
            context.items,
            context.draw,
            context.grid_params,
            context.teams,
        )
        return [winner] if winner else None
