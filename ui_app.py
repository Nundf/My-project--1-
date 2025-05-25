import sys
import subprocess
from PyQt5.QtWidgets import (
    QApplication, QWidget, QPushButton, QVBoxLayout, QHBoxLayout,
    QLabel, QComboBox, QListWidget
)
from PyQt5.QtCore import Qt
from yolo_stream import start_dataset_collection, start_yolo_pipeline
from PyQt5.QtGui import QImage, QPixmap
from yolo_stream import pause_game, resume_game, send_scene_command, send_add_object_command

class ImageDropLabel(QLabel):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setAcceptDrops(True)
        self.setStyleSheet("background-color: black; color: white; padding: 10px;")
    
    def dragEnterEvent(self, event):
        if event.mimeData().hasText():
            event.acceptProposedAction()
    
    def dropEvent(self, event):
        object_type = event.mimeData().text()
        pos = event.pos()
        x, y = pos.x(), pos.y()

        scene_width, scene_height = 10,10
        label_width = self.width()
        label_height = self.height()    

        scene_x = (x / label_width) * scene_width - scene_width / 2
        scene_z = (y / label_height) * scene_height - scene_height / 2
        scene_y = 1

        send_add_object_command(object_type, (scene_x, scene_y, scene_z))

def launch_ui():
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app.exec_())

class MainWindow(QWidget):
    def __init__(self):
        super().__init__()

        self.setStyleSheet("""
            QPushButton {font-size: 18px;}
            QLabel {font-size: 16px;}
            QComboBox {font-size: 16px;}
            QListWidget {font-size: 16px;}
            """)

        self.setWindowTitle("UI запуск симуляции")

        # Кнопки запуска и управления
        self.start_button = QPushButton("Запуск")
        self.start_button.clicked.connect(self.start_simulation)

        # Кнопки для управления паузой и продолжением
        self.pause_button = QPushButton("Пауза")
        self.pause_button.clicked.connect(lambda: pause_game(True))

        self.resume_button = QPushButton("Продолжить")
        self.resume_button.clicked.connect(lambda: pause_game(False))

        # Кнопка остановки сцены
        self.stop_button = QPushButton("Остановить сцену")
        self.stop_button.clicked.connect(self.stop_scene)

        self.dataset_button = QPushButton("Запустить сбор датасета")
        self.dataset_button.clicked.connect(lambda: start_dataset_collection("start_dataset"))
     

        # Выбор камеры и сцены
        self.scene_combo = QComboBox()
        self.scene_combo.addItems(["Сцена 1", "Сцена 2", "Сцена 3"])

        self.camera_combo = QComboBox()
        self.camera_combo.addItems(["Камера 1", "Камера 2", "Камера 3"])

        # Панель объектов
        self.objects_list = QListWidget()
        self.objects_list.setDragEnabled(True)
        self.objects_list.addItems(["Куб", "Сфера", "Машина"])
        self.objects_list.itemClicked.connect(self.add_object)

        for i in range(self.objects_list.count()):
            item = self.objects_list.item(i)
            item.setData(Qt.UserRole, item.text())
            item.setFlags(item.flags() | Qt.ItemIsDragEnabled)

        self.image_label = ImageDropLabel()
        self.image_label.setAlignment(Qt.AlignCenter)
        self.image_label.setText("Окно камеры будет здесь")
        self.image_label.setFixedSize(960,720)

        # # Центр для изображения (пока заглушка)
        # self.image_label = QLabel("Окно камеры будет здесь")
        # self.image_label.setAlignment(Qt.AlignCenter)
        # self.image_label.setStyleSheet("background-color: black; color: white; padding: 10px;")

        # Layout
        layout = QHBoxLayout()
        left_panel = QVBoxLayout()
        left_panel.addWidget(QLabel("Выбор сцены"))
        left_panel.addWidget(self.scene_combo)
        left_panel.addWidget(QLabel("Выбор камеры"))
        left_panel.addWidget(self.camera_combo)
        left_panel.addStretch()

        right_panel = QVBoxLayout()
        right_panel.addWidget(QLabel("Объекты"))
        right_panel.addWidget(self.objects_list)
        right_panel.addStretch()

        center_panel = QVBoxLayout()
        center_panel.addWidget(self.image_label)
        center_panel.addWidget(self.start_button)
        center_panel.addWidget(self.pause_button)
        center_panel.addWidget(self.resume_button)
        center_panel.addWidget(self.stop_button)
        center_panel.addWidget(self.dataset_button)

        layout.addLayout(left_panel, 1)
        layout.addLayout(center_panel, 2)
        layout.addLayout(right_panel, 1)

        self.setLayout(layout)
        self.setMinimumSize(1400, 1000)
        self.window = self

    def pause_game(self):
        print("Пауза игры...")
        pause_game()

    def resume_game(self):
        print("Продолжение игры...")
        resume_game()

    def start_simulation(self):
        print("Запуск симуляции...")
        subprocess.Popen(["python", "unity_launcher.py"])

        start_yolo_pipeline(self)

    def stop_scene(self):
        print("Остановка сцены...")
        send_scene_command("stop")

    def start_dataset_collection(self):
        print("Запуск сбора датасета...")
        start_dataset_collection("start_dataset")

    def add_object(self, item):
        obj_type = item.text().lower()
        type_map = {
            "Куб": "Cube",
            "Сфера": "Sphere",
            "Машина": "Car"
        }
        obj_type = type_map.get(obj_type, "Cube")

        position = (0, 1, 0)  # Позиция по умолчанию
        send_add_object_command(obj_type, position)

    def update_image(self, frame):
        # Обновление изображения на QLabel.
        height, width, channel = frame.shape
        bytes_per_line = 3 * width
        q_img = QImage(frame.data, width, height, bytes_per_line, QImage.Format_BGR888)
        pixmap = QPixmap.fromImage(q_img)
        self.image_label.setPixmap(pixmap)  
