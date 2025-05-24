using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Utils.Logging;

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.Scenes.PersonalAreaScene.Settings;

public class SettingsPanelPrefabCreator
{
    private const float SCREEN_WIDTH = 1080f;
    private const float SCREEN_HEIGHT = 1920f;
    private const float HEADER_HEIGHT = 160f;
    private const float TOGGLE_HEIGHT = 120f;
    private const float DROPDOWN_HEIGHT = 120f;
    private const float BUTTON_HEIGHT = 130f;
    private const float SPACING = 30f;
    private const float SIDE_PADDING = 40f;
    private const float POPUP_WIDTH = 800f;
    private const float POPUP_HEIGHT = 400f;
    private const float POPUP_BUTTON_HEIGHT = 100f;
    private const float POPUP_BUTTON_WIDTH = 300f;
    private const float POPUP_SPACING = 20f;

    [MenuItem("Tools/Create Mobile Settings Panel Prefab")]
    public static void CreateMobileSettingsPanelPrefab()
    {
        // Root Canvas
        GameObject root = new GameObject("SettingsPanel");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(SCREEN_WIDTH, SCREEN_HEIGHT);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        
        // Add Controller
        var controller = root.AddComponent<SettingsPanelController>();

        // Background
        GameObject background = CreateBackground(root.transform);

        // Main Container
        GameObject container = CreateContainer(root.transform);
        
        // Header
        GameObject header = CreateHeader("Настройки", container.transform);

        // Settings Group
        GameObject settingsGroup = CreateSettingsGroup(container.transform);

        // Create UI Elements
        GameObject notificationsToggle = CreateToggle("NotificationsToggle", "Уведомления", settingsGroup.transform);
        GameObject soundToggle = CreateToggle("SoundToggle", "Звук", settingsGroup.transform);
        GameObject themeDropdown = CreateDropdown("ThemeDropdown", "Тема", settingsGroup.transform);
        GameObject languageDropdown = CreateDropdown("LanguageDropdown", "Язык", settingsGroup.transform);

        // Buttons Group
        GameObject buttonsGroup = CreateButtonsGroup(container.transform);
        GameObject saveButton = CreateButton("SaveButton", "Сохранить", buttonsGroup.transform, new Color(0.2f, 0.7f, 0.2f));
        GameObject resetButton = CreateButton("ResetButton", "Сбросить", buttonsGroup.transform, new Color(0.7f, 0.7f, 0.7f));
        GameObject deleteButton = CreateButton("DeleteAccountButton", "Удалить аккаунт", buttonsGroup.transform, new Color(0.7f, 0.2f, 0.2f));

        // Создаем окно подтверждения удаления аккаунта
        GameObject deletionConfirmation = CreateDeletionConfirmationPanel(root.transform);

        // Assign references to controller
        SerializedObject serializedController = new SerializedObject(controller);
        
        AssignFieldToController(serializedController, "_notificationsToggle", notificationsToggle.GetComponent<Toggle>());
        AssignFieldToController(serializedController, "_soundToggle", soundToggle.GetComponent<Toggle>());
        AssignFieldToController(serializedController, "_themeDropdown", themeDropdown.GetComponent<TMP_Dropdown>());
        AssignFieldToController(serializedController, "_languageDropdown", languageDropdown.GetComponent<TMP_Dropdown>());
        AssignFieldToController(serializedController, "_saveButton", saveButton.GetComponent<Button>());
        AssignFieldToController(serializedController, "_resetButton", resetButton.GetComponent<Button>());
        AssignFieldToController(serializedController, "_deleteAccountButton", deleteButton.GetComponent<Button>());
        AssignFieldToController(serializedController, "_deletionConfirmationPanel", deletionConfirmation);
        AssignFieldToController(serializedController, "_confirmDeletionButton", 
            deletionConfirmation.transform.Find("ButtonsContainer/ConfirmButton").GetComponent<Button>());
        AssignFieldToController(serializedController, "_cancelDeletionButton", 
            deletionConfirmation.transform.Find("ButtonsContainer/CancelButton").GetComponent<Button>());
        AssignFieldToController(serializedController, "_logoutButton", 
            deletionConfirmation.transform.Find("ButtonsContainer/LogoutButton").GetComponent<Button>());

        serializedController.ApplyModifiedProperties();

        // Save Prefab
        string prefabPath = "Assets/Prefabs/UI/SettingsPanel.prefab";
        SavePrefab(root, prefabPath);
    }

