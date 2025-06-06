import json
import websocket
import cv2
import numpy as np
from ultralytics import YOLO
import threading
import queue
import requests
import time
import atexit

# Загрузка модели YOLO
model = YOLO('runs/detect/train25/weights/best.pt')

frame_queue = queue.Queue(maxsize=1)
output_frame = None
lock = threading.Lock()

ws_status = None  # глобально

# Логирование результатов
detection_log = []
frame_index = 0

def connect_ws_status(max_retries=20, delay=1):
    global ws_status
    ws_status = websocket.WebSocket()

    for attempt in range(max_retries):
        try:
            ws_status.connect("ws://localhost:8080/CarStatus")
            print("✅ Успешное подключение к Unity")
            return
        except Exception as e:
            print(f"[{attempt+1}/{max_retries}] Ожидание подключения к Unity...")
            time.sleep(delay)

    print("❌ Не удалось подключиться к Unity после нескольких попыток.")

def pause_game(pause: bool):
    try:
        ws_pause = websocket.WebSocket()
        ws_pause.connect("ws://localhost:8080/SceneControl")
        command = {"action": "pause" if pause else "resume"}
        ws_pause.send(json.dumps(command))
        ws_pause.close()
        save_detections_to_file()  # <- Сохраняем лог при паузе сцены
        print(f"[Pause] Команда {'pause' if pause else 'resume'} отправлена")
    except Exception as e:
        print(f"[Pause] Ошибка при отправке команды: {e}")

def resume_game():
    msg = {"action": "resume"}
    try:
        ws_status.send(json.dumps(msg))
    except Exception as e:
        print(f"Ошибка отправки команды продолжения: {e}")   

def start_dataset_collection(mode="start_dataset"):
    try:
        ws = websocket.WebSocket()
        ws.connect("ws://localhost:8080/SceneControl")
        command = {
            "action": "start_dataset",  # команда, которую Unity должен обработать
            "dataset": mode  # режим сбора датасета
        }
        ws.send(json.dumps(command))
        ws.close()
        print("[Dataset] Запущен режим сбора датасета")
    except Exception as e:
        print(f"[Dataset] Ошибка отправки команды: {e}")


def send_add_object_command(object_type="Cube", position=(0, 1, 0)):
    try:
        ws = websocket.WebSocket()
        ws.connect("ws://localhost:8080/SceneControl")
        command = {
            "action": "add_object",
            "object_type": object_type,
            "position": list(position) 
        }
        ws.send(json.dumps(command))
        ws.close()
        print(f"[Add] Объект {object_type} добавлен в позицию {position}")
    except Exception as e:
        print(f"[Add] Ошибка: {e}")

def update_object_position_in_unity(object_id="Cube_1", position=(2, 1, 0)):
    try:
        ws = websocket.WebSocket()
        ws.connect("ws://localhost:8080/SceneControl")
        command = {
            "action": "update",
            "id": object_id,
            "position": {"x": position[0], "y": position[1], "z": position[2]}
        }
        ws.send(json.dumps(command))
        ws.close()
        print(f"[Update] Объект {object_id} перемещён в позицию {position}")
    except Exception as e:
        print(f"[Update] Ошибка: {e}")

def remove_object_from_unity(object_id="Cube_1"):
    try:
        ws = websocket.WebSocket()
        ws.connect("ws://localhost:8080/SceneControl")
        command = {
            "action": "remove",
            "id": object_id
        }
        ws.send(json.dumps(command))
        ws.close()
        print(f"[Remove] Объект {object_id} удалён")
    except Exception as e:
        print(f"[Remove] Ошибка: {e}")


def send_scene_command(window):
    try:
        if ws_status:
            ws_status.send(json.dumps({"command": "stop_scene"}))
            print("[Python] Отправлена команда остановки сцены")
        else:
            print("[Python] WebSocket сцены не подключен")

        window.update_image(None)
        
    except Exception as e:
        print(f"Ошибка при остановке Unity-сцены: {e}")

def save_detections_to_file():
    try:
        with open("yolo_detections.json", "w") as f:
            json.dump(detection_log, f, indent=2)
        print("✅ Результаты YOLO сохранены в yolo_detections.json")
    except Exception as e:
        print(f"❌ Ошибка при сохранении результатов YOLO: {e}")

