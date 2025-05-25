# from PyQt5.QtWidgets import (
#     QApplication, QWidget, QLabel, QPushButton, QVBoxLayout,
#     QHBoxLayout, QListWidget, QListWidgetItem, QComboBox
# )
# from PyQt5.QtGui import QPixmap, QImage, QIcon
# from PyQt5.QtCore import Qt, QSize
# import sys
# import numpy as np
# import cv2


# class UI(QWidget):
#     def __init__(self):
#         super().__init__()

#         self.setWindowTitle("Unity Simulation Control")
#         self.setGeometry(100, 100, 1000, 600)

#         layout = QVBoxLayout(self)

#         # Верхняя часть: списки сцен, камер, объектов и изображение
#         main_layout = QHBoxLayout()

#         # Левая панель: сцены и камеры
#         left_panel = QVBoxLayout()
#         left_panel.setContentsMargins(0, 0, 0, 0)  # Убираем отступы

#         # Сцена
#         self.scene_selector = QComboBox()
#         self.scene_selector.addItem("Сцена 1")
#         self.scene_selector.addItem("Сцена 2")
#         self.scene_selector.addItem("Сцена 3")

#         # Камера
#         self.camera_selector = QComboBox()
#         self.camera_selector.addItem("Камера 1")
#         self.camera_selector.addItem("Камера 2")
#         self.camera_selector.addItem("Камера 3")

#         # Добавление виджетов в левую панель
#         left_panel.addWidget(QLabel("Сцена:"))
#         left_panel.addWidget(self.scene_selector)
#         left_panel.addWidget(QLabel("Камера:"))
#         left_panel.addWidget(self.camera_selector)

#         # Центральная панель: изображение
#         self.image_label = QLabel()
#         self.image_label.setAlignment(Qt.AlignCenter)
#         self.image_label.setFixedSize(512, 512)  # размер под изображение

#         # Правая панель: объекты
#         right_panel = QVBoxLayout()
#         right_panel.setContentsMargins(0, 0, 0, 0)  # Убираем отступы
#         self.object_list = QListWidget()
#         self.object_list.setViewMode(QListWidget.IconMode)
#         self.object_list.setIconSize(QSize(64, 64))
#         self.object_list.setResizeMode(QListWidget.Adjust)

#         objects = {
#             "Cube": "icons/cube.png",
#             "Sphere": "icons/sphere.png",
#             "Car": "icons/car.png"
#         }
#         for name, icon_path in objects.items():
#             item = QListWidgetItem(QIcon(icon_path), name)
#             self.object_list.addItem(item)

#         right_panel.addWidget(QLabel("Объекты:"))
#         right_panel.addWidget(self.object_list)

#         # Сборка центральной части
#         main_layout.addLayout(left_panel)
#         main_layout.addWidget(self.image_label)
#         main_layout.addLayout(right_panel)

#         # Нижняя панель: кнопка
#         self.start_button = QPushButton("Запуск")
#         layout.addLayout(main_layout)
#         layout.addWidget(self.start_button)

#         # Устанавливаем одинаковую растяжку для левой и правой панели
#         main_layout.setStretch(0, 1)  # Левую панель растягиваем
#         main_layout.setStretch(1, 2)  # Центральную панель растягиваем больше
#         main_layout.setStretch(2, 1)  # Правую панель растягиваем
#         self.setLayout(layout)

#     def update_image(self, frame):
#         """Отображает OpenCV-кадр в QLabel."""
#         rgb_image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
#         h, w, ch = rgb_image.shape
#         bytes_per_line = ch * w
#         qt_image = QImage(rgb_image.data, w, h, bytes_per_line, QImage.Format_RGB888)
#         pixmap = QPixmap.fromImage(qt_image)
#         self.image_label.setPixmap(pixmap.scaled(self.image_label.size(), Qt.KeepAspectRatio))

#     def start_simulation(self):
#         print("Симуляция запущена")
#         # Можно сюда вставить команду отправки в Unity или запуска потока


# def run_ui():
#     app = QApplication(sys.argv)
#     window = UI()
#     window.show()
#     sys.exit(app.exec_())
