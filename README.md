# mapgen

This project creates 128x128 pixel images of bingo boards for use in a Minecraft game mode called "Bingo."
Generation is affected by various inputs, such as grid size and difficulty.
This project is part of the [Bingo-Gamemode-Plugin](https://github.com/manueljonasgreub/Bingo-Gamemode-Plugin) by [@manueljonasgreub](https://www.github.com/manueljonasgreub) and [@marekvonrogall](https://www.github.com/marekvonrogall).

## Bingo Map Generation

### Request

HTTP POST: Call the `create` endpoint to genereate a new bingo map.

Allowed parameters:
- `grid_size`: (optional)

  Specify the grid size of the bingo board. Default is 5 (for a 5x5 bingo board).

- `game_mode`: (optional)

  Specify the gamemode of the bingo game. Default is "1P". Valid inputs: "1P", "2P", "3P", "4P".

- `team_names`: (required)

  Specify which teams are playing the bingo game. It is required to have as many teams as specified by the gamemode.

- `difficulty`: (optional)

  Specify the difficulty of the bingo board. Default is "easy". Valid inputs: "very easy", "easy", "medium", "hard", "very hard".

Example request:

```http://167.99.130.136/create/```
With RequestBody (JSON):

```json
{
  "grid_size": 5,
  "game_mode": "3P",
  "team_names": "custom_team_name1,custom_team_name2,custom_team_name3",
  "difficulty": "easy"
}
```

### Response

Our example request will generate an easy bingo board with a gridSize of 5.

The response body will look like this:

```json
{
    "mapURL": "/public/5613f476-587d-4d59-b30c-803a0f2b96ff.png",
    "mapRAW": {
        "settings": {
            "grid_size": 5,
            "game_mode": "3P",
            "teams": [
                "team1": {
                    "name": "custom_team_name1",
                    "placement": "bottom"
                },
                "team2": {
                    "name": "custom_team_name2",
                    "placement": "top-right"
                },
                "team3": {
                    "name": "custom_team_name3",
                    "placement": "top-left"
                }
            ]
        },
        "items": [
            {
                "row": 0,
                "column": 0,
                "type": "block",
                "name": "dark_oak_log",
                "difficulty": "very easy",
                "completed": {
                    "custom_team_name1": false,
                    "custom_team_name2": false,
                    "custom_team_name3": false
                }
            },
            {
                // MORE ITEMS HERE DEPENDING ON GRID SIZE.
            }
        ]
    }
}
```

`mapURL`: Returns the generated bingo board as image (128x128 pixels).

<img src="http://167.99.130.136/public/5613f476-587d-4d59-b30c-803a0f2b96ff.png" width="250" height="250" alt="Bingo map">

`mapRAW`: Returns the generated bingo board as raw json.
  - `settings`:
    Contains the bingo board settings.
      - `grid_size`: Specified grid size.
      - `game_mode`: Specified game mode.
      - `teams`: Specifies the name (`name`) and the placement (`placement`) for each team.

        You may consider changing these placements depending on gamemode. Valid inputs are: "top", "bottom", "right", "left", "top-left", "top-right", "bottom-left", "bottom-left".
        Modifying these placements will change where the team item/block completion is rendered on a completed item/block cell.

  - `items`:
    Contains the item or block for each cell on the bingo board.
      - `row`: Specifies the row of an item/block on the bingo board.
      - `column`: Specifies the column of an item/block on the bingo board.
      - `type`: Specifies the type (block/item) of an item/block on the bingo board.
      - `name`: Specifies the name of an item/block on the bingo board.
      - `difficulty`: Specifies the difficulty of an item/block on the bingo board.
      - `completed`: Specifies the completion status of an item/block on the bingo board.

        Each team participating at the bingo game should be listet in the `completed` section of an `item`. If a team has successfully aquired the item/block, the value of said team must be changed to true.

## Bingo Map item/block Completion

The project offers the possibility to generate a bingo board based on raw map data.
This is useful to mark items/blocks as completed, by drawing a rectangle or a corner around the item.

The Minecraft Plugin running the actual bingo game needs to keep track of the completion status of each item.

If a team aquired an item/block a a new map can be generated.
Should a team aquire all items of a vertical column, horizontal row or diagonal line, the map will draw a line accordingly. In this case, the response body will also state which team won the bingo game.

### Request

HTTP POST: Call the `update` endpoint to genereate a new bingo map.

Example request:

```http://167.99.130.136/update/```
With RequestBody (JSON): --> Use modified `mapRAW` data returned from `create` endpoint.

### Response

Map update response without bingo:
```json
{
    "bingo": null,
    "url": "/public/bfade128-5d9b-4353-a2a3-c59e9ca94e9b.png"
}
```

Map update response with bingo:
```json
{
    "bingo": "custom_team_name3",
    "url": "/public/612e0248-e4cf-496b-9b89-79940a3038ee.png"
}
```

Map update response images (without and with bingo):
<div style="display: flex;">
  <img src="http://167.99.130.136/public/bfade128-5d9b-4353-a2a3-c59e9ca94e9b.png" width="250" height="250" alt="Bingo map">
  <img src="http://167.99.130.136/public/612e0248-e4cf-496b-9b89-79940a3038ee.png" width="250" height="250" alt="Bingo map">
</div>
