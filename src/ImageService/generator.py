from flask import Flask, jsonify
from PIL import Image, ImageDraw
import os
import uuid

app = Flask(__name__)

OUTPUT_DIR = "/app/public"
os.makedirs(OUTPUT_DIR, exist_ok=True)

@app.route('/generate', methods=['POST'])
def generate_image():
    try:
        # Image dimensions and colors
        img_size = 128
        grid_size = 5
        bg_color = (200, 200, 200)  # Light gray
        line_color = (105, 105, 105)  # Dark gray

        # Create the base image
        image = Image.new("RGBA", (img_size, img_size), bg_color)
        draw = ImageDraw.Draw(image)

        # Calculate the size of each grid cell
        cell_size = img_size / grid_size

        # Grid lines
        for i in range(grid_size + 1):
            # Vertical lines
            x = round(i * cell_size)
            draw.line([(x, 0), (x, img_size - 1)], fill=line_color)

            # Horizontal lines
            y = round(i * cell_size)
            draw.line([(0, y), (img_size - 1, y)], fill=line_color)

        # Outline
        draw.rectangle([0, 0, img_size - 1, img_size - 1], outline=line_color)

        # White squares
        padding = int(cell_size // 6)
        for row in range(grid_size):
            for col in range(grid_size):
                x0 = round(col * cell_size) + padding
                y0 = round(row * cell_size) + padding
                x1 = round((col + 1) * cell_size) - padding
                y1 = round((row + 1) * cell_size) - padding
                draw.rectangle([x0, y0, x1, y1], fill="white")

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
