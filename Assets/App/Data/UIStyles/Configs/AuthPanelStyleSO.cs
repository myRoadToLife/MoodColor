using UnityEngine;
using UnityEngine.UI; // Добавлено для Image.Type и ColorBlock
using TMPro; // Для TMP_FontAsset

// Атрибут для создания ассета через меню Unity
[CreateAssetMenu(fileName = "AuthPanelStyle", menuName = "MoodColor/UI Styles/Auth Panel Style")]
public class AuthPanelStyleSO : ScriptableObject
{
    [Header("Шрифты")]
    public TMP_FontAsset DefaultFont;
    public TMP_FontAsset TitleFont;
    // Можно добавить еще специфичные шрифты, если нужно

    [Header("Глобальные цвета")]
    public Color GlobalBackgroundColor = new Color(0.95f, 0.9f, 0.85f, 1f); // Светло-бежевый фон по умолчанию
    public Color DefaultTextColor = new Color(0.2f, 0.15f, 0.1f, 1f); // Темно-коричневый по умолчанию

    [Header("Стили Панелей")]
    public PanelStyle LoginPanelStyle;
    public PanelStyle RegisterPanelStyle;
    public PanelStyle ResetPasswordPanelStyle;
    public PanelStyle EmailVerificationPanelStyle; // Если будут использоваться
    public PanelStyle ProfilePanelStyle;           // Если будут использоваться

    [Header("Стили Элементов Управления")]
    public InputFieldStyle DefaultInputFieldStyle;
    public ButtonStyle DefaultButtonStyle;
    public ButtonStyle DestructiveButtonStyle; // Например, для кнопки "Отмена" или "Удалить" (если понадобится)
    public ToggleStyle DefaultToggleStyle;

    [Header("Стили Текста")]
    public TextStyle TitleTextStyle;
    public TextStyle LabelTextStyle;
    public TextStyle MessageTextStyle; // Для _messageText (ошибки, информация)
    public TextStyle ButtonTextStyle;
    public TextStyle InputPlaceholderTextStyle;
    public TextStyle InputValueTextStyle;

    [Header("Отступы и Размеры")]
    public float DefaultPadding = 10f;
    public float ItemSpacing = 15f;
    public Vector2 DefaultButtonSize = new Vector2(350, 70);
    public Vector2 DefaultInputSize = new Vector2(500, 60);

    [Header("Иконки (Спрайты)")]
    public Sprite EyeIconOpened;
    public Sprite EyeIconClosed;
    public Sprite CheckmarkIcon;
    // public Sprite WoodenFrameSprite; // Если будет общий спрайт для рамок
    // public Sprite WoodenPlankSprite; // Если будет общий спрайт для кнопок

    // Вложенные классы/структуры для стилей
    [System.Serializable]
    public class PanelStyle
    {
        public Color BackgroundColor = new Color(0.82f, 0.71f, 0.55f, 1f); // "Деревянный" цвет по умолчанию
        public Sprite BackgroundSprite; // Если вместо цвета используется спрайт
        public Image.Type BackgroundImageType = Image.Type.Sliced; // Для масштабируемых спрайтов
        public Color BorderColor = Color.clear; // Если нужна рамка вокруг панели
        public float BorderWidth = 0f;
        public Sprite FrameSprite;
        public Color FrameColor = new Color(0.75f, 0.64f, 0.48f, 1f); // Темно-деревянный цвет рамки по умолчанию
        public Image.Type FrameImageType = Image.Type.Sliced;
    }

    [System.Serializable]
    public class TextStyle
    {
        public TMP_FontAsset Font; // Если null, используется DefaultFont из SO
        [Min(1)]
        public int FontSize = 24;
        public Color TextColor; // Если Color.clear, используется DefaultTextColor из SO
        public FontStyles FontStyle = FontStyles.Normal;
        public TextAlignmentOptions Alignment = TextAlignmentOptions.Left;
    }

    [System.Serializable]
    public class InputFieldStyle
    {
        public Color BackgroundColor = new Color(1f, 0.98f, 0.95f, 1f); // Светло-кремовый
        public Sprite BackgroundSprite;
        public Image.Type BackgroundImageType = Image.Type.Sliced;
        public Color PlaceholderBackgroundColor = new Color(0.9f, 0.88f, 0.85f, 0.3f); // Полупрозрачный цвет фона для "заглушки" поля ввода
        // Задаем значения по умолчанию, которые были в AuthPanelGenerator
        public ColorBlock InputStateColors = new ColorBlock
        {
            normalColor = new Color(0.9f, 0.85f, 0.8f, 1f), // Было в inputField.colors.normalColor
            highlightedColor = new Color(1f, 0.95f, 0.9f, 1f), // Было в inputField.colors.highlightedColor и selectedColor
            pressedColor = new Color(0.85f, 0.8f, 0.75f, 1f), // Было в inputField.colors.pressedColor
            selectedColor = new Color(1f, 0.95f, 0.9f, 1f), // Повторяет highlightedColor как было
            disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f), // Было в inputField.colors.disabledColor
            colorMultiplier = 1,
            fadeDuration = 0.1f // Стандартное значение
        };
        // public TextStyle PlaceholderStyle;
        // public TextStyle ValueStyle;
    }

    [System.Serializable]
    public class ButtonStyle
    {
        public Color BackgroundColor = new Color(0.75f, 0.64f, 0.48f, 1f); // Цвет кнопки по умолчанию (деревянный)
        public Sprite BackgroundSprite;
        public Image.Type BackgroundImageType = Image.Type.Sliced;
        // Задаем значения по умолчанию, которые были в AuthPanelGenerator
        public ColorBlock ButtonStateColors = new ColorBlock
        {
            normalColor = Color.white, // Для спрайта/фона по умолчанию (не меняет цвет фона кнопки)
            highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f), // Было в button.colors.highlightedColor
            pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f), // Было в button.colors.pressedColor
            selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f), // Повторяет highlightedColor как было
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f), // Было в button.colors.disabledColor
            colorMultiplier = 1,
            fadeDuration = 0.1f // Стандартное значение
        };
        public Color TextColor = Color.white; // Цвет текста на кнопке по умолчанию
        // public TextStyle TextStyle;
    }

    [System.Serializable]
    public class ToggleStyle
    {
        public Color BoxBackgroundColor = new Color(1f, 0.98f, 0.95f, 1f); // Светло-кремовый фон для квадратика
        public Sprite BoxBackgroundSprite;
        public Color CheckmarkColor = new Color(0.3f, 0.2f, 0.15f, 1f); // Темный цвет галочки
        public Sprite CheckmarkSprite; // Если null, используется CheckmarkIcon из SO
        // public TextStyle LabelStyle;
    }
} 