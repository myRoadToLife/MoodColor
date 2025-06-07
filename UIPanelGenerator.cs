using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
#if UNITY_TEXTMESHPRO
using TMPro; // Используем TextMeshPro, если он доступен
#endif
public class UIPanelGenerator : EditorWindow
{
    // --- Параметры панели ---
    private string _panelName = "MyNewPanel";
    private Vector2 _panelSize = new Vector2(400, 300);
    private Color _panelBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private Sprite _panelBackgroundSprite = null;

    private bool _includeHeader = true;
    private string _headerTitle = "Panel Title";
    private float _headerHeight = 40f;
    private Color _headerBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    private Sprite _headerBackgroundSprite = null;
    private Color _headerTextColor = Color.white;
    private int _headerFontSize = 18;
    private bool _includeCloseButton = true;
    private Sprite _closeButtonSprite = null; // Можете назначить спрайт крестика здесь
    private Color _closeButtonColor = Color.white;

    private bool _includeFooter = true;
    private float _footerHeight = 50f;
    private Color _footerBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    private Sprite _footerBackgroundSprite = null;
    private bool _addOkCancelButtons = true;

    // --- Вспомогательные ---
    private static bool _useTextMeshPro = false;

    [MenuItem("Tools/Custom Panel Generator")]
    public static void ShowWindow()
    {
        // Проверяем наличие TextMeshPro при открытии окна
        #if UNITY_TEXTMESHPRO
            _useTextMeshPro = true;
        #else
            _useTextMeshPro = false;
        #endif
        GetWindow<UIPanelGenerator>("UI Panel Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Panel Settings", EditorStyles.boldLabel);
        _panelName = EditorGUILayout.TextField("Panel Name", _panelName);
        _panelSize = EditorGUILayout.Vector2Field("Panel Size (Width, Height)", _panelSize);
        _panelBackgroundColor = EditorGUILayout.ColorField("Background Color", _panelBackgroundColor);
        _panelBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background Sprite", _panelBackgroundSprite, typeof(Sprite), false);

        EditorGUILayout.Space();
        _includeHeader = EditorGUILayout.Toggle("Include Header", _includeHeader);
        if (_includeHeader)
        {
            EditorGUI.indentLevel++;
            _headerTitle = EditorGUILayout.TextField("Header Title", _headerTitle);
            _headerHeight = EditorGUILayout.FloatField("Header Height", _headerHeight);
            _headerBackgroundColor = EditorGUILayout.ColorField("Header BG Color", _headerBackgroundColor);
            _headerBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Header BG Sprite", _headerBackgroundSprite, typeof(Sprite), false);
            _headerTextColor = EditorGUILayout.ColorField("Header Text Color", _headerTextColor);
            _headerFontSize = EditorGUILayout.IntField("Header Font Size", _headerFontSize);
            _includeCloseButton = EditorGUILayout.Toggle("Include Close Button", _includeCloseButton);
            if (_includeCloseButton)
            {
                _closeButtonSprite = (Sprite)EditorGUILayout.ObjectField("Close Button Sprite", _closeButtonSprite, typeof(Sprite), false);
                _closeButtonColor = EditorGUILayout.ColorField("Close Button Color", _closeButtonColor);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        _includeFooter = EditorGUILayout.Toggle("Include Footer", _includeFooter);
        if (_includeFooter)
        {
            EditorGUI.indentLevel++;
            _footerHeight = EditorGUILayout.FloatField("Footer Height", _footerHeight);
            _footerBackgroundColor = EditorGUILayout.ColorField("Footer BG Color", _footerBackgroundColor);
            _footerBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Footer BG Sprite", _footerBackgroundSprite, typeof(Sprite), false);
            _addOkCancelButtons = EditorGUILayout.Toggle("Add OK/Cancel Buttons", _addOkCancelButtons);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(20);

        if (_useTextMeshPro == false)
        {
            EditorGUILayout.HelpBox("TextMeshPro package not found or not enabled. Standard UI Text will be used. For best results, import TextMeshPro.", MessageType.Warning);
        }


        if (GUILayout.Button("Generate Panel", GUILayout.Height(40)))
        {
            GeneratePanel();
        }
    }

    private void GeneratePanel()
    {
        // 1. Убедиться, что есть Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // 2. Создать корневой объект панели
        GameObject panelRootGO = CreateUIElement(_panelName, canvas.transform);
        RectTransform panelRect = panelRootGO.GetComponent<RectTransform>();
        panelRect.sizeDelta = _panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelRootGO.AddComponent<Image>();
        panelImage.sprite = _panelBackgroundSprite;
        panelImage.color = _panelBackgroundColor;
        if (_panelBackgroundSprite == null) panelImage.type = Image.Type.Sliced; // Для растягивания цвета

        // 3. Создать область контента (до заголовка и футера, чтобы правильно рассчитать ее размер)
        GameObject contentAreaGO = CreateUIElement("ContentArea", panelRect);
        RectTransform contentRect = contentAreaGO.GetComponent<RectTransform>();
        // Растягиваем по всему родителю, отступы будут позже
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.offsetMin = Vector2.zero; // left, bottom
        contentRect.offsetMax = Vector2.zero; // -right, -top

        // --- Смещение для контента ---
        float topOffset = 0;
        float bottomOffset = 0;

        // 4. Создать заголовок (Header)
        if (_includeHeader)
        {
            GameObject headerGO = CreateUIElement("Header", panelRect);
            RectTransform headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, _headerHeight); // Ширина по родителю, высота фиксированная
            headerRect.anchoredPosition = Vector2.zero;

            Image headerImage = headerGO.AddComponent<Image>();
            headerImage.sprite = _headerBackgroundSprite;
            headerImage.color = _headerBackgroundColor;
            if (_headerBackgroundSprite == null) headerImage.type = Image.Type.Sliced;

            // Текст заголовка
            GameObject headerTextGO = CreateTextElement("TitleText", headerRect, _headerTitle, _headerFontSize, _headerTextColor);
            RectTransform headerTextRect = headerTextGO.GetComponent<RectTransform>();
            headerTextRect.anchorMin = new Vector2(0, 0.5f);
            headerTextRect.anchorMax = new Vector2(1, 0.5f);
            headerTextRect.pivot = new Vector2(0.5f, 0.5f);
            headerTextRect.anchoredPosition = Vector2.zero;
            headerTextRect.offsetMin = new Vector2(10, 0); // Отступ слева
            headerTextRect.offsetMax = new Vector2(_includeCloseButton ? -_headerHeight - 5 : -10, 0); // Отступ справа (учитывая кнопку закрытия)

            if (_useTextMeshPro)
            {
                #if UNITY_TEXTMESHPRO
                TextMeshProUGUI tmpText = headerTextGO.GetComponent<TextMeshProUGUI>();
                tmpText.alignment = TextAlignmentOptions.Center;
                #endif
            }
            else
            {
                Text uiText = headerTextGO.GetComponent<Text>();
                uiText.alignment = TextAnchor.MiddleCenter;
            }

            // Кнопка закрытия
            if (_includeCloseButton)
            {
                GameObject closeButtonGO = CreateButtonElement("CloseButton", headerRect, "X");
                RectTransform closeButtonRect = closeButtonGO.GetComponent<RectTransform>();
                closeButtonRect.anchorMin = new Vector2(1, 0.5f);
                closeButtonRect.anchorMax = new Vector2(1, 0.5f);
                closeButtonRect.pivot = new Vector2(1, 0.5f);
                closeButtonRect.sizeDelta = new Vector2(_headerHeight - 10, _headerHeight - 10);
                closeButtonRect.anchoredPosition = new Vector2(-5, 0);

                Image closeButtonImage = closeButtonGO.GetComponent<Image>();
                closeButtonImage.sprite = _closeButtonSprite;
                closeButtonImage.color = _closeButtonColor;
                // Если нет спрайта, можно просто скрыть текст и сделать фон кнопки красным
                // if (closeButtonSprite == null) closeButtonImage.color = new Color(0.8f,0.2f,0.2f,0.8f);


                // Текст на кнопке (если нет спрайта)
                Transform textChild = closeButtonGO.transform.GetChild(0);
                if (_closeButtonSprite != null)
                {
                    Object.DestroyImmediate(textChild.gameObject); // Удаляем текст, если есть иконка
                }
                else
                {
                    // Настроить текст "X"
                    if (_useTextMeshPro)
                    {
                        #if UNITY_TEXTMESHPRO
                        TextMeshProUGUI btnTmpText = textChild.GetComponent<TextMeshProUGUI>();
                        if (btnTmpText) { btnTmpText.text = "X"; btnTmpText.color = Color.black; btnTmpText.fontSize = _headerFontSize * 0.8f; }
                        #endif
                    }
                    else
                    {
                        Text btnUiText = textChild.GetComponent<Text>();
                        if (btnUiText) { btnUiText.text = "X"; btnUiText.color = Color.black; btnUiText.fontSize = (int)(_headerFontSize * 0.8f); }
                    }
                }
            }
            topOffset = _headerHeight;
        }

        // 5. Создать футер (Footer)
        if (_includeFooter)
        {
            GameObject footerGO = CreateUIElement("Footer", panelRect);
            RectTransform footerRect = footerGO.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(0, _footerHeight); // Ширина по родителю, высота фиксированная
            footerRect.anchoredPosition = Vector2.zero;

            Image footerImage = footerGO.AddComponent<Image>();
            footerImage.sprite = _footerBackgroundSprite;
            footerImage.color = _footerBackgroundColor;
            if (_footerBackgroundSprite == null) footerImage.type = Image.Type.Sliced;

            if (_addOkCancelButtons)
            {
                HorizontalLayoutGroup hlg = footerGO.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(10, 10, 5, 5);
                hlg.spacing = 10;
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.childControlHeight = true;
                hlg.childControlWidth = false; // Кнопки будут иметь предпочтительную ширину

                // Кнопка Cancel
                GameObject cancelButtonGO = CreateButtonElement("CancelButton", footerRect, "Cancel");
                LayoutElement cancelLE = cancelButtonGO.AddComponent<LayoutElement>();
                cancelLE.preferredWidth = 80;


                // Кнопка OK
                GameObject okButtonGO = CreateButtonElement("OkButton", footerRect, "OK");
                LayoutElement okLE = okButtonGO.AddComponent<LayoutElement>();
                okLE.preferredWidth = 80;
            }
            bottomOffset = _footerHeight;
        }

        // 6. Настроить отступы для ContentArea
        contentRect.offsetMin = new Vector2(0, bottomOffset); // left, bottom
        contentRect.offsetMax = new Vector2(0, -topOffset);    // -right, -top

        // Выбрать созданную панель
        Undo.RegisterCreatedObjectUndo(panelRootGO, "Create " + _panelName);
        Selection.activeGameObject = panelRootGO;

        Debug.Log($"Panel '{_panelName}' generated successfully!");
    }

    // --- Вспомогательные методы для создания UI элементов ---

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, Color color)
    {
        GameObject go = CreateUIElement(name, parent);
        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            TextMeshProUGUI tmpText = go.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.color = color;
            tmpText.alignment = TextAlignmentOptions.Left; // По умолчанию
            #endif
        }
        else
        {
            Text uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.color = color;
            uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.alignment = TextAnchor.MiddleLeft; // По умолчанию
        }
        return go;
    }

    private GameObject CreateButtonElement(string name, Transform parent, string buttonText)
    {
        // Стандартный путь к UI спрайтам (может отличаться в зависимости от версии Unity)
        string uiSpritePath = "UI/Skin/UISprite.psd";
        Sprite defaultButtonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(uiSpritePath);

        GameObject buttonGO = CreateUIElement(name, parent);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.sprite = defaultButtonSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white; // Цвет фона кнопки по умолчанию

        Button button = buttonGO.AddComponent<Button>();
        // Можно настроить Button Transition, Navigation и т.д. здесь, если нужно

        // Текст для кнопки
        GameObject textGO = CreateTextElement("Text", buttonGO.transform, buttonText, 14, Color.black);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            TextMeshProUGUI tmpText = textGO.GetComponent<TextMeshProUGUI>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = new Color32(50, 50, 50, 255);
            #endif
        }
        else
        {
            Text uiText = textGO.GetComponent<Text>();
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = new Color32(50, 50, 50, 255);
        }
        return buttonGO;
    }
} 