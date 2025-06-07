using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public enum UIElementType
{
    Button,
    InputField,
    Text,
    Image,
    ScrollView,
    Slider,
    Toggle,
    Dropdown,
    ProgressBar,
    Spacer
}

[Serializable]
public enum LayoutType
{
    Manual,          // Ручное позиционирование
    VerticalLayout,  // Vertical Layout Group
    HorizontalLayout, // Horizontal Layout Group
    GridLayout       // Grid Layout Group
}

[Serializable]
public class UIElementData
{
    [Header("Element Basic Info")]
    public string elementName = "NewElement";
    public UIElementType elementType = UIElementType.Button;
    public bool isActive = true;

    [Header("Position & Size")]
    public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
    public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
    public Vector2 anchoredPosition = Vector2.zero;
    public Vector2 sizeDelta = new Vector2(100, 30);

    [Header("Text Properties")]
    public string text = "Button";
    public int fontSize = 14;
    public Color textColor = Color.black;
    public TextAnchor textAlignment = TextAnchor.MiddleCenter;

    [Header("Visual Properties")]
    public Color backgroundColor = Color.white;
    public Sprite backgroundSprite = null;
    public Image.Type imageType = Image.Type.Simple;

    [Header("Interactive Properties")]
    public bool interactable = true;
    public string onClickMethodName = ""; // Имя метода для кнопки

    [Header("Specific Properties")]
    public float sliderValue = 0.5f; // Для Slider
    public float sliderMinValue = 0f;
    public float sliderMaxValue = 1f;
    public bool toggleValue = false; // Для Toggle
    public string[] dropdownOptions = new string[] { "Option 1", "Option 2", "Option 3" }; // Для Dropdown
    public string inputPlaceholder = "Enter text..."; // Для InputField
    public InputField.ContentType inputContentType = InputField.ContentType.Standard;

    // Конструктор с значениями по умолчанию для разных типов
    public UIElementData(UIElementType type)
    {
        elementType = type;
        SetDefaultsForType(type);
    }

    public UIElementData()
    {
        SetDefaultsForType(UIElementType.Button);
    }

    private void SetDefaultsForType(UIElementType type)
    {
        switch (type)
        {
            case UIElementType.Button:
                elementName = "Button";
                text = "Button";
                sizeDelta = new Vector2(100, 30);
                backgroundColor = Color.white;
                break;

            case UIElementType.InputField:
                elementName = "InputField";
                text = "";
                sizeDelta = new Vector2(200, 30);
                backgroundColor = Color.white;
                break;

            case UIElementType.Text:
                elementName = "Text";
                text = "Text";
                sizeDelta = new Vector2(160, 30);
                backgroundColor = Color.clear;
                textAlignment = TextAnchor.MiddleLeft;
                break;

            case UIElementType.Image:
                elementName = "Image";
                text = "";
                sizeDelta = new Vector2(100, 100);
                backgroundColor = Color.white;
                break;

            case UIElementType.ScrollView:
                elementName = "ScrollView";
                text = "";
                sizeDelta = new Vector2(200, 200);
                backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                break;

            case UIElementType.Slider:
                elementName = "Slider";
                text = "";
                sizeDelta = new Vector2(160, 20);
                backgroundColor = Color.white;
                sliderValue = 0.5f;
                break;

            case UIElementType.Toggle:
                elementName = "Toggle";
                text = "Toggle";
                sizeDelta = new Vector2(160, 20);
                backgroundColor = Color.white;
                break;

            case UIElementType.Dropdown:
                elementName = "Dropdown";
                text = "Option 1";
                sizeDelta = new Vector2(160, 30);
                backgroundColor = Color.white;
                break;

            case UIElementType.ProgressBar:
                elementName = "ProgressBar";
                text = "";
                sizeDelta = new Vector2(200, 20);
                backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                break;

            case UIElementType.Spacer:
                elementName = "Spacer";
                text = "";
                sizeDelta = new Vector2(10, 10);
                backgroundColor = Color.clear;
                interactable = false;
                break;
        }
    }
}

[CreateAssetMenu(fileName = "NewPanelTemplate", menuName = "UI/Panel Template", order = 1)]
public class PanelTemplate : ScriptableObject
{
    private void OnEnable()
    {
        if (layoutPadding == null)
        {
            layoutPadding = new RectOffset(10, 10, 10, 10);
        }
    }
    [Header("Panel Settings")]
    public string panelName = "MyNewPanel";
    public Vector2 panelSize = new Vector2(400, 300);
    public Color panelBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Sprite panelBackgroundSprite = null;

    [Header("Header Settings")]
    public bool includeHeader = true;
    public string headerTitle = "Panel Title";
    public float headerHeight = 40f;
    public Color headerBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Sprite headerBackgroundSprite = null;
    public Color headerTextColor = Color.white;
    public int headerFontSize = 18;
    public bool includeCloseButton = true;
    public Sprite closeButtonSprite = null;
    public Color closeButtonColor = Color.white;

    [Header("Footer Settings")]
    public bool includeFooter = true;
    public float footerHeight = 50f;
    public Color footerBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Sprite footerBackgroundSprite = null;
    public bool addOkCancelButtons = true;

    [Header("Prefab Settings")]
    public bool saveAsPrefab = true;
    public string prefabPath = "Assets/Prefabs/UI/GeneratedPanels/";

    [Header("Content Layout")]
    public LayoutType contentLayoutType = LayoutType.Manual;
    public Vector2 layoutSpacing = new Vector2(5, 5);
    public RectOffset layoutPadding;
    public int gridColumns = 2; // Для Grid Layout
    public Vector2 gridCellSize = new Vector2(100, 100); // Для Grid Layout

    [Header("UI Elements")]
    public UIElementData[] uiElements = new UIElementData[0];

    [Header("Template Info")]
    [TextArea(3, 5)]
    public string description = "Описание шаблона панели";
} 