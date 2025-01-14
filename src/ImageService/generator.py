from flask import Flask, jsonify, request
from PIL import Image, ImageDraw
import os
import uuid

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
TEXTURES_DIR = "/app/textures"
os.makedirs(OUTPUT_DIR, exist_ok=True)

@app.route('/generate', methods=['POST'])
def generate_image():
    try:
        data = request.get_json()

        grid_size = data.get("grid_size", 5)  # use 5 by default
        items = data.get("items", [])

        # Image dimensions and colors
        img_size = 128
        bg_color = (214, 190, 150)  # Light Beige
        line_color = (153, 135, 108)  # Dark Beige
        line_width = 3
        outline_width = 4
        padding = 4 # Team color padding between line and texture

        # Create the base image
        image = Image.new("RGBA", (img_size, img_size), bg_color)
        draw = ImageDraw.Draw(image)

        # Calculate the size of each grid cell
        cell_size = (img_size - (line_width * (grid_size - 1)) - (outline_width * 2)) / grid_size

        # Grid lines
        for i in range(grid_size + 1):
            # Vertical lines
            x = round(i * cell_size) + outline_width + (line_width * i-1)
            draw.line([(x, 0), (x, img_size - 1)], fill=line_color, width=line_width)

            # Horizontal lines
            y = round(i * cell_size) + outline_width + (line_width * i-1)
            draw.line([(0, y), (img_size - 1, y)], fill=line_color, width=line_width)

        # Outline
        draw.rectangle([0, 0, img_size - 1, img_size - 1], outline=line_color, width=outline_width)
        
        # Add images from textures
        for item in items:
            row = item['row']
            column = item['column']
            texture_type = item['type']
            texture_name = item['name']
            
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

            # Resize texture
            texture_image = texture_image.resize((int(cell_size - 2*padding), int(cell_size - 2*padding)))

            # Calculate texture position on grid
            x0 = round(column * cell_size) + outline_width + (line_width * column) + padding
            y0 = round(row * cell_size) + outline_width + (line_width * row) + padding
            x1 = x0 + texture_image.width
            y1 = y0 + texture_image.height
            #x1 = x0 + (cell_size - 2*padding) # Might stretch texture, but ensures good styling
            #y1 = y0 + (cell_size - 2*padding) # Might stretch texture, but ensures good styling

            # Paste texture on map image
            image.paste(texture_image, (x0, y0), texture_image)

        # Save image
        filename = f"{uuid.uuid4()}.png"
        filepath = os.path.join(OUTPUT_DIR, filename)
        image.save(filepath)

        # Return URL
        return jsonify({"url": f"/public/{filename}"}), 201

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
