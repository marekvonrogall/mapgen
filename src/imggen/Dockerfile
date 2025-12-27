FROM python:3.12-slim

WORKDIR /app

RUN pip install flask pillow gunicorn

COPY . /app/
COPY textures /app/textures/

EXPOSE 5000

CMD ["gunicorn", "-w", "2", "-b", "0.0.0.0:5000", "--timeout", "60", "--access-logfile", "-", "--error-logfile", "-", "generator:app"]