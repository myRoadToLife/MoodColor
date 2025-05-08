using UnityEngine;
using UnityEditor;
using System.IO;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    #if UNITY_EDITOR
    public class IconGenerator
    {
        private const string ICONS_FOLDER = "Assets/App/Resources/UI/Icons";
        
        [MenuItem("MoodColor/Generate/UI Icons")]
        public static void GenerateIcons()
        {
            Debug.Log("🔄 Начинаем генерацию иконок UI...");
            
            // Убедимся, что папка существует
            if (!AssetDatabase.IsValidFolder(ICONS_FOLDER))
            {
                string[] folderParts = ICONS_FOLDER.Split('/');
                string currentPath = folderParts[0];
                
                for (int i = 1; i < folderParts.Length; i++)
                {
                    string newFolder = folderParts[i];
                    string checkPath = $"{currentPath}/{newFolder}";
                    
                    if (!AssetDatabase.IsValidFolder(checkPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, newFolder);
                        Debug.Log($"📁 Создана папка {checkPath}");
                    }
                    
                    currentPath = checkPath;
                }
            }
            
            // Создаем иконки
            CreateEmotionPlusIcon();
            CreateHistoryIcon();
            CreateFriendsIcon();
            CreateSettingsIcon();
            CreateWorkshopIcon();
            
            AssetDatabase.Refresh();
            Debug.Log("✅ Генерация иконок UI завершена");
        }
        
        private static void CreateEmotionPlusIcon()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color mainColor = new Color(1f, 0.6f, 0.2f, 1f); // Оранжевый
            Color bgColor = new Color(1f, 1f, 1f, 0f); // Прозрачный
            
            // Заполняем прозрачным
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            // Рисуем круг (эмоцию)
            int radius = 24;
            int centerX = texture.width / 2;
            int centerY = texture.height / 2;
            
            for (int y = centerY - radius; y < centerY + radius; y++)
            {
                for (int x = centerX - radius; x < centerX + radius; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            // Рисуем плюс
            int plusThickness = 6;
            int plusLength = 16;
            
            // Горизонтальная линия
            for (int y = centerY - plusThickness/2; y < centerY + plusThickness/2; y++)
            {
                for (int x = centerX - plusLength; x < centerX + plusLength; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }
            
            // Вертикальная линия
            for (int y = centerY - plusLength; y < centerY + plusLength; y++)
            {
                for (int x = centerX - plusThickness/2; x < centerX + plusThickness/2; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }
            
            texture.Apply();
            SaveTextureAsSprite(texture, "EmotionPlusIcon");
        }
        
        private static void CreateHistoryIcon()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color mainColor = new Color(0.3f, 0.6f, 1f, 1f); // Голубой
            Color bgColor = new Color(1f, 1f, 1f, 0f); // Прозрачный
            
            // Заполняем прозрачным
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            // Рисуем круг циферблата
            int radius = 24;
            int centerX = texture.width / 2;
            int centerY = texture.height / 2;
            
            for (int y = centerY - radius; y < centerY + radius; y++)
            {
                for (int x = centerX - radius; x < centerX + radius; x++)
                {
                    float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
                    if (distance <= radius && distance >= radius - 4)
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            // Рисуем стрелки
            int hourHandLength = 10;
            int minuteHandLength = 18;
            int thickness = 3;
            
            // Часовая стрелка (направлена на 10 часов)
            DrawLine(texture, centerX, centerY, 
                     (int)(centerX - hourHandLength * 0.7f), 
                     (int)(centerY + hourHandLength * 0.7f), 
                     thickness, mainColor);
            
            // Минутная стрелка (направлена на 2 часа)
            DrawLine(texture, centerX, centerY, 
                     (int)(centerX + minuteHandLength * 0.7f), 
                     (int)(centerY + minuteHandLength * 0.7f), 
                     thickness, mainColor);
            
            // Центральная точка
            for (int y = centerY - 3; y <= centerY + 3; y++)
            {
                for (int x = centerX - 3; x <= centerX + 3; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, 3))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            texture.Apply();
            SaveTextureAsSprite(texture, "HistoryIcon");
        }
        
        private static void CreateFriendsIcon()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color mainColor = new Color(0.2f, 0.8f, 0.4f, 1f); // Зеленый
            Color bgColor = new Color(1f, 1f, 1f, 0f); // Прозрачный
            
            // Заполняем прозрачным
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            // Рисуем две фигурки людей
            int headRadius = 8;
            int bodyHeight = 18;
            int shoulderWidth = 14;
            
            // Первая фигурка (слева)
            int figure1X = texture.width / 2 - 10;
            int figure1Y = texture.height / 2 + 5;
            
            // Голова
            for (int y = figure1Y - headRadius; y < figure1Y + headRadius; y++)
            {
                for (int x = figure1X - headRadius; x < figure1X + headRadius; x++)
                {
                    if (IsInCircle(x, y, figure1X, figure1Y, headRadius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            // Тело
            for (int y = figure1Y - headRadius - bodyHeight; y < figure1Y - headRadius; y++)
            {
                for (int x = figure1X - 2; x < figure1X + 3; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }
            
            // Плечи
            for (int y = figure1Y - headRadius - 5; y < figure1Y - headRadius; y++)
            {
                for (int x = figure1X - shoulderWidth/2; x < figure1X + shoulderWidth/2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }
            
            // Вторая фигурка (справа)
            int figure2X = texture.width / 2 + 10;
            int figure2Y = texture.height / 2 + 5;
            
            // Голова
            for (int y = figure2Y - headRadius; y < figure2Y + headRadius; y++)
            {
                for (int x = figure2X - headRadius; x < figure2X + headRadius; x++)
                {
                    if (IsInCircle(x, y, figure2X, figure2Y, headRadius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            // Тело
            for (int y = figure2Y - headRadius - bodyHeight; y < figure2Y - headRadius; y++)
            {
                for (int x = figure2X - 2; x < figure2X + 3; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }
            
            // Плечи
            for (int y = figure2Y - headRadius - 5; y < figure2Y - headRadius; y++)
            {
                for (int x = figure2X - shoulderWidth/2; x < figure2X + shoulderWidth/2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }
            
            texture.Apply();
            SaveTextureAsSprite(texture, "FriendsIcon");
        }
        
        private static void CreateSettingsIcon()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color mainColor = new Color(0.8f, 0.8f, 0.85f, 1f); // Серебристый
            Color bgColor = new Color(1f, 1f, 1f, 0f); // Прозрачный
            
            // Заполняем прозрачным
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            // Рисуем шестеренку
            int centerX = texture.width / 2;
            int centerY = texture.height / 2;
            int outerRadius = 24;
            int innerRadius = 16;
            int teethCount = 8;
            
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float angle = Mathf.Atan2(y - centerY, x - centerX);
                    float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
                    
                    // Выступы шестеренки
                    float toothAngle = (angle + Mathf.PI) * teethCount / (2 * Mathf.PI);
                    float toothSize = (Mathf.Abs(toothAngle % 1 - 0.5f) * 2) * 8;
                    
                    if (distance <= outerRadius + toothSize && distance >= innerRadius)
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }
            
            // Центральное отверстие
            for (int y = centerY - 8; y < centerY + 8; y++)
            {
                for (int x = centerX - 8; x < centerX + 8; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, 8))
                    {
                        texture.SetPixel(x, y, bgColor);
                    }
                }
            }
            
            texture.Apply();
            SaveTextureAsSprite(texture, "SettingsIcon");
        }
        
        private static void CreateWorkshopIcon()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color mainColor = new Color(0.9f, 0.5f, 0.2f, 1f); // Медный оттенок
            Color bgColor = new Color(1f, 1f, 1f, 0f); // Прозрачный
            
            // Заполняем прозрачным
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            int centerX = texture.width / 2;
            int centerY = texture.height / 2;
            
            // Рисуем молоток
            int hammerHeadWidth = 16;
            int hammerHeadHeight = 10;
            int hammerHandleThickness = 4;
            int hammerHandleLength = 30;
            
            // Рукоятка молотка
            for (int y = centerY - hammerHandleLength/2; y < centerY + hammerHandleLength/2; y++)
            {
                for (int x = centerX - hammerHandleThickness/2; x < centerX + hammerHandleThickness/2; x++)
                {
                    // Наклон влево
                    int adjustedX = x - (int)((y - centerY + hammerHandleLength/2) * 0.3f);
                    if (adjustedX >= 0 && adjustedX < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(adjustedX, y, mainColor);
                    }
                }
            }
            
            // Голова молотка
            for (int y = centerY + hammerHandleLength/2 - hammerHeadHeight; y < centerY + hammerHandleLength/2; y++)
            {
                for (int x = centerX - hammerHeadWidth/2; x < centerX + hammerHeadWidth/2; x++)
                {
                    // Наклон влево
                    int adjustedX = x - (int)((y - centerY + hammerHandleLength/2) * 0.3f) - 10;
                    if (adjustedX >= 0 && adjustedX < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(adjustedX, y, mainColor);
                    }
                }
            }
            
            // Рисуем гаечный ключ
            int wrenchLength = 30;
            int wrenchThickness = 4;
            int wrenchHeadSize = 10;
            
            // Рукоятка ключа
            for (int y = centerY - wrenchLength/2; y < centerY + wrenchLength/2; y++)
            {
                for (int x = centerX - wrenchThickness/2; x < centerX + wrenchThickness/2; x++)
                {
                    // Наклон вправо
                    int adjustedX = x + (int)((y - centerY + wrenchLength/2) * 0.3f) + 5;
                    if (adjustedX >= 0 && adjustedX < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(adjustedX, y, mainColor);
                    }
                }
            }
            
            // Голова ключа (верхняя)
            for (int y = centerY + wrenchLength/2 - wrenchHeadSize; y < centerY + wrenchLength/2; y++)
            {
                for (int x = centerX - wrenchHeadSize; x < centerX + wrenchHeadSize; x++)
                {
                    // Наклон вправо
                    int adjustedX = x + (int)((y - centerY + wrenchLength/2) * 0.3f) + 5;
                    if (adjustedX >= 0 && adjustedX < texture.width && y >= 0 && y < texture.height)
                    {
                        float distance = Mathf.Sqrt(Mathf.Pow(adjustedX - (centerX + 15), 2) + Mathf.Pow(y - (centerY + wrenchLength/2 - wrenchHeadSize/2), 2));
                        if (distance <= wrenchHeadSize && distance >= wrenchHeadSize - 4)
                        {
                            texture.SetPixel(adjustedX, y, mainColor);
                        }
                    }
                }
            }
            
            texture.Apply();
            SaveTextureAsSprite(texture, "WorkshopIcon");
        }
        
        private static bool IsInCircle(int x, int y, int centerX, int centerY, int radius)
        {
            float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
            return distance <= radius;
        }
        
        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                for (int ty = -thickness/2; ty <= thickness/2; ty++)
                {
                    for (int tx = -thickness/2; tx <= thickness/2; tx++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
                
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
        
        private static void SaveTextureAsSprite(Texture2D texture, string name)
        {
            string filePath = $"{ICONS_FOLDER}/{name}.png";
            
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            
            // Настраиваем импорт как спрайт
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.filterMode = FilterMode.Bilinear;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            }
            
            Debug.Log($"🎨 Сохранена иконка {name} в {filePath}");
        }
    }
    #endif
} 