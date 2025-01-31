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

```https://api.vrmarek.me/create/```
With RequestBody (JSON):

```json
{
  "grid_size": 5,
  "game_mode": "3P",
  "team_names": "custom_team_name1,custom_team_name2,custom_team_name3",
  "difficulty": "easy"
}
```

> [!IMPORTANT]  
> The API endpoint 'api.vrmarek.me' is provided for demonstration purposes only and is not intended for use in production environments. You can try it out [here](https://vrmarek.me/#mapgen-demo).
> If you wish to create your own Bingo game mode in Minecraft using this API, you can deploy the [following packages](https://github.com/marekvonrogall?tab=packages&repo_name=mapgen) on your own server.
> Please be aware that 'api.vrmarek.me' may be renamed or discontinued in the future.

### Response

Our example request will generate an easy bingo board with a gridSize of 5.

The response body will look like this:

```json
{
    "mapURL": "/public/263158ea-3981-4001-ada1-4750e00ff799.png",
    "mapRAW": {
        "settings": {
            "grid_size": 5,
            "game_mode": "3P",
            "teams": [
                {
                    "name": "custom_team_name1",
                    "placement": "bottom"
                },
                {
                    "name": "custom_team_name2",
                    "placement": "top-right"
                },
                {
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
                "name": "yellow_wool",
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

<img src="https://api.vrmarek.me/public/263158ea-3981-4001-ada1-4750e00ff799.png" width="250" height="250" alt="Bingo map">

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

```https://api.vrmarek.me/update/```
With RequestBody (JSON): --> Use modified `mapRAW` data returned from `create` endpoint.

> [!IMPORTANT]  
> The API endpoint 'api.vrmarek.me' is provided for demonstration purposes only and is not intended for use in production environments. You can try it out [here](https://vrmarek.me/#mapgen-demo).
> If you wish to create your own Bingo game mode in Minecraft using this API, you can deploy the [following packages](https://github.com/marekvonrogall?tab=packages&repo_name=mapgen) on your own server.
> Please be aware that 'api.vrmarek.me' may be renamed or discontinued in the future.

### Response

Map update response without bingo:
```json
{
    "bingo": null,
    "url": "/public/ef0e72bc-bbe4-4f6d-8755-0c1f1c80b524.png"
}
```

Map update response with bingo:
```json
{
    "bingo": "custom_team_name1",
    "url": "/public/1752abad-b8b6-4e51-93d8-2c229dd8ef62.png"
}
```

Map update response images (without and with bingo):
<div style="display: flex;">
  <img src="https://api.vrmarek.me/public/ef0e72bc-bbe4-4f6d-8755-0c1f1c80b524.png" width="250" height="250" alt="Bingo map">
  <img src="https://api.vrmarek.me/public/1752abad-b8b6-4e51-93d8-2c229dd8ef62.png" width="250" height="250" alt="Bingo map">
</div>
