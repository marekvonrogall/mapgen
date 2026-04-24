from typing import Any

from .base import GameModeContext, RenderResult
from .bingo import BingoGameMode


def is_bingo_still_possible(grid_size: int, owner_grid: list[list[str | None]]) -> bool:
    def line_is_viable(cells: list[tuple[int, int]]) -> bool:
        owners = set()
        for row, column in cells:
            owner = owner_grid[row][column]
            if owner is not None:
                owners.add(owner)
                if len(owners) > 1:
                    return False
        return True

    for r in range(grid_size):
        if line_is_viable([(r, c) for c in range(grid_size)]):
            return True

    for c in range(grid_size):
        if line_is_viable([(r, c) for r in range(grid_size)]):
            return True

    if line_is_viable([(i, i) for i in range(grid_size)]):
        return True

    if line_is_viable([(i, grid_size - i - 1) for i in range(grid_size)]):
        return True

    return False


def count_tiles(owner_grid: list[list[str | None]]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for row in owner_grid:
        for cell in row:
            if cell is None:
                continue
            if cell in counts:
                counts[cell] += 1
            else:
                counts[cell] = 1
    return counts


def teams_with_most_tiles(owner_grid: list[list[str | None]]) -> list[str] | None:
    counts = count_tiles(owner_grid)
    if not counts:
        return None

    max_count = max(counts.values())
    winners = []
    for team, count in counts.items():
        if count == max_count:
            winners.append(team)

    return winners or None


def team_with_majority(owner_grid: list[list[str | None]]) -> str | None:
    counts = count_tiles(owner_grid)

    total = 0
    for count in counts.values():
        total += count

    for team, count in counts.items():
        if count > total / 2:
            return team

    return None


def detect_win_condition_lockout(
    items: list[dict[str, Any]],
    grid_size: int,
) -> list[str] | None:
    owner_grid: list[list[str | None]] = [
        [None for _ in range(grid_size)] for _ in range(grid_size)
    ]

    for item in items:
        if item["row"] + 1 > grid_size or item["column"] + 1 > grid_size:
            continue

        row = item["row"]
        col = item["column"]

        if "completed" not in item:
            continue

        for team, completed in item["completed"].items():
            if completed:
                if owner_grid[row][col] is None:
                    owner_grid[row][col] = team
                break

    if all(cell is not None for row in owner_grid for cell in row):
        return teams_with_most_tiles(owner_grid)

    if not is_bingo_still_possible(grid_size, owner_grid):
        majority = team_with_majority(owner_grid)
        if majority:
            return [majority]

    return None


class LockoutGameMode(BingoGameMode):
    def check_win(
        self, context: GameModeContext, render_result: RenderResult
    ) -> list[str] | None:
        bingo_winner = super().check_win(context, render_result)
        if bingo_winner:
            return bingo_winner

        return detect_win_condition_lockout(context.items, context.grid_size)
