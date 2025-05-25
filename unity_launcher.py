import subprocess
import os

def launch_unity():
    unity_path = r"C:\Users\aleks\6000.0.40f1\Editor\Unity.exe"
    project_path = r"C:\Users\aleks\My project (1)"
    scene_path = r"Assets\CubexCube - Free City Pack I\Scenes\Free_City_Scene_I.unity"

    subprocess.Popen([
        unity_path,
        "-projectPath", project_path,
        "-openScenes", scene_path,
        "-executeMethod", "SceneLoader.LoadFromPython"
    ])

if __name__ == "__main__":
    launch_unity()
