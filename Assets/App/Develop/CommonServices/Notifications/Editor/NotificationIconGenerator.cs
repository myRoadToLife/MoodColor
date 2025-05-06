using UnityEngine;
using UnityEditor;
using System.IO;

namespace App.Develop.CommonServices.Notifications.Editor
{
    public class NotificationIconGenerator : EditorWindow
    {
        // Настройки размеров иконок
        private const int SMALL_ICON_SIZE = 24;
        private const int LARGE_ICON_SIZE = 48;
        
        // Настройки цветов иконок по категориям
        private static readonly Color SystemColor = new Color(0.2f, 0.6f, 1f);
        private static readonly Color ReminderColor = new Color(1f, 0.8f, 0.2f);
        private static readonly Color ActivityColor = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color AchievementColor = new Color(0.8f, 0.3f, 1f);
        private static readonly Color PromotionColor = new Color(1f, 0.4f, 0.2f);
        private static readonly Color UpdateColor = new Color(0.5f, 0.5f, 0.5f);
        
        // Пути сохранения
        private const string ANDROID_DRAWABLE_PATH = "Assets/Plugins/Android/res/drawable";
        private const string RESOURCES_ICONS_PATH = "Assets/Resources/Icons/Notifications";
        
        [MenuItem("Tools/MoodColor/Generate Notification Icons")]
        public static void ShowWindow()
        {
            GetWindow<NotificationIconGenerator>("Notification Icons");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Генератор иконок уведомлений", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Сгенерировать Android иконки"))
            {
                GenerateAndroidIcons();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Сгенерировать иконки категорий"))
            {
                GenerateCategoryIcons();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Сгенерировать все иконки"))
            {
                GenerateAndroidIcons();
                GenerateCategoryIcons();
            }
        }
        
        private void GenerateAndroidIcons()
        {
            // Создаем директорию, если не существует
            if (!Directory.Exists(ANDROID_DRAWABLE_PATH))
            {
                Directory.CreateDirectory(ANDROID_DRAWABLE_PATH);
            }
            
            // Создаем маленькую иконку для уведомлений
            Texture2D smallIcon = CreateCircleTexture(SMALL_ICON_SIZE, Color.white);
            SaveTextureToFile(smallIcon, Path.Combine(ANDROID_DRAWABLE_PATH, "notification_small_icon.png"));
            
            // Создаем большую иконку для уведомлений
            Texture2D largeIcon = CreateCircleTexture(LARGE_ICON_SIZE, Color.white);
            SaveTextureToFile(largeIcon, Path.Combine(ANDROID_DRAWABLE_PATH, "notification_large_icon.png"));
            
            Debug.Log("Android notification icons generated successfully");
            AssetDatabase.Refresh();
        }
        
        private void GenerateCategoryIcons()
        {
            // Создаем директорию, если не существует
            if (!Directory.Exists(RESOURCES_ICONS_PATH))
            {
                Directory.CreateDirectory(RESOURCES_ICONS_PATH);
            }
            
            // Генерируем иконки для всех категорий уведомлений
            foreach (NotificationCategory category in System.Enum.GetValues(typeof(NotificationCategory)))
            {
                Color iconColor = GetColorForCategory(category);
                int iconSize = 32; // Стандартный размер иконки категории
                
                Texture2D iconTexture = CreateIconForCategory(category, iconSize, iconColor);
                string iconPath = Path.Combine(RESOURCES_ICONS_PATH, $"{category}.png");
                SaveTextureToFile(iconTexture, iconPath);
            }
            
            Debug.Log("Category notification icons generated successfully");
            AssetDatabase.Refresh();
        }
        
        private Color GetColorForCategory(NotificationCategory category)
        {
            switch (category)
            {
                case NotificationCategory.System:
                    return SystemColor;
                case NotificationCategory.Reminder:
                    return ReminderColor;
                case NotificationCategory.Activity:
                    return ActivityColor;
                case NotificationCategory.Achievement:
                    return AchievementColor;
                case NotificationCategory.Promotion:
                    return PromotionColor;
                case NotificationCategory.Update:
                    return UpdateColor;
                default:
                    return Color.gray;
            }
        }
        
        private Texture2D CreateCircleTexture(int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Заполняем текстуру прозрачным цветом
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            texture.SetPixels(pixels);
            
            // Рисуем круг
            float radius = size * 0.4f;
            float centerX = size * 0.5f;
            float centerY = size * 0.5f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        private Texture2D CreateIconForCategory(NotificationCategory category, int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Заполняем текстуру прозрачным цветом
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            texture.SetPixels(pixels);
            
            switch (category)
            {
                case NotificationCategory.System:
                    DrawInfoIcon(texture, size, color);
                    break;
                case NotificationCategory.Reminder:
                    DrawClockIcon(texture, size, color);
                    break;
                case NotificationCategory.Activity:
                    DrawActivityIcon(texture, size, color);
                    break;
                case NotificationCategory.Achievement:
                    DrawAchievementIcon(texture, size, color);
                    break;
                case NotificationCategory.Promotion:
                    DrawPromotionIcon(texture, size, color);
                    break;
                case NotificationCategory.Update:
                    DrawUpdateIcon(texture, size, color);
                    break;
                default:
                    DrawCircleIcon(texture, size, color);
                    break;
            }
            
            texture.Apply();
            return texture;
        }
        
        private void DrawInfoIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем кружок
            DrawCircleIcon(texture, size, color);
            
            // Рисуем букву "i"
            int centerX = size / 2;
            int centerY = size / 2;
            int thickness = Mathf.Max(1, size / 16);
            
            // Точка
            for (int y = centerY - size/5; y < centerY - size/8; y++)
            {
                for (int x = centerX - thickness; x <= centerX + thickness; x++)
                {
                    if (IsInBounds(x, y, size))
                        texture.SetPixel(x, y, Color.white);
                }
            }
            
            // Палочка
            for (int y = centerY; y < centerY + size/4; y++)
            {
                for (int x = centerX - thickness; x <= centerX + thickness; x++)
                {
                    if (IsInBounds(x, y, size))
                        texture.SetPixel(x, y, Color.white);
                }
            }
        }
        
        private void DrawClockIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем круг - часы
            DrawCircleIcon(texture, size, color);
            
            int centerX = size / 2;
            int centerY = size / 2;
            int radius = size / 3;
            
            // Рисуем стрелки часов
            // Часовая стрелка
            DrawLine(texture, centerX, centerY, centerX, centerY - radius/2, Color.white);
            
            // Минутная стрелка
            DrawLine(texture, centerX, centerY, centerX + radius/2, centerY, Color.white);
        }
        