    private static GameObject CreateBackground(Transform parent)
    {
        GameObject background = new GameObject("Background");
        background.transform.SetParent(parent);
        
        Image image = background.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.95f);
        
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return background;
    }

    private static GameObject CreateContainer(Transform parent)
    {
        GameObject container = new GameObject("Container");
        container.transform.SetParent(parent);
        
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(SIDE_PADDING, SIDE_PADDING);
        rect.offsetMax = new Vector2(-SIDE_PADDING, -SIDE_PADDING);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = SPACING;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return container;
    }

    private static GameObject CreateHeader(string title, Transform parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent);
        
        RectTransform rect = header.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, HEADER_HEIGHT);

        GameObject textObj = new GameObject("Title");
        textObj.transform.SetParent(header.transform);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return header;
    }

    private static GameObject CreateSettingsGroup(Transform parent)
    {
        GameObject group = new GameObject("SettingsGroup");
        group.transform.SetParent(parent);
        
        RectTransform rect = group.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 500); // Высота будет регулироваться содержимым

        VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
        layout.spacing = SPACING;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return group;
    }

    private static GameObject CreateToggle(string name, string label, Transform parent)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent);
        
        RectTransform rect = toggleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, TOGGLE_HEIGHT);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObj.transform);
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0.1f);
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform);
        
        TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 36;
        labelText.color = Color.white;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.8f, 1);
        labelRect.offsetMin = new Vector2(20, 0);
        labelRect.offsetMax = new Vector2(0, 0);

        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(toggleObj.transform);
        
        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f);
        
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.9f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.95f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(0, TOGGLE_HEIGHT * 0.5f);
        checkmarkRect.anchoredPosition = Vector2.zero;

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkmarkImage;

        return toggleObj;
    }

    private static GameObject CreateDropdown(string name, string label, Transform parent)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent);
        
        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, DROPDOWN_HEIGHT);

        // Background
        Image bgImage = dropdownObj.AddComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0.1f);

        // Dropdown component
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bgImage;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform);
        
        TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 36;
        labelText.color = Color.white;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.4f, 1);
        labelRect.offsetMin = new Vector2(20, 0);
        labelRect.offsetMax = new Vector2(0, 0);

        // Value Text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(dropdownObj.transform);
        
        TMP_Text valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "Выберите...";
        valueText.fontSize = 36;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Right;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.4f, 0);
        valueRect.anchorMax = new Vector2(0.95f, 1);
        valueRect.offsetMin = new Vector2(20, 0);
        valueRect.offsetMax = new Vector2(-20, 0);

        dropdown.captionText = valueText;

        return dropdownObj;
    }

    private static GameObject CreateButtonsGroup(Transform parent)
    {
        GameObject group = new GameObject("ButtonsGroup");
        group.transform.SetParent(parent);
        
        RectTransform rect = group.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, BUTTON_HEIGHT * 3 + SPACING * 2);

        VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
        layout.spacing = SPACING;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return group;
    }

    private static GameObject CreateButton(string name, string label, Transform parent, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, BUTTON_HEIGHT);

        Button button = buttonObj.AddComponent<Button>();
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        button.targetGraphic = image;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private static GameObject CreateDeletionConfirmationPanel(Transform parent)
    {
        GameObject panel = new GameObject("DeletionConfirmationPanel");
        panel.transform.SetParent(parent);
        panel.SetActive(false);

        // Настройка RectTransform
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Затемненный фон
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(panel.transform);
        
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.95f);
        
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Контейнер окна подтверждения
        GameObject container = new GameObject("Container");
        container.transform.SetParent(panel.transform);
        
        Image containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(POPUP_WIDTH, POPUP_HEIGHT);
        containerRect.anchoredPosition = Vector2.zero;

        // Заголовок
        GameObject title = CreatePopupText("Title", "Удаление аккаунта", 48, container.transform);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, -20);

        // Текст сообщения
        GameObject message = CreatePopupText("Message", 
            "Вы уверены, что хотите удалить свой аккаунт?\nЭто действие необратимо.", 
            36, container.transform);
        RectTransform messageRect = message.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0, 0.4f);
        messageRect.anchorMax = new Vector2(1, 0.8f);
        messageRect.offsetMin = new Vector2(20, 0);
        messageRect.offsetMax = new Vector2(-20, -20);

        // Контейнер для кнопок
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(container.transform);
        
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0, 0);
        buttonsRect.anchorMax = new Vector2(1, 0.4f);
        buttonsRect.offsetMin = new Vector2(20, 20);
        buttonsRect.offsetMax = new Vector2(-20, -20);

        HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = POPUP_SPACING;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = false;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;

        // Кнопки
        CreatePopupButton("CancelButton", "Отмена", 
            new Color(0.7f, 0.7f, 0.7f), buttonsContainer.transform);
        CreatePopupButton("LogoutButton", "Выйти", 
            new Color(0.3f, 0.6f, 0.9f), buttonsContainer.transform);
        CreatePopupButton("ConfirmButton", "Удалить аккаунт", 
            new Color(0.9f, 0.3f, 0.3f), buttonsContainer.transform);

        return panel;
    }

    private static GameObject CreatePopupText(string name, string text, int fontSize, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return textObj;
    }

    private static GameObject CreatePopupButton(string name, string label, Color color, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(POPUP_BUTTON_WIDTH, POPUP_BUTTON_HEIGHT);

        Button button = buttonObj.AddComponent<Button>();
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        button.targetGraphic = image;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private static void AssignFieldToController(SerializedObject serializedObject, string fieldName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
        else
        {
            MyLogger.EditorLogError($"Field {fieldName} not found in SettingsPanelController");
        }
    }

    private static void SavePrefab(GameObject root, string path)
    {
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        bool success = false;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        if (success)
        {
            MyLogger.EditorLog($"Settings Panel prefab created at: {path}");
        }
        else
        {
            MyLogger.EditorLogError($"Failed to create prefab at: {path}");
        }

        GameObject.DestroyImmediate(root);
    }
}
