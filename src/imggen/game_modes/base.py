from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Any

from PIL import Image, ImageDraw


@dataclass
class GameModeContext:
    items: list[dict[str, Any]]
    grid_size: int
    grid_params: dict[str, int] | None
    teams: dict[str, dict[str, Any]]
    textures_dir: str
    image: Image.Image
    draw: ImageDraw.ImageDraw
    line_color: str
    border_color: str


@dataclass
class RenderResult:
    image: Image.Image
    used_width: int | None


class GameMode(ABC):
    uses_grid: bool = True

    @abstractmethod
    def render(self, context: GameModeContext) -> RenderResult:
        pass

    @abstractmethod
    def check_win(
        self, context: GameModeContext, render_result: RenderResult
    ) -> list[str] | None:
        pass
