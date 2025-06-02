# %%
import json
import os

# Определяем данные для кнопок настроения
mood_button_data = [
    {"id": "happy", "text": "Счастливый", "color_hex": "#FFD700", "icon_path": "Icons/HappyIcon"},
    {"id": "calm", "text": "Спокойный", "color_hex": "#87CEEB", "icon_path": "Icons/CalmIcon"},
    {"id": "sad", "text": "Грустный", "color_hex": "#6A5ACD", "icon_path": "Icons/SadIcon"},
    {"id": "energetic", "text": "Энергичный", "color_hex": "#FF4500", "icon_path": "Icons/EnergeticIcon"},
    {"id": "relaxed", "text": "Расслабленный", "color_hex": "#32CD32", "icon_path": "Icons/RelaxedIcon"},
    # Добавьте больше настроений по мере необходимости
]

# %% [markdown]
"""
Сохраняем сгенерированные данные в JSON-файл в папке Unity Assets/Data. 
Убедитесь, что эта папка существует в вашем Unity проекте, или измените путь.
"""

# %%
output_dir = "Assets/Data"
output_file_path = os.path.join(output_dir, "mood_buttons.json")

os.makedirs(output_dir, exist_ok=True) # Убедимся, что папка существует

with open(output_file_path, "w", encoding="utf-8") as f:
    json.dump(mood_button_data, f, ensure_ascii=False, indent=4)

print(f"Данные кнопок настроения сохранены в: {output_file_path}") 