atexit.register(save_detections_to_file)

confidence_threshold = 0.50

def process_yolo():
    global output_frame, frame_index
    try:
        while True:
            if not frame_queue.empty():
                img = frame_queue.get()
                results = model(img, verbose=False)[0]

                best_conf = 0
                best_position = None
                car_detected = False
                best_box = None
                boxes_info = []

                for class_id, box, conf in zip(results.boxes.cls.cpu().numpy(),
                                               results.boxes.xyxy.cpu().numpy().astype(np.int32),
                                               results.boxes.conf.cpu().numpy()):
                    x1, y1, x2, y2 = box
                    class_name = results.names[int(class_id)]
                    confidence = float(conf)

                    if class_name.lower() == "car":
                        car_detected = True
                        boxes_info.append(((x1, y1, x2, y2), confidence))

                        if confidence > best_conf and confidence >= confidence_threshold:
                            best_conf = confidence
                            best_box = (x1, y1, x2, y2)
                            center_x = (x1 + x2) / 2
                            center_y = (y1 + y2) / 2
                            best_position = (float(center_x), float(center_y))

                for (x1, y1, x2, y2), confidence in boxes_info:
                    color = (0, 255, 0) if (x1, y1, x2, y2) == best_box else (0, 255, 255)
                    label = f"car: {confidence * 100:.1f}%"
                    cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
                    cv2.putText(img, label, (x1, y1 - 10),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

                msg = {
                    "car_detected": True,
                    "confidence": best_conf * 100,
                    "position": best_position
                } if car_detected else {"car_detected": False}

                try:
                    ws_status.send(json.dumps(msg))
                except Exception as e:
                    print(f"Status send error: {e}")

                # Сохраняем распознавания
                frame_data = {
                    "frame": frame_index,
                    "objects": []
                }

                if best_box is not None:
                    frame_data["objects"].append({
                                                    "class": "car",
                                                    "confidence": round(best_conf, 4),
                                                    "bbox": list(map(int, best_box))
                                                  })                   

                detection_log.append(frame_data)
                frame_index += 1

                with lock:
                    output_frame = img.copy()
    except Exception as e:
        print(f"[process_yolo] Ошибка: {e}")

def display_frames(window):
    global output_frame
    try:
        while True:
            with lock:
                if output_frame is not None:
                    window.update_image(output_frame.copy())

            time.sleep(0.03)
    except Exception as e:
        print(f"[display_frames] Ошибка: {e}")

    cv2.destroyAllWindows()

def on_message(ws, message):
    try:
        np_arr = np.frombuffer(message, np.uint8)
        img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
        if img is None:
            print("[on_message] Ошибка: изображение не декодировано")
            return

        img = cv2.resize(img, (1024, 1024))
        if not frame_queue.full():
            frame_queue.put(img)
    except Exception as e:
        print(f"[on_message] Ошибка: {e}")

def on_error(ws, error):
    print(f"Error: {error}")

def on_close(ws, close_status_code, close_msg):
    print(f"### Connection closed ### Status code: {close_status_code}, Message: {close_msg}")
    cv2.destroyAllWindows()

def on_open(ws):
    print("Connected to the server.")

def start_yolo_pipeline(window):
    connect_ws_status()
    threading.Thread(target=process_yolo, daemon=True).start()
    threading.Thread(target=lambda: display_frames(window), daemon=True).start()

    def start_camera_ws(max_retries=20, delay=1):
        for attempt in range(max_retries):
            try:
                ws_app = websocket.WebSocketApp(
                    "ws://localhost:8080/CameraStream",
                    on_open=on_open,
                    on_message=on_message,
                    on_error=on_error,
                    on_close=on_close)
                print("✅ Успешное подключение к CameraStream")
                ws_app.run_forever()
                send_add_object_command("Cube", (0, 1, 0))
                update_object_position_in_unity("Cube_1", (2, 1, 0))
                remove_object_from_unity("Cube_1")
                return
            except Exception as e:
                print(f"[{attempt+1}/{max_retries}] Ожидание подключения к CameraStream...")
                time.sleep(delay)

        print("❌ Не удалось подключиться к CameraStream")

    threading.Thread(target=start_camera_ws, daemon=True).start()
