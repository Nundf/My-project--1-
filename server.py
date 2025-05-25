import json
import websocket
import cv2
import numpy as np
from ultralytics import YOLO
import threading
import queue

# Загрузка модели YOLO
model = YOLO('runs/detect/train25/weights/best.pt')

frame_queue = queue.Queue(maxsize=1)
output_frame = None
lock = threading.Lock()

# Подключение для отправки статуса обнаружения машины
ws_status = websocket.WebSocket()
ws_status.connect("ws://localhost:8080/CarStatus")

def process_yolo():
    global output_frame
    last_status = None
    while True:
        if not frame_queue.empty():
            img = frame_queue.get()
            results = model(img, verbose=False)[0]

            car_detected = False

            for class_id, box in zip(results.boxes.cls.cpu().numpy(), results.boxes.xyxy.cpu().numpy().astype(np.int32)):
                x1, y1, x2, y2 = box
                class_name = results.names[int(class_id)]

                if class_name.lower() == "car":
                    car_detected = True

                cv2.rectangle(img, (x1, y1), (x2, y2), (0, 255, 255), 2)
                cv2.putText(img, class_name, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 2)

            # Отправляем статус в Unity только если изменился
            if car_detected != last_status:
                try:
                    if car_detected:
                        ws_status.send(json.dumps({"car_detected": True}))
                    else:
                        ws_status.send(json.dumps({"car_detected": False}))
                    last_status = car_detected
                except Exception as e:
                    print(f"Status sending error: {e}")

            with lock:
                output_frame = img.copy()

def display_frames():
    global output_frame
    while True:
        with lock:
            if output_frame is not None:
                cv2.imshow('CameraUnity', output_frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cv2.destroyAllWindows()

def on_message(ws, message):
    np_arr = np.frombuffer(message, np.uint8)
    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    if img is not None:
        img = cv2.resize(img, (512, 512))
        if not frame_queue.full():
            frame_queue.put(img)

def on_error(ws, error):
    print(f"Error: {error}")

def on_close(ws, close_status_code, close_msg):
    print(f"### Connection closed ### Status code: {close_status_code}, Message: {close_msg}")
    cv2.destroyAllWindows()

def on_open(ws):
    print("Connected to the server.")

if __name__ == "__main__":
    # Создаем потоки
    threading.Thread(target=process_yolo, daemon=True).start()
    threading.Thread(target=display_frames, daemon=True).start()

    # Отдельное подключение для получения видео с камеры
    ws = websocket.WebSocketApp("ws://localhost:8080/CameraStream",
                                on_open=on_open,
                                on_message=on_message,
                                on_error=on_error,
                                on_close=on_close)

    ws.run_forever()