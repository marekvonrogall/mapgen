# mapgen

This project creates 128x128 pixel images of bingo boards for use in a Minecraft game mode called "Bingo."
Generation is affected by various inputs, such as grid size and difficulty.
This project is part of the [Bingo-Gamemode-Plugin](https://github.com/manueljonasgreub/Bingo-Gamemode-Plugin) by [@manueljonasgreub](https://www.github.com/manueljonasgreub) and [@marekvonrogall](https://www.github.com/marekvonrogall)

## Bingo Map Generation

### Request

HTTP POST: Call the `create` endpoint to genereate a new bingo map.

Allowed parameters:
- `gridSize`: (optional)

  Specify the grid size of the bingo board. Default is 5 (for a 5x5 bingo board).

- `gamemode`: (optional)

  Specify the gamemode of the bingo game. Default is "1P". Valid inputs: "1P", "2P", "3P", "4P".

- `teams`: (required)

  Specify which teams are playing the bingo game. It is required to have as many teams as specified by the gamemode.

- `difficulty`: (optional)

  Specify the difficulty of the bingo board. Default is "easy". Valid inputs: "very easy", "easy", "medium", "hard", "very hard".

Example request:

```http://167.99.130.136/create/?gridSize=5&gamemode=3P&teams=team1,team2,team4&difficulty=easy```

### Response

Our example request will generate an easy bingo board with a gridSize of 5.

The response body will look like this:

```json
{
    "mapURL": "/public/5613f476-587d-4d59-b30c-803a0f2b96ff.png",
    "mapRAW": {
        "settings": [
            {
                "grid_size": 5,
                "gamemode": "3P",
                "bingo": [
                    {
                        "start": "null",
                        "end": "null",
                        "team": "null"
                    }
                ],
                "placements": [
                    {
                        "team1": "bottom",
                        "team2": "top-right",
                        "team4": "top-left"
                    }
                ]
            }
        ],
        "items": [
            {
                "row": 0,
                "column": 0,
                "type": "block",
                "name": "dark_oak_log",
                "difficulty": "very easy",
                "completed": [
                    {
                        "team1": false,
                        "team2": false,
                        "team4": false
                    }
                ]
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
      - `gridSize`: Specified grid size.
      - `gamemode`: Specified game mode.
      - `bingo`: Specify if a team got a bingo by entering the start and end cell (format: row,column) of the bingo, aswell as the team that won the bingo game.
      - `placements`: Specifies team placements for item completion.

        You may consider changing these depending on gamemode. Valid inputs are: "top", "bottom", "right", "left", "top-left", "top-right", "bottom-left", "bottom-left".
        Changing these placements will change where the team item/block completion is rendered on a completed item/block cell.

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

HTTP POST: Call the `update` endpoint to genereate a new bingo map.

Example request: 
```http://167.99.130.136/update/```
With Body: raw (JSON) --> Use modified mapRAW data returned from `create` endpoint.

<div style="display: flex;">
  <img src="http://167.99.130.136/public/bfade128-5d9b-4353-a2a3-c59e9ca94e9b.png" width="250" height="250" alt="Bingo map">
  <img src="http://167.99.130.136/public/612e0248-e4cf-496b-9b89-79940a3038ee.png" width="250" height="250" alt="Bingo map">
</div>
