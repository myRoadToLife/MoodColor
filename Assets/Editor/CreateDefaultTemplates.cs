using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateDefaultTemplates
{
    [MenuItem("Tools/Create Default Panel Templates")]
    public static void CreateDefaultPanelTemplates()
    {
        string templatePath = "Assets/Editor/PanelTemplates/";
        
        // Убедимся, что папка существует
        if (!Directory.Exists(templatePath))
        {
            Directory.CreateDirectory(templatePath);
        }

        // 1. MessageBox Template
        CreateMessageBoxTemplate(templatePath);
        
        // 2. Settings Panel Template
        CreateSettingsPanelTemplate(templatePath);
        
        // 3. Login Form Template
        CreateLoginFormTemplate(templatePath);
        
        // 4. Inventory Grid Template
        CreateInventoryGridTemplate(templatePath);
        
        AssetDatabase.Refresh();
        Debug.Log("Созданы готовые шаблоны панелей в " + templatePath);
    }

    private static void CreateMessageBoxTemplate(string path)
    {
        PanelTemplate template = ScriptableObject.CreateInstance<PanelTemplate>();
        
        template.panelName = "MessageBox";
        template.panelSize = new Vector2(400, 200);
        template.panelBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
        
        template.includeHeader = true;
        template.headerTitle = "Сообщение";
        template.headerHeight = 35f;
        template.headerBackgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        template.headerTextColor = Color.white;
        template.headerFontSize = 16;
        template.includeCloseButton = true;
        template.closeButtonColor = Color.white;
        
        template.includeFooter = true;
        template.footerHeight = 45f;
        template.footerBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        template.addOkCancelButtons = true;
        
        template.saveAsPrefab = true;
        template.prefabPath = "Assets/Prefabs/UI/MessageBoxes/";
        template.description = "Стандартное диалоговое окно для отображения сообщений пользователю с кнопками OK/Cancel";
        
        // Настройка Layout и UI элементов
        template.contentLayoutType = LayoutType.VerticalLayout;
        template.layoutSpacing = new Vector2(0, 10);
        template.layoutPadding = new RectOffset(20, 20, 15, 15);
        template.uiElements = new UIElementData[]
        {
            new UIElementData(UIElementType.Text) 
            { 
                elementName = "MessageText", 
                text = "Вы уверены, что хотите выполнить это действие?",
                fontSize = 14,
                textColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                textAlignment = TextAnchor.MiddleCenter
            }
        };
        
        string fullPath = Path.Combine(path, "MessageBox_Template.asset");
        AssetDatabase.CreateAsset(template, fullPath);
    }

    private static void CreateSettingsPanelTemplate(string path)
    {
        PanelTemplate template = ScriptableObject.CreateInstance<PanelTemplate>();
        
        template.panelName = "SettingsPanel";
        template.panelSize = new Vector2(600, 450);
        template.panelBackgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        
        template.includeHeader = true;
        template.headerTitle = "Настройки";
        template.headerHeight = 40f;
        template.headerBackgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        template.headerTextColor = Color.white;
        template.headerFontSize = 18;
        template.includeCloseButton = true;
        template.closeButtonColor = Color.white;
        
        template.includeFooter = true;
        template.footerHeight = 50f;
        template.footerBackgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        template.addOkCancelButtons = true;
        
        template.saveAsPrefab = true;
        template.prefabPath = "Assets/Prefabs/UI/Settings/";
        template.description = "Панель настроек с темным дизайном, подходящая для игровых настроек";
        
        // Настройка Layout и UI элементов
        template.contentLayoutType = LayoutType.VerticalLayout;
        template.layoutSpacing = new Vector2(0, 15);
        template.layoutPadding = new RectOffset(20, 20, 10, 10);
        template.uiElements = new UIElementData[]
        {
            new UIElementData(UIElementType.Text) 
            { 
                elementName = "SoundLabel", 
                text = "Громкость звука:",
                fontSize = 16,
                textColor = Color.white,
                textAlignment = TextAnchor.MiddleLeft
            },
            new UIElementData(UIElementType.Slider) 
            { 
                elementName = "SoundSlider",
                sizeDelta = new Vector2(400, 25),
                sliderValue = 0.8f,
                sliderMinValue = 0f,
                sliderMaxValue = 1f,
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                textColor = new Color(0.4f, 0.7f, 1f, 1f)
            },
            new UIElementData(UIElementType.Text) 
            { 
                elementName = "MusicLabel", 
                text = "Громкость музыки:",
                fontSize = 16,
                textColor = Color.white,
                textAlignment = TextAnchor.MiddleLeft
            },
            new UIElementData(UIElementType.Slider) 
            { 
                elementName = "MusicSlider",
                sizeDelta = new Vector2(400, 25),
                sliderValue = 0.6f,
                sliderMinValue = 0f,
                sliderMaxValue = 1f,
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                textColor = new Color(0.4f, 0.7f, 1f, 1f)
            },
            new UIElementData(UIElementType.Toggle) 
            { 
                elementName = "FullscreenToggle",
                text = "Полноэкранный режим",
                fontSize = 16,
                textColor = Color.white,
                toggleValue = true,
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f)
            }
        };
        
        string fullPath = Path.Combine(path, "SettingsPanel_Template.asset");
        AssetDatabase.CreateAsset(template, fullPath);
    }

    private static void CreateLoginFormTemplate(string path)
    {
        PanelTemplate template = ScriptableObject.CreateInstance<PanelTemplate>();
        
        template.panelName = "LoginForm";
        template.panelSize = new Vector2(350, 300);
        template.panelBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        
        template.includeHeader = true;
        template.headerTitle = "Вход в систему";
        template.headerHeight = 40f;
        template.headerBackgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        template.headerTextColor = Color.white;
        template.headerFontSize = 16;
        template.includeCloseButton = false; // Обычно формы входа не имеют кнопки закрытия
        
        template.includeFooter = true;
        template.footerHeight = 50f;
        template.footerBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        template.addOkCancelButtons = false; // У форм входа обычно свои кнопки
        
        template.saveAsPrefab = true;
        template.prefabPath = "Assets/Prefabs/UI/Forms/";
        template.description = "Форма входа в систему со светлым дизайном, без стандартных кнопок OK/Cancel";
        
        // Настройка Layout и UI элементов
        template.contentLayoutType = LayoutType.VerticalLayout;
        template.layoutSpacing = new Vector2(0, 10);
        template.layoutPadding = new RectOffset(25, 25, 15, 15);
        template.uiElements = new UIElementData[]
        {
            new UIElementData(UIElementType.Text) 
            { 
                elementName = "UsernameLabel", 
                text = "Имя пользователя:",
                fontSize = 14,
                textColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                textAlignment = TextAnchor.MiddleLeft
            },
            new UIElementData(UIElementType.InputField) 
            { 
                elementName = "UsernameInput",
                sizeDelta = new Vector2(280, 30),
                inputPlaceholder = "Введите имя пользователя",
                fontSize = 14,
                textColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                backgroundColor = Color.white
            },
            new UIElementData(UIElementType.Text) 
            { 
                elementName = "PasswordLabel", 
                text = "Пароль:",
                fontSize = 14,
                textColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                textAlignment = TextAnchor.MiddleLeft
            },
            new UIElementData(UIElementType.InputField) 
            { 
                elementName = "PasswordInput",
                sizeDelta = new Vector2(280, 30),
                inputPlaceholder = "Введите пароль",
                inputContentType = UnityEngine.UI.InputField.ContentType.Password,
                fontSize = 14,
                textColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                backgroundColor = Color.white
            },
            new UIElementData(UIElementType.Toggle) 
            { 
                elementName = "RememberToggle",
                text = "Запомнить меня",
                fontSize = 12,
                textColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                toggleValue = false,
                backgroundColor = Color.white
            },
            new UIElementData(UIElementType.Button) 
            { 
                elementName = "LoginButton",
                text = "Войти",
                sizeDelta = new Vector2(120, 35),
                fontSize = 16,
                textColor = Color.white,
                backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f)
            }
        };
        
        string fullPath = Path.Combine(path, "LoginForm_Template.asset");
        AssetDatabase.CreateAsset(template, fullPath);
    }

    private static void CreateInventoryGridTemplate(string path)
    {
        PanelTemplate template = ScriptableObject.CreateInstance<PanelTemplate>();
        
        template.panelName = "InventoryGrid";
        template.panelSize = new Vector2(500, 400);
        template.panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        template.includeHeader = true;
        template.headerTitle = "Инвентарь";
        template.headerHeight = 35f;
        template.headerBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        template.headerTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        template.headerFontSize = 16;
        template.includeCloseButton = true;
        template.closeButtonColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        template.includeFooter = false; // Инвентарь обычно не имеет футера
        
        template.saveAsPrefab = true;
        template.prefabPath = "Assets/Prefabs/UI/Inventory/";
        template.description = "Темная панель инвентаря для игр, оптимизированная для сетки предметов";
        
        // Настройка Layout и UI элементов
        template.contentLayoutType = LayoutType.GridLayout;
        template.layoutSpacing = new Vector2(5, 5);
        template.layoutPadding = new RectOffset(10, 10, 10, 10);
        template.gridColumns = 8;
        template.gridCellSize = new Vector2(50, 50);
        template.uiElements = new UIElementData[]
        {
            // Создаем несколько слотов инвентаря
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_01",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_02",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_03",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_04",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_05",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_06",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_07",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_08",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_09",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_10",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_11",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            },
            new UIElementData(UIElementType.Image) 
            { 
                elementName = "InventorySlot_12",
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            }
        };
        
        string fullPath = Path.Combine(path, "InventoryGrid_Template.asset");
        AssetDatabase.CreateAsset(template, fullPath);
    }
} 