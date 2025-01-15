from flask import Flask, jsonify, request
from PIL import Image, ImageDraw
import os
import uuid

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def exists(value):
    return value is not None and value != False

@app.route('/generate', methods=['POST'])
def generate_image():
    try:
        data = request.get_json()

        grid_size = data.get("grid_size", 5)
        gamemode = data.get("gamemode", "1P") 
        items = data.get("items", [])

        # Image dimensions and colors
        img_size = 128
        bg_color = (214, 190, 150)  # Light Beige
        line_color = (153, 135, 108)  # Dark Beige
        outline_color = (153, 135, 108)  # Dark Beige

        # Team colors
        team_color1 = (100, 255, 100) # Green
        team_color2 = (100, 255, 255) # Blue
        team_color3 = (255, 255, 100) # Yellow
        team_color4 = (255, 100, 100) # Red
        
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

            completionData = item.get("completed", [])
            completed_teams = []

            if "completed" in item:
                for completed_item in item["completed"]:
                    for key, value in completed_item.items():
                        if value:
                            completed_teams.append(key)
            else:
                completed_teams = None

            # Path to texture
            texture_folder = os.path.join(TEXTURES_DIR, texture_type)
            texture_path = os.path.join(texture_folder, f"{texture_name}.png")
            
            # Check if texture exists
            if not os.path.exists(texture_path):
                return jsonify({"error": f"Texture {texture_name}.png not found"}), 404
            
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
                    print(completed_team)
                    match gamemode:
                        case "1P":
                            rectColor = None
                            match completed_team:
                                case "team1":
                                    rectColor = team_color1
                                case "team2":
                                    rectColor = team_color2
                                case "team3":
                                    rectColor = team_color3
                                case "team4":
                                    rectColor = team_color4
                            draw.rectangle([cell_x, cell_y, cell_x + cell_size -1, cell_y + cell_size -1], outline=rectColor, width=padding)
            

        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"url": f"/public/{filename}", "cell_size": cell_size}), 201

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
