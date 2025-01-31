from flask import Flask, jsonify, request
from PIL import Image, ImageDraw
import os
import uuid

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def exists(value):
    return value != None and value != False and value != "null"

def detectBingo(grid_size, items, draw, cell_size, outline_width, line_width, team_names, team_colors, padding):
    # Grid for each team
    grid = {team: [[False] * grid_size for _ in range(grid_size)] for team in team_names.values()}

    # Check for item/block completion
    for item in items:
        row = item['row']
        column = item['column']

        if "completed" in item:
            for team, value in item.get("completed", {}).items():
                if value and team in grid:
                    grid[team][row][column] = True

    def calculate_cell_coordinates(row, column):
        x = int(column * cell_size + outline_width + (line_width * column))
        y = int(row * cell_size + outline_width + (line_width * row))
        return x, y

    # Check for bingo
    for _, team_name in team_names.items():
        team_grid = grid[team_name]
        team_color = team_colors[team_name]

        # Check rows and columns
        for i in range(grid_size):
            if all(team_grid[i]):  # Row bingo
                start_x, start_y = calculate_cell_coordinates(i, 0)
                end_x, end_y = calculate_cell_coordinates(i, grid_size - 1)
                drawBingoLine(draw, start_x, start_y, end_x, end_y, cell_size, team_color, padding)
                return team_name
            if all(row[i] for row in team_grid):  # Column bingo
                start_x, start_y = calculate_cell_coordinates(0, i)
                end_x, end_y = calculate_cell_coordinates(grid_size - 1, i)
                drawBingoLine(draw, start_x, start_y, end_x, end_y, cell_size, team_color, padding)
                return team_name

        # Check diagonals
        if all(team_grid[i][i] for i in range(grid_size)):  # Top-left to bottom-right
            start_x, start_y = calculate_cell_coordinates(0, 0)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, grid_size - 1)
            drawBingoLine(draw, start_x, start_y, end_x, end_y, cell_size, team_color, padding)
            return team_name
        if all(team_grid[i][grid_size - i - 1] for i in range(grid_size)):  # Top-right to bottom-left
            start_x, start_y = calculate_cell_coordinates(0, grid_size - 1)
            end_x, end_y = calculate_cell_coordinates(grid_size - 1, 0)
            drawBingoLine(draw, start_x, start_y, end_x, end_y, cell_size, team_color, padding)
            return team_name

    return None  # No bingo detected

def drawBingoLine(draw, start_cell_x, start_cell_y, end_cell_x, end_cell_y, cell_size, color, padding):
    draw.line([(start_cell_x + (cell_size / 2), start_cell_y + (cell_size / 2)), (end_cell_x + (cell_size / 2), end_cell_y + (cell_size / 2))], fill=color, width=padding)

def drawLine(draw, type, cell_x, cell_y, cell_size, color, padding): # Apply offset here aswell (gridsize 5 works fine, 3 is off)
    match type:
        case "top-left":
            draw.line([(cell_x, cell_y+1), (cell_x + (cell_size/2) -1, cell_y +1)], fill=color, width=padding)
            draw.line([(cell_x+1, cell_y), (cell_x +1, cell_y + (cell_size/2) -1)], fill=color, width=padding)
        case "top-right":
            draw.line([(cell_x + (cell_size/2), cell_y+1), (cell_x + cell_size -1, cell_y+1)], fill=color, width=padding)
            draw.line([(cell_x + cell_size -2, cell_y), (cell_x + cell_size -2, cell_y + (cell_size/2) -1)], fill=color, width=padding)
        case "bottom-left":
            draw.line([(cell_x, cell_y + cell_size -2), (cell_x + (cell_size/2) -1, cell_y + cell_size -2)], fill=color, width=padding)
            draw.line([(cell_x+1, cell_y + (cell_size/2)), (cell_x+1, cell_y + cell_size -1)], fill=color, width=padding)
        case "bottom-right":
            draw.line([(cell_x + (cell_size/2), cell_y + cell_size -2), (cell_x + cell_size -1, cell_y + cell_size -2)], fill=color, width=padding)
            draw.line([(cell_x + cell_size -2, cell_y + (cell_size/2)), (cell_x + cell_size -2, cell_y + cell_size -2)], fill=color, width=padding)