        private void DrawActivityIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем фон
            DrawCircleIcon(texture, size, color);
            
            int centerX = size / 2;
            int centerY = size / 2;
            int width = size / 2;
            int height = size / 4;
            
            // Рисуем график активности
            DrawLine(texture, centerX - width/2, centerY, centerX - width/4, centerY - height, Color.white);
            DrawLine(texture, centerX - width/4, centerY - height, centerX, centerY + height/2, Color.white);
            DrawLine(texture, centerX, centerY + height/2, centerX + width/4, centerY - height/2, Color.white);
            DrawLine(texture, centerX + width/4, centerY - height/2, centerX + width/2, centerY, Color.white);
        }
        
        private void DrawAchievementIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем фон
            DrawCircleIcon(texture, size, color);
            
            int centerX = size / 2;
            int centerY = size / 2;
            int starSize = size / 3;
            
            // Рисуем звезду для достижения (упрощенно)
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 2 * Mathf.PI / 5 - Mathf.PI / 2;
                int tipX = centerX + Mathf.RoundToInt(starSize * Mathf.Cos(angle));
                int tipY = centerY + Mathf.RoundToInt(starSize * Mathf.Sin(angle));
                
                DrawLine(texture, centerX, centerY, tipX, tipY, Color.white);
            }
        }
        
        private void DrawPromotionIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем фон
            DrawCircleIcon(texture, size, color);
            
            int centerX = size / 2;
            int centerY = size / 2;
            int tagSize = size / 3;
            
            // Рисуем ценник
            int tagTopX = centerX;
            int tagTopY = centerY - tagSize;
            int tagBottomLeftX = centerX - tagSize;
            int tagBottomRightX = centerX + tagSize;
            int tagBottomY = centerY + tagSize/2;
            
            DrawLine(texture, tagTopX, tagTopY, tagBottomLeftX, tagBottomY, Color.white);
            DrawLine(texture, tagTopX, tagTopY, tagBottomRightX, tagBottomY, Color.white);
            DrawLine(texture, tagBottomLeftX, tagBottomY, tagBottomRightX, tagBottomY, Color.white);
        }
        
        private void DrawUpdateIcon(Texture2D texture, int size, Color color)
        {
            // Рисуем фон
            DrawCircleIcon(texture, size, color);
            
            int centerX = size / 2;
            int centerY = size / 2;
            int arrowSize = size / 3;
            
            // Рисуем стрелку обновления (по кругу)
            for (float angle = 0; angle < 1.5f * Mathf.PI; angle += 0.1f)
            {
                int x = centerX + Mathf.RoundToInt(arrowSize * Mathf.Cos(angle));
                int y = centerY + Mathf.RoundToInt(arrowSize * Mathf.Sin(angle));
                
                if (IsInBounds(x, y, size))
                    texture.SetPixel(x, y, Color.white);
            }
            
            // Рисуем наконечник стрелки
            int tipX = centerX;
            int tipY = centerY - arrowSize;
            DrawLine(texture, tipX, tipY, tipX + arrowSize/3, tipY + arrowSize/3, Color.white);
            DrawLine(texture, tipX, tipY, tipX - arrowSize/3, tipY + arrowSize/3, Color.white);
        }
        
        private void DrawCircleIcon(Texture2D texture, int size, Color color)
        {
            int centerX = size / 2;
            int centerY = size / 2;
            int radius = size / 3;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int size = texture.width;
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                if (IsInBounds(x0, y0, size))
                    texture.SetPixel(x0, y0, color);
                
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        
        private bool IsInBounds(int x, int y, int size)
        {
            return x >= 0 && x < size && y >= 0 && y < size;
        }
        
        private void SaveTextureToFile(Texture2D texture, string filePath)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"Saved texture to: {filePath}");
        }
    }
} 