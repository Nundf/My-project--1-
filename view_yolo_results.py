import json
from tabulate import tabulate

def load_detections(json_path="yolo_detections.json"):
    try:
        with open(json_path, 'r') as f:
            return json.load(f)
    except Exception as e:
        print(f"❌ Ошибка загрузки: {e}")
        return []

def generate_table(detections):
    rows = []
    for frame_data in detections:
        frame = frame_data.get("frame")
        objects = frame_data.get("objects", [])
        for obj in objects:
            if obj.get("class") == "car":
                conf = obj.get("confidence")
                bbox = obj.get("bbox")
                if frame is not None and conf is not None and bbox is not None:
                    conf_percent = f"{conf * 100:.1f}%"
                    bbox_str = str(bbox)
                    rows.append([frame, conf_percent, bbox_str])
    return rows

def export_html_table(rows, out_file="yolo_table.html"):
    html = "<html><head><meta charset='utf-8'><title>YOLO Обнаружения</title></head><body>"
    html += "<h2>Таблица обнаруженных автомобилей</h2><table border='1' cellspacing='0' cellpadding='4'>"
    html += "<tr><th>Кадр</th><th>Уверенность</th><th>Координаты (bbox)</th></tr>"

    for row in rows:
        html += "<tr>" + "".join(f"<td>{cell}</td>" for cell in row) + "</tr>"

    html += "</table></body></html>"

    with open(out_file, "w", encoding="utf-8") as f:
        f.write(html)

    print(f"✅ HTML-таблица сохранена в {out_file}")

def main():
    detections = load_detections()
    if not detections:
        print("Нет данных для отображения.")
        return

    table_rows = generate_table(detections)
    if not table_rows:
        print("Нет обнаруженных автомобилей.")
        return

    print(tabulate(table_rows, headers=["Кадр", "Уверенность", "Координаты"], tablefmt="fancy_grid"))
    export_html_table(table_rows)

if __name__ == "__main__":
    main()