@app.route('/generate', methods=['POST'])
def generate_image():
    try:
        data = request.get_json()

        settings = data.get("settings", {})
        teams = settings.get("teams", [])
        items = data.get("items", [])
        
        gamemode = settings.get("game_mode", "1P")
        grid_size = settings.get("grid_size", 5)

        default_team_names = ["team1", "team2", "team3", "team4"]
        team_names = {}
        team_placements = {}

        for i in range(len(teams)):
            team = teams[i]
            custom_name = team.get("name", default_team_names[i])
            team_names[default_team_names[i]] = custom_name
            team_placements[custom_name] = team.get("placement", None)

        team_colors = {
            team_names.get("team1", "team1"): (100, 255, 100),
            team_names.get("team2", "team2"): (100, 255, 255),
            team_names.get("team3", "team3"): (255, 255, 100),
            team_names.get("team4", "team4"): (255, 100, 100)
        }

        # Image dimensions and colors
        img_size = 128
        bg_color = (214, 190, 150)  # Light Beige
        line_color = (153, 135, 108)  # Dark Beige
        outline_color = (153, 135, 108)  # Dark Beige

        offset = 0 # Offset for grid lines, because its not always pixel perfect
        modifier = 3

        match grid_size:
            case 3:
                modifier = 5
                offset = 1
            case 4:
                modifier = 4
            case 5:
                modifier = 3
            case 6:
                modifier = 2
                offset = -1
            case 7:
                modifier = 2
                offset = -1
        
        line_width = modifier
        outline_width = modifier
        padding = modifier

        # Create the base image
        image = Image.new("RGBA", (img_size, img_size), bg_color)
        draw = ImageDraw.Draw(image)

        # Calculate the size of each grid cell
        cell_size = (img_size - (line_width * (grid_size - 1)) - (outline_width * 2)) / grid_size

        # Grid lines
        for i in range(grid_size - 1):
            # Vertical lines                                          v --> i+1 because it appears to draw to the left of coordinate
            x = outline_width + ((i+1) * cell_size) + (line_width * i+1)
            draw.line([(x+offset, 0), (x+offset, img_size - 1)], fill=line_color, width=line_width)

            # Horizontal lines
            y = outline_width + ((i+1) * cell_size) + (line_width * i+1)
            draw.line([(0, y+offset), (img_size - 1, y+offset)], fill=line_color, width=line_width)

        # Outline
        draw.rectangle([0, 0, img_size - 1, img_size - 1], outline=outline_color, width=outline_width)

        # Add images from textures
        for item in items:
            row = item['row']
            column = item['column']
            texture_type = item['type']
            texture_name = item['name']

            completed_teams = []

            if "completed" in item:
                completed_teams = [team for team, value in item.get("completed", {}).items() if value]

            # Path to texture
            texture_folder = os.path.join(TEXTURES_DIR, texture_type)
            texture_path = os.path.join(texture_folder, f"{texture_name}.png")
            
            # Check if texture exists
            if not os.path.exists(texture_path):
                return jsonify({"error": f"Texture {texture_name}.png not found (Searching in {texture_path} for texture type {texture_type})."}), 404
            
            # Open texture
            texture_image = Image.open(texture_path)
            
            # Convert to RGBA
            if texture_image.mode != "RGBA":
                texture_image = texture_image.convert("RGBA")

            # Calculate texture position on grid
            cell_x = int(column * cell_size + outline_width + (line_width * column))
            cell_y= int(row * cell_size + outline_width + (line_width * row))

            x0 = cell_x + padding
            y0 = cell_y + padding
            #x1 = x0 + texture_image.width
            #y1 = y0 + texture_image.height

            # Might stretch texture, but ensures good styling
            x1 = x0 + int(cell_size - 2*padding)
            y1 = y0 + int(cell_size - 2*padding)
            texture_image = texture_image.resize((x1 - x0, y1 - y0))
            
            # Paste texture on map image
            image.paste(texture_image, (x0, y0), texture_image)

            # Item / Block completion
            if (exists(completed_teams)):
                for completed_team in completed_teams:
                    rectColor = team_colors.get(completed_team)
                    placement = team_placements.get(completed_team)

                    if not rectColor or not placement:
                        return jsonify({"error": f"Invalid team key entered ({completed_team} in 'completed' section of {texture_type}/{texture_name} [row {row}, column {column}])."}), 404
                    
                    match gamemode:
                        case "1P":
                            if placement == "full":
                                draw.rectangle([cell_x, cell_y, cell_x + cell_size -1, cell_y + cell_size -1], outline=rectColor, width=padding)
                        case "2P":
                            match placement:
                                case "top":
                                    drawLine(draw, "top-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "top-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case "bottom":
                                    drawLine(draw, "bottom-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case "left":
                                    drawLine(draw, "top-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-left", cell_x, cell_y, cell_size, rectColor, padding)
                                case "right":
                                    drawLine(draw, "top-right", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case _:
                                    if placement != None:
                                        return jsonify({"error": f"Invalid placement key entered ({placement} in 'placements' section of 'settings')."}), 404
                        case "3P":
                            match placement:
                                case "top":
                                    drawLine(draw, "top-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "top-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case "bottom":
                                    drawLine(draw, "bottom-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case "left":
                                    drawLine(draw, "top-left", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-left", cell_x, cell_y, cell_size, rectColor, padding)
                                case "right":
                                    drawLine(draw, "top-right", cell_x, cell_y, cell_size, rectColor, padding)
                                    drawLine(draw, "bottom-right", cell_x, cell_y, cell_size, rectColor, padding)
                                case _:
                                    drawLine(draw, placement, cell_x, cell_y, cell_size, rectColor, padding)
                        case "4P":
                            drawLine(draw, placement, cell_x, cell_y, cell_size, rectColor, padding)
                        case _:
                            return jsonify({"error": f"Invalid gamemode key entered ({gamemode} in 'settings' section)."}), 404
        
        # Detect and draw bingo
        bingo_result = detectBingo(grid_size, items, draw, cell_size, outline_width, line_width, team_names, team_colors, padding)
        
        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"url": f"/public/{filename}", "bingo": bingo_result}), 201

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
