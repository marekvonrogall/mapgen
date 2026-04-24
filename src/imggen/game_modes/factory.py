from .base import GameMode
from .bingo import BingoGameMode
from .lockout import LockoutGameMode


def create_game_mode(mode_name: str | None) -> GameMode:
    normalized_mode_name = (mode_name or "bingo").lower()

    modes: dict[str, type[GameMode]] = {
        "bingo": BingoGameMode,
        "lockout": LockoutGameMode,
    }

    game_mode_class = modes.get(normalized_mode_name, BingoGameMode)
    return game_mode_class()
