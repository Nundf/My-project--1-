from ultralytics import YOLO
import cv2
import json

def detect_and_show_video(input_video="C:/Users/aleks/Downloads/depositphotos_774281756-stock-video-new-york-city-new-york.mp4", output_json="yolo_detections.json", model_path="runs/detect/train25/weights/best.pt"):
    model = YOLO(model_path)
    cap = cv2.VideoCapture(input_video)

    if not cap.isOpened():
        print(f"❌ Не удалось открыть видео: {input_video}")
        return

    detections = []
    frame_num = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        results = model(frame)[0]
        frame_detections = []

        for box in results.boxes:
            cls_id = int(box.cls[0])
            cls_name = model.names[cls_id]
            conf = float(box.conf[0])

            # Отображаем все объекты (без фильтра по conf)
            if cls_name == "car":
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                label = f'car {conf:.2f}'
                cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                cv2.putText(frame, label, (x1, y1 - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

                # Но сохраняем только с уверенностью >= 0.5
                if conf >= 0.5:
                    frame_detections.append({
                        "class": "car",
                        "confidence": conf,
                        "bbox": [x1, y1, x2, y2]
                    })

        # Добавляем в JSON, если что-то найдено (с conf >= 0.5)
        if frame_detections:
            detections.append({
                "frame": frame_num,
                "objects": frame_detections
            })

        # Отображение
        cv2.imshow("YOLOv8 — Распознавание машин", frame)
        if cv2.waitKey(1) & 0xFF == 27:  # Esc
            break

        frame_num += 1

    cap.release()
    cv2.destroyAllWindows()

    with open(output_json, "w", encoding="utf-8") as f:
        json.dump(detections, f, ensure_ascii=False, indent=2)

    print(f"✅ Детекции с уверенностью ≥ 50% сохранены в {output_json}")

if __name__ == "__main__":
    detect_and_show_video()
