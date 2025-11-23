# mapgen

This project creates 128x128px images of bingo boards for use in a Minecraft game mode called "Bingo". Generation is affected by various inputs, such as grid size, item type, and item difficulty.
This project is part of the [Bingo-Gamemode-Plugin](https://github.com/manueljonasgreub/Bingo-Gamemode-Plugin) by [@manueljonasgreub](https://www.github.com/manueljonasgreub) and [@marekvonrogall](https://www.github.com/marekvonrogall).

Special thanks to [@cubicmetre](https://github.com/cubicmetre) - this project uses a modified version of their 1.21.10 [items.json](https://github.com/cubicmetre/mis-builder/blob/23407007701d5c108ef44a024166e257f9dc54da/items.json) file.

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

- `max_per_group_or_material`: (optional)

  Specify the max amount of items per bingo board that belong to the same group / material. Default is 2.

Example request:

```https://api.vrmarek.me/create/```
With RequestBody (JSON):

```json
{
  "grid_size": 5,
  "game_mode": "3P",
  "team_names": "custom_team_name1,custom_team_name2,custom_team_name3",
  "difficulty": "medium",
  "max_per_group_or_material": 1
}
```

> [!IMPORTANT]  
> The API endpoint 'api.vrmarek.me' is provided for demonstration purposes only and is not intended for use in production environments. You can try it out [here](https://vrmarek.me/#mapgen-demo).
> If you wish to create your own Bingo game mode in Minecraft using this API, you can deploy the [following packages](https://github.com/marekvonrogall?tab=packages&repo_name=mapgen) on your own server.
> Please be aware that 'api.vrmarek.me' may be renamed or discontinued in the future.

### Response

Our example request will generate a medium bingo board with a gridSize of 5.

The response body will look like this:

```json
{
    "mapURL": "/public/348e2a68-5a33-48b8-adcb-b94b5457c78c.png",
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
                "id": "nether_brick_stairs",
                "name": "Nether Brick Stairs",
                "sprite": "sprites/nether_brick_stairs.png",
                "difficulty": "medium",
                "completed": {
                    "custom_team_name1": false,
                    "custom_team_name2": false,
                    "custom_team_name3": false
                }
            },
            <MORE_ITEMS_HERE>
        ]
    }
}
```

`mapURL`: Returns the generated bingo board as image (128x128 pixels).

<img src="https://github.com/user-attachments/assets/ed794ce8-7c2e-44e5-8ee0-952d0b969128" width="250" height="250" alt="Bingo map">

`mapRAW`: Returns the generated bingo board as raw json.
  - `settings`:
    Contains the bingo board settings.
      - `grid_size`: Specified grid size.
      - `game_mode`: Specified game mode.
      - `teams`: Specifies the name (`name`) and the placement (`placement`) for each team.

        You may consider changing these placements depending on gamemode. Valid inputs are: "top", "bottom", "right", "left", "top-left", "top-right", "bottom-left", "bottom-left".
        Modifying these placements will change where the team item completion is rendered on a completed item cell.

  - `items`:
    Contains the item or block for each cell on the bingo board.
      - `row`: Specifies the row of an item on the bingo board.
      - `column`: Specifies the column of an item on the bingo board.
      - `id`: The in-game id of the generated item.
      - `name`: The in-game name of the generated item.
      - `sprite`: Path to the icon texture.
      - `difficulty`: How hard it is to obtain this item.
      - `completed`: Completion status of an item on the bingo board.

        Each team participating at the bingo game should be listet in the `completed` section of an `item`. If a team has successfully aquired an item, the value of said team must be changed to true.

## Bingo Map Item Completion

The project offers the possibility to generate a bingo board based on raw map data.
This is useful to mark items as completed, by drawing a rectangle or a corner around the item.

The Minecraft Plugin running the actual bingo game needs to keep track of the completion status of each item.

If a team aquired an item the map can be updated using the `update` endpoint.
Should a team aquire all items of a vertical column, horizontal row or diagonal line, the map will draw a line accordingly. In this case, the response body will also state which team won the bingo game.

### Request

HTTP POST: Call the `update` endpoint to genereate a new bingo map.

Example request:

```https://api.vrmarek.me/update/```
With RequestBody (JSON): --> Use modified `mapRAW` data returned from `create` endpoint.

```json
{
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
      "id": "nether_brick_stairs",
      "name": "Nether Brick Stairs",
      "sprite": "sprites/nether_brick_stairs.png",
      "difficulty": "medium",
      "completed": {
        "custom_team_name1": true, <--- Set to true if a team found the item
        "custom_team_name2": true,
        "custom_team_name3": false
      }
    },
    <MORE_ITEMS_HERE>
  ]
}
```

> [!IMPORTANT]  
> The API endpoint 'api.vrmarek.me' is provided for demonstration purposes only and is not intended for use in production environments. You can try it out [here](https://vrmarek.me/#mapgen-demo).
> If you wish to create your own Bingo game mode in Minecraft using this API, you can deploy the [following packages](https://github.com/marekvonrogall?tab=packages&repo_name=mapgen) on your own server.
> Please be aware that 'api.vrmarek.me' may be renamed or discontinued in the future.

### Response

Map update response without bingo:
```json
{
    "bingo": null,
    "url": "/public/fa0abc0d-ed21-4b1f-943e-155d749f818a.png"
}
```

Map update response with bingo:
```json
{
    "bingo": "custom_team_name2",
    "url": "/public/e2f2f5f3-f6cb-45cd-9450-f6629acc9c19.png"
}
```

Map update response images (without and with bingo):
<div style="display: flex;">
  <img src="https://github.com/user-attachments/assets/3162d26f-a42d-473a-8940-2203c862105b" width="250" height="250" alt="Bingo map">
  <img src="https://github.com/user-attachments/assets/71425703-af37-4a5d-8695-e87b4f8fe890" width="250" height="250" alt="Bingo map (Bingo)">
</div>
