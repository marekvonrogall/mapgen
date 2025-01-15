FROM python:3.12-slim

WORKDIR /app

RUN pip install flask pillow

COPY . /app/
COPY textures /app/textures/

EXPOSE 5000

CMD ["python", "generator.py"]