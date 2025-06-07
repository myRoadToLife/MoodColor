// UIPanelGenerator.cs
using System.Collections.Generic; // For lists, if needed for dropdowns
using System.IO; // For Path operations
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI; // For LINQ operations on template names

#if UNITY_TEXTMESHPRO
using TMPro; // Используем TextMeshPro, если он доступен
#endif

public class UIPanelGenerator : EditorWindow
{
    // --- Текущий редактируемый шаблон или временные настройки ---
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
    private Sprite _closeButtonSprite = null;
    private Color _closeButtonColor = Color.white;

    private bool _includeFooter = true;
    private float _footerHeight = 50f;
    private Color _footerBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    private Sprite _footerBackgroundSprite = null;
    private bool _addOkCancelButtons = true;

    private bool _saveAsPrefab = true; // Changed default, as we often want to save
    private string _prefabPath = "Assets/Prefabs/UI/GeneratedPanels/"; // Default path

    // --- Управление шаблонами ---
    private PanelTemplate _currentTemplate;
    private string _newTemplateName = "NewPanelTemplate";
    private string _templateAssetPath = "Assets/Editor/PanelTemplates/"; // Where to save PanelTemplate assets

    // --- Вспомогательные ---
    private static bool _useTextMeshPro = false;
    private Vector2 _scrollPosition;
    private Vector2 _elementsScrollPosition;
    private bool _showElementsSettings = true;
    private int _selectedElementIndex = -1;
    private UIElementType _newElementType = UIElementType.Button;


    [MenuItem("Tools/Custom Panel Generator")]
    public static void ShowWindow()
    {
        #if UNITY_TEXTMESHPRO
            _useTextMeshPro = true;
        #else
            _useTextMeshPro = false;
        #endif
        GetWindow<UIPanelGenerator>("UI Panel Generator");
    }

    void OnGUI()
    {
        // Базовая структура GUI без вложенных блоков для защиты от layout ошибок
        DrawSafeGUI();
    }
    
    private void DrawSafeGUI()
    {
        // Используем try-finally для гарантированного закрытия scroll view
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        try
        {
            // Блок управления шаблонами
            DrawTemplateManagement();
            
            EditorGUILayout.Space(10);
            
            // Блок настроек панели
            DrawPanelSettings();
            
            EditorGUILayout.Space(10);
            
            // Блок UI элементов (переделан для безопасности)
            DrawSafeUIElementsSection();
            
            EditorGUILayout.Space(10);
            
            // Блок TextMeshPro предупреждения
            if (_useTextMeshPro == false)
            {
                EditorGUILayout.HelpBox("TextMeshPro package not found or not enabled. Standard UI Text will be used. For best results, import TextMeshPro.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(5);
            
            // Блок настроек префаба
            DrawPrefabSettings();
            
            EditorGUILayout.Space(20);
            
            // Кнопка генерации
            if (GUILayout.Button("Generate Panel from Current Settings", GUILayout.Height(40)))
            {
                GeneratePanel();
            }
        }
        finally
        {
            // Гарантированно закрываем scroll view
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void DrawTemplateManagement()
    {
        GUILayout.Label("Template Management", EditorStyles.boldLabel);
        
        // Load Template
        EditorGUILayout.BeginHorizontal();
        try
        {
            _currentTemplate = (PanelTemplate)EditorGUILayout.ObjectField("Load Template", _currentTemplate, typeof(PanelTemplate), false);
            if (GUILayout.Button("Apply", GUILayout.Width(60)) && _currentTemplate != null)
            {
                ApplyTemplate(_currentTemplate);
                GUI.FocusControl(null);
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
        
        // Save Template
        EditorGUILayout.BeginHorizontal();
        try
        {
            _newTemplateName = EditorGUILayout.TextField("New Template Name", _newTemplateName);
            if (GUILayout.Button("Save as New Template", GUILayout.Width(150)))
            {
                SaveAsNewTemplate();
                GUI.FocusControl(null);
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
        
        // Update Template
        if (_currentTemplate != null)
        {
            if (GUILayout.Button($"Update '{_currentTemplate.name}' Template"))
            {
                UpdateExistingTemplate(_currentTemplate);
                GUI.FocusControl(null);
            }
        }
        
        EditorGUILayout.HelpBox("Templates are ScriptableObjects. Create them via Project > Create > UI > Panel Template, or save current settings as a new template.", MessageType.Info);
    }
    
    private void DrawPanelSettings()
    {
        GUILayout.Label("Current Panel Settings (Editable)", EditorStyles.boldLabel);
        
        // Basic Panel Settings
        _panelName = EditorGUILayout.TextField("Panel Name", _panelName);
        _panelSize = EditorGUILayout.Vector2Field("Panel Size (Width, Height)", _panelSize);
        _panelBackgroundColor = EditorGUILayout.ColorField("Background Color", _panelBackgroundColor);
        _panelBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background Sprite", _panelBackgroundSprite, typeof(Sprite), false);
        
        EditorGUILayout.Space();
        
        // Header Settings
        _includeHeader = EditorGUILayout.Toggle("Include Header", _includeHeader);
        if (_includeHeader)
        {
            EditorGUI.indentLevel++;
            try
            {
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
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.Space();
        
        // Footer Settings
        _includeFooter = EditorGUILayout.Toggle("Include Footer", _includeFooter);
        if (_includeFooter)
        {
            EditorGUI.indentLevel++;
            try
            {
                _footerHeight = EditorGUILayout.FloatField("Footer Height", _footerHeight);
                _footerBackgroundColor = EditorGUILayout.ColorField("Footer BG Color", _footerBackgroundColor);
                _footerBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Footer BG Sprite", _footerBackgroundSprite, typeof(Sprite), false);
                _addOkCancelButtons = EditorGUILayout.Toggle("Add OK/Cancel Buttons", _addOkCancelButtons);
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }
    }
    
    private void DrawSafeUIElementsSection()
    {
        _showElementsSettings = EditorGUILayout.Foldout(_showElementsSettings, "UI Elements", true);
        if (!_showElementsSettings) return;
        
        EditorGUI.indentLevel++;
        try
        {
            // Проверяем и инициализируем layoutPadding для безопасности
            if (_currentTemplate != null && _currentTemplate.layoutPadding == null)
            {
                _currentTemplate.layoutPadding = new RectOffset(10, 10, 10, 10);
            }
            
            // Content Layout Settings
            if (_currentTemplate != null)
            {
                EditorGUILayout.LabelField("Content Layout", EditorStyles.boldLabel);
                _currentTemplate.contentLayoutType = (LayoutType)EditorGUILayout.EnumPopup("Layout Type", _currentTemplate.contentLayoutType);
                
                if (_currentTemplate.contentLayoutType != LayoutType.Manual)
                {
                    _currentTemplate.layoutSpacing = EditorGUILayout.Vector2Field("Spacing", _currentTemplate.layoutSpacing);
                    _currentTemplate.layoutPadding.left = EditorGUILayout.IntField("Padding Left", _currentTemplate.layoutPadding.left);
                    _currentTemplate.layoutPadding.right = EditorGUILayout.IntField("Padding Right", _currentTemplate.layoutPadding.right);
                    _currentTemplate.layoutPadding.top = EditorGUILayout.IntField("Padding Top", _currentTemplate.layoutPadding.top);
                    _currentTemplate.layoutPadding.bottom = EditorGUILayout.IntField("Padding Bottom", _currentTemplate.layoutPadding.bottom);
                    
                    if (_currentTemplate.contentLayoutType == LayoutType.GridLayout)
                    {
                        _currentTemplate.gridColumns = EditorGUILayout.IntField("Grid Columns", _currentTemplate.gridColumns);
                        _currentTemplate.gridCellSize = EditorGUILayout.Vector2Field("Cell Size", _currentTemplate.gridCellSize);
                    }
                }
                
                EditorGUILayout.Space();
                
                // UI Elements List Header
                EditorGUILayout.LabelField("Elements List", EditorStyles.boldLabel);
                
                // Add Element Controls
                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField("Add New Element:", GUILayout.Width(120));
                    _newElementType = (UIElementType)EditorGUILayout.EnumPopup(_newElementType, GUILayout.Width(120));
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                    {
                        AddNewUIElement(_newElementType);
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
                
                // Quick Add Buttons
                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField("Quick Add:", GUILayout.Width(80));
                    if (GUILayout.Button("Button", GUILayout.Width(60))) AddNewUIElement(UIElementType.Button);
                    if (GUILayout.Button("Text", GUILayout.Width(50))) AddNewUIElement(UIElementType.Text);
                    if (GUILayout.Button("Input", GUILayout.Width(55))) AddNewUIElement(UIElementType.InputField);
                    if (GUILayout.Button("Image", GUILayout.Width(55))) AddNewUIElement(UIElementType.Image);
                    if (GUILayout.Button("Slider", GUILayout.Width(55))) AddNewUIElement(UIElementType.Slider);
                    if (GUILayout.Button("Toggle", GUILayout.Width(60))) AddNewUIElement(UIElementType.Toggle);
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(5);
                
                // Draw Elements List
                if (_currentTemplate != null && _currentTemplate.uiElements != null && _currentTemplate.uiElements.Length > 0)
                {
                    // Используем try-finally для безопасного закрытия ScrollView
                    _elementsScrollPosition = EditorGUILayout.BeginScrollView(_elementsScrollPosition, GUILayout.Height(200));
                    try
                    {
                        // Работаем с копией массива для безопасного прохода
                        UIElementData[] elementsCopy = new UIElementData[_currentTemplate.uiElements.Length];
                        System.Array.Copy(_currentTemplate.uiElements, elementsCopy, _currentTemplate.uiElements.Length);
                        
                        // Сохраняем индексы элементов для удаления, чтобы сделать это после цикла
                        List<int> elementsToRemove = new List<int>();
                        
                        for (int i = 0; i < elementsCopy.Length; i++)
                        {
                            if (i >= _currentTemplate.uiElements.Length) continue;
                            
                            bool shouldRemove = DrawSafeUIElementEntry(i, _currentTemplate.uiElements[i]);
                            if (shouldRemove)
                            {
                                elementsToRemove.Add(i);
                            }
                        }
                        
                        // Удаляем элементы в обратном порядке после отрисовки всех элементов
                        for (int i = elementsToRemove.Count - 1; i >= 0; i--)
                        {
                            RemoveUIElement(elementsToRemove[i]);
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndScrollView();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Load a template to edit UI elements, or save current settings as a new template.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Load a template to edit UI elements, or save current settings as a new template.", MessageType.Info);
            }
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }
    
    private bool DrawSafeUIElementEntry(int index, UIElementData element)
    {
        bool shouldRemove = false;
        
        EditorGUILayout.BeginVertical("box");
        try
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                element.isActive = EditorGUILayout.Toggle(element.isActive, GUILayout.Width(20));
                
                bool isSelected = _selectedElementIndex == index;
                string elementIcon = GetElementIcon(element.elementType);
                string elementLabel = $"{elementIcon} {element.elementName} ({element.elementType})";
                
                if (GUILayout.Toggle(isSelected, elementLabel, "button"))
                {
                    _selectedElementIndex = isSelected ? -1 : index;
                }
                
                // Управление порядком
                if (GUILayout.Button("▲", GUILayout.Width(25)) && index > 0)
                {
                    SwapUIElements(index, index - 1);
                }
                if (GUILayout.Button("▼", GUILayout.Width(25)) && index < _currentTemplate.uiElements.Length - 1)
                {
                    SwapUIElements(index, index + 1);
                }
                
                // Кнопка удаления
                shouldRemove = GUILayout.Button("×", GUILayout.Width(25));
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
            
            // Отображаем детали только если элемент выбран и не планируется удаление
            if (!shouldRemove && _selectedElementIndex == index)
            {
                EditorGUI.indentLevel++;
                try
                {
                    DrawSafeUIElementDetails(element);
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
        
        return shouldRemove;
    }
    
    private void DrawSafeUIElementDetails(UIElementData element)
    {
        EditorGUILayout.LabelField("Element Details", EditorStyles.boldLabel);
        
        element.elementName = EditorGUILayout.TextField("Name", element.elementName);
        element.elementType = (UIElementType)EditorGUILayout.EnumPopup("Type", element.elementType);
        
        EditorGUILayout.Space();
        
        // Position & Size
        EditorGUILayout.LabelField("Position & Size", EditorStyles.boldLabel);
        element.anchorMin = EditorGUILayout.Vector2Field("Anchor Min", element.anchorMin);
        element.anchorMax = EditorGUILayout.Vector2Field("Anchor Max", element.anchorMax);
        element.anchoredPosition = EditorGUILayout.Vector2Field("Position", element.anchoredPosition);
        element.sizeDelta = EditorGUILayout.Vector2Field("Size", element.sizeDelta);
        
        EditorGUILayout.Space();
        
        // Visual
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        element.backgroundColor = EditorGUILayout.ColorField("Background Color", element.backgroundColor);
        element.backgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background Sprite", element.backgroundSprite, typeof(Sprite), false);
        
        // Text Properties
        if (HasTextProperty(element.elementType))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            element.text = EditorGUILayout.TextField("Text", element.text);
            element.fontSize = EditorGUILayout.IntField("Font Size", element.fontSize);
            element.textColor = EditorGUILayout.ColorField("Text Color", element.textColor);
            element.textAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Text Alignment", element.textAlignment);
        }
        
        // InputField Properties
        if (element.elementType == UIElementType.InputField)
        {
            element.inputPlaceholder = EditorGUILayout.TextField("Placeholder", element.inputPlaceholder);
            element.inputContentType = (InputField.ContentType)EditorGUILayout.EnumPopup("Content Type", element.inputContentType);
        }
        
        // Slider Properties
        if (element.elementType == UIElementType.Slider)
        {
            element.sliderMinValue = EditorGUILayout.FloatField("Min Value", element.sliderMinValue);
            element.sliderMaxValue = EditorGUILayout.FloatField("Max Value", element.sliderMaxValue);
            element.sliderValue = EditorGUILayout.Slider("Value", element.sliderValue, element.sliderMinValue, element.sliderMaxValue);
        }
        
        // Toggle Properties
        if (element.elementType == UIElementType.Toggle)
        {
            element.toggleValue = EditorGUILayout.Toggle("Default Value", element.toggleValue);
        }
        
        // Dropdown Properties
        if (element.elementType == UIElementType.Dropdown)
        {
            EditorGUILayout.LabelField("Dropdown Options");
            if (element.dropdownOptions == null) element.dropdownOptions = new string[1] { "Option 1" };
            
            // Сохраняем индексы опций для удаления
            List<int> optionsToRemove = new List<int>();
            
            // Безопасное отображение опций
            for (int i = 0; i < element.dropdownOptions.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    element.dropdownOptions[i] = EditorGUILayout.TextField($"Option {i + 1}", element.dropdownOptions[i]);
                    if (GUILayout.Button("-", GUILayout.Width(20)) && element.dropdownOptions.Length > 1)
                    {
                        optionsToRemove.Add(i);
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            // Удаление опций после цикла
            if (optionsToRemove.Count > 0)
            {
                var newOptions = new List<string>(element.dropdownOptions);
                for (int i = optionsToRemove.Count - 1; i >= 0; i--)
                {
                    newOptions.RemoveAt(optionsToRemove[i]);
                }
                element.dropdownOptions = newOptions.ToArray();
            }
            
            // Добавление новой опции
            if (GUILayout.Button("Add Option"))
            {
                var newOptions = new string[element.dropdownOptions.Length + 1];
                System.Array.Copy(element.dropdownOptions, newOptions, element.dropdownOptions.Length);
                newOptions[element.dropdownOptions.Length] = $"Option {element.dropdownOptions.Length + 1}";
                element.dropdownOptions = newOptions;
            }
        }
        
        // Interactive Properties
        if (IsInteractiveElement(element.elementType))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interactive", EditorStyles.boldLabel);
            element.interactable = EditorGUILayout.Toggle("Interactable", element.interactable);
            if (element.elementType == UIElementType.Button)
            {
                element.onClickMethodName = EditorGUILayout.TextField("OnClick Method", element.onClickMethodName);
            }
        }
    }
    
    private void DrawPrefabSettings()
    {
        GUILayout.Label("Prefab Generation Settings", EditorStyles.boldLabel);
        _saveAsPrefab = EditorGUILayout.Toggle("Save as Prefab", _saveAsPrefab);
        if (_saveAsPrefab)
        {
            EditorGUI.indentLevel++;
            try
            {
                _prefabPath = EditorGUILayout.TextField("Prefab Save Folder", _prefabPath);
                EditorGUILayout.HelpBox("The panel name above will be used as the prefab file name.", MessageType.None);
            }
            finally
            {
                EditorGUI.indentLevel--;
            }
        }
    }

    private void ApplyTemplate(PanelTemplate template)
    {
        if (template == null) return;

        _panelName = template.panelName;
        _panelSize = template.panelSize;
        _panelBackgroundColor = template.panelBackgroundColor;
        _panelBackgroundSprite = template.panelBackgroundSprite;

        _includeHeader = template.includeHeader;
        _headerTitle = template.headerTitle;
        _headerHeight = template.headerHeight;
        _headerBackgroundColor = template.headerBackgroundColor;
        _headerBackgroundSprite = template.headerBackgroundSprite;
        _headerTextColor = template.headerTextColor;
        _headerFontSize = template.headerFontSize;
        _includeCloseButton = template.includeCloseButton;
        _closeButtonSprite = template.closeButtonSprite;
        _closeButtonColor = template.closeButtonColor;

        _includeFooter = template.includeFooter;
        _footerHeight = template.footerHeight;
        _footerBackgroundColor = template.footerBackgroundColor;
        _footerBackgroundSprite = template.footerBackgroundSprite;
        _addOkCancelButtons = template.addOkCancelButtons;

        _saveAsPrefab = template.saveAsPrefab;
        _prefabPath = template.prefabPath;

        Repaint(); // Ensure the UI updates
    }

    private void PopulateTemplateFromCurrentSettings(PanelTemplate template)
    {
        if (template == null) return;

        template.panelName = _panelName;
        template.panelSize = _panelSize;
        template.panelBackgroundColor = _panelBackgroundColor;
        template.panelBackgroundSprite = _panelBackgroundSprite;

        template.includeHeader = _includeHeader;
        template.headerTitle = _headerTitle;
        template.headerHeight = _headerHeight;
        template.headerBackgroundColor = _headerBackgroundColor;
        template.headerBackgroundSprite = _headerBackgroundSprite;
        template.headerTextColor = _headerTextColor;
        template.headerFontSize = _headerFontSize;
        template.includeCloseButton = _includeCloseButton;
        template.closeButtonSprite = _closeButtonSprite;
        template.closeButtonColor = _closeButtonColor;

        template.includeFooter = _includeFooter;
        template.footerHeight = _footerHeight;
        template.footerBackgroundColor = _footerBackgroundColor;
        template.footerBackgroundSprite = _footerBackgroundSprite;
        template.addOkCancelButtons = _addOkCancelButtons;

        template.saveAsPrefab = _saveAsPrefab;
        template.prefabPath = _prefabPath;

        // UI Elements сохраняются отдельно через интерфейс управления элементами

        EditorUtility.SetDirty(template);
    }

    private void SaveAsNewTemplate()
    {
        if (string.IsNullOrWhiteSpace(_newTemplateName))
        {
            EditorUtility.DisplayDialog("Error", "New template name cannot be empty.", "OK");
            return;
        }

        if (!Directory.Exists(_templateAssetPath))
        {
            Directory.CreateDirectory(_templateAssetPath);
            AssetDatabase.Refresh();
        }

        string path = Path.Combine(_templateAssetPath, _newTemplateName + ".asset");
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        PanelTemplate newTemplate = ScriptableObject.CreateInstance<PanelTemplate>();
        PopulateTemplateFromCurrentSettings(newTemplate);

        AssetDatabase.CreateAsset(newTemplate, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _currentTemplate = newTemplate; // Optionally select the newly created template
        EditorUtility.DisplayDialog("Success", $"Template '{_newTemplateName}' saved at {path}", "OK");
    }

    private void UpdateExistingTemplate(PanelTemplate templateToUpdate)
    {
        if (templateToUpdate == null)
        {
            EditorUtility.DisplayDialog("Error", "No template selected to update.", "OK");
            return;
        }
        PopulateTemplateFromCurrentSettings(templateToUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Template '{templateToUpdate.name}' updated.", "OK");
    }


    private void GeneratePanel()
    {
        // Ensure Canvas exists (same as your original code)
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // Create root panel object
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
        if (_panelBackgroundSprite == null && panelImage.sprite == null) // Check if sprite is null also after assignment
        {
            panelImage.type = Image.Type.Sliced; // For stretching color
            // Ensure we have a default white sprite for slicing if no custom sprite is set
            // This prevents the "material not set" warning if only color is used.
            // A small 1x1 white pixel sprite in your project's Resources or assigned via editor is best.
            // For now, we'll rely on Unity's default UI sprite if available or color tinting alone.
        }


        GameObject contentAreaGO = CreateUIElement("ContentArea", panelRect);
        RectTransform contentRect = contentAreaGO.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        float topOffset = 0;
        float bottomOffset = 0;

        if (_includeHeader)
        {
            GameObject headerGO = CreateUIElement("Header", panelRect);
            RectTransform headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, _headerHeight);
            headerRect.anchoredPosition = Vector2.zero;

            Image headerImage = headerGO.AddComponent<Image>();
            headerImage.sprite = _headerBackgroundSprite;
            headerImage.color = _headerBackgroundColor;
            if (_headerBackgroundSprite == null && headerImage.sprite == null) headerImage.type = Image.Type.Sliced;

            GameObject headerTextGO = CreateTextElement("TitleText", headerRect, _headerTitle, _headerFontSize, _headerTextColor);
            RectTransform headerTextRect = headerTextGO.GetComponent<RectTransform>();
            headerTextRect.anchorMin = new Vector2(0, 0.5f);
            headerTextRect.anchorMax = new Vector2(1, 0.5f);
            headerTextRect.pivot = new Vector2(0.5f, 0.5f);
            headerTextRect.anchoredPosition = Vector2.zero;
            headerTextRect.offsetMin = new Vector2(10, 0);
            headerTextRect.offsetMax = new Vector2(_includeCloseButton ? -(_headerHeight - 10 + 5 + 5) : -10, 0); // Adjusted for button width and padding

            if (_useTextMeshPro)
            {
                #if UNITY_TEXTMESHPRO
                TextMeshProUGUI tmpText = headerTextGO.GetComponent<TextMeshProUGUI>();
                if(tmpText) tmpText.alignment = TextAlignmentOptions.Center;
                #endif
            }
            else
            {
                Text uiText = headerTextGO.GetComponent<Text>();
                if(uiText) uiText.alignment = TextAnchor.MiddleCenter;
            }

            if (_includeCloseButton)
            {
                GameObject closeButtonGO = CreateButtonElement("CloseButton", headerRect, ""); // No text by default if sprite is used
                RectTransform closeButtonRect = closeButtonGO.GetComponent<RectTransform>();
                closeButtonRect.anchorMin = new Vector2(1, 0.5f);
                closeButtonRect.anchorMax = new Vector2(1, 0.5f);
                closeButtonRect.pivot = new Vector2(1, 0.5f);
                float buttonSize = Mathf.Max(20, _headerHeight - 10); // Ensure min size
                closeButtonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
                closeButtonRect.anchoredPosition = new Vector2(-5, 0);

                Image closeButtonImage = closeButtonGO.GetComponent<Image>();
                closeButtonImage.sprite = _closeButtonSprite;
                closeButtonImage.color = _closeButtonColor; // This will tint the sprite or be the background if no sprite

                Transform textChild = closeButtonGO.transform.Find("Text"); // Standard button text child
                if (textChild)
                {
                    if (_closeButtonSprite != null)
                    {
                        DestroyImmediate(textChild.gameObject); // Remove text if icon is present
                    }
                    else
                    {
                        // Configure "X" text if no sprite
                        if (_useTextMeshPro)
                        {
                            #if UNITY_TEXTMESHPRO
                            TextMeshProUGUI btnTmpText = textChild.GetComponent<TextMeshProUGUI>();
                            if (btnTmpText) { btnTmpText.text = "X"; btnTmpText.color = ContrastColor(_closeButtonColor); btnTmpText.fontSize = _headerFontSize * 0.8f; }
                            #endif
                        }
                        else
                        {
                            Text btnUiText = textChild.GetComponent<Text>();
                            if (btnUiText) { btnUiText.text = "X"; btnUiText.color = ContrastColor(_closeButtonColor); btnUiText.fontSize = (int)(_headerFontSize * 0.8f); }
                        }
                        // If no sprite, make the button image color more distinct if it's too similar to header
                        if(closeButtonImage.color == _headerBackgroundColor) closeButtonImage.color = Color.gray;
                    }
                }
            }
            topOffset = _headerHeight;
        }

        if (_includeFooter)
        {
            GameObject footerGO = CreateUIElement("Footer", panelRect);
            RectTransform footerRect = footerGO.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(0, _footerHeight);
            footerRect.anchoredPosition = Vector2.zero;

            Image footerImage = footerGO.AddComponent<Image>();
            footerImage.sprite = _footerBackgroundSprite;
            footerImage.color = _footerBackgroundColor;
            if (_footerBackgroundSprite == null && footerImage.sprite == null) footerImage.type = Image.Type.Sliced;

            if (_addOkCancelButtons)
            {
                HorizontalLayoutGroup hlg = footerGO.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(10, 10, 5, 5); // top, bottom padding within footer
                hlg.spacing = 10;
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.childForceExpandHeight = false; // Let buttons control their height
                hlg.childForceExpandWidth = false;
                hlg.childControlHeight = true; // Use children's preferred height
                hlg.childControlWidth = false;

                // Кнопка Cancel
                GameObject cancelButtonGO = CreateButtonElement("CancelButton", footerRect, "Cancel");
                LayoutElement cancelLE = cancelButtonGO.AddComponent<LayoutElement>();
                cancelLE.preferredWidth = 80;
                cancelLE.preferredHeight = _footerHeight - hlg.padding.top - hlg.padding.bottom; // Fit in padding

                // Кнопка OK
                GameObject okButtonGO = CreateButtonElement("OkButton", footerRect, "OK");
                LayoutElement okLE = okButtonGO.AddComponent<LayoutElement>();
                okLE.preferredWidth = 80;
                okLE.preferredHeight = _footerHeight - hlg.padding.top - hlg.padding.bottom;
            }
            bottomOffset = _footerHeight;
        }

        contentRect.offsetMin = new Vector2(contentRect.offsetMin.x, bottomOffset);
        contentRect.offsetMax = new Vector2(contentRect.offsetMax.x, -topOffset);

        // Create UI Elements from template
        CreateUIElementsFromTemplate(contentAreaGO, _currentTemplate);

        Undo.RegisterCreatedObjectUndo(panelRootGO, "Create " + _panelName);
        Selection.activeGameObject = panelRootGO;

        if (_saveAsPrefab)
        {
            if (string.IsNullOrWhiteSpace(_prefabPath) || string.IsNullOrWhiteSpace(_panelName))
            {
                Debug.LogError("Prefab path or panel name is empty. Prefab not saved.");
                return;
            }

            string fullPath = Path.Combine(_prefabPath, _panelName + ".prefab");

            // Ensure the folder path ends with a slash for Directory.CreateDirectory
            string folderOnlyPath = _prefabPath;
            if (!folderOnlyPath.EndsWith("/") && !folderOnlyPath.EndsWith("\\"))
            {
                folderOnlyPath += "/";
            }
            // Use Path.GetDirectoryName to be safe with full paths or relative paths
            folderOnlyPath = Path.GetDirectoryName(folderOnlyPath); // This will correctly get "Assets/Prefabs/UI" from "Assets/Prefabs/UI/"

            if (!string.IsNullOrEmpty(folderOnlyPath) && !Directory.Exists(folderOnlyPath))
            {
                 try
                {
                    Directory.CreateDirectory(folderOnlyPath);
                    AssetDatabase.Refresh(); // Refresh to make sure Unity sees the new folder
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Could not create directory {_prefabPath}. Error: {e.Message}");
                    return;
                }
            }
            
            // Check again if directory exists after attempting to create it
            if (!string.IsNullOrEmpty(folderOnlyPath) && !Directory.Exists(folderOnlyPath))
            {
                 Debug.LogError($"Prefab directory '{folderOnlyPath}' does not exist and could not be created. Prefab not saved.");
                 return;
            }


            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            PrefabUtility.SaveAsPrefabAsset(panelRootGO, fullPath);
            Debug.Log($"Panel saved as prefab at: {fullPath}");
        }

        Debug.Log($"Panel '{_panelName}' generated successfully!");
    }


    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false); // false for worldPositionStays
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
            tmpText.alignment = TextAlignmentOptions.Left; // Default
            tmpText.raycastTarget = false;
            #endif
        }
        else
        {
            Text uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.color = color;
            
            // Попробуем получить шрифт, используя разные варианты для совместимости
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (defaultFont == null)
            {
                defaultFont = Resources.Load<Font>("Arial");
            }
            if (defaultFont != null)
            {
                uiText.font = defaultFont;
            }
            
            uiText.alignment = TextAnchor.MiddleLeft; // Default
            uiText.raycastTarget = false;
        }
        return go;
    }

    private GameObject CreateButtonElement(string name, Transform parent, string buttonText)
    {
        // Path to Unity's default UI sprites. Might need adjustment for very old/new Unity versions.
        // A common one is "UI/Skin/UISprite.psd"
        // Another is "UI/Skin/Background.psd" (often used by Panel)
        // For buttons, "UISprite" is usually the one with borders.
        Sprite defaultButtonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultButtonSprite == null) {
            // Fallback for older/different Unity versions, or if you want a simpler sprite
            defaultButtonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }


        GameObject buttonGO = CreateUIElement(name, parent);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.sprite = defaultButtonSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white; // Default button background color

        Button button = buttonGO.AddComponent<Button>();
        // You might want to set targetGraphic for the button
        button.targetGraphic = buttonImage;

        // Text for the button
        GameObject textGO = CreateTextElement("Text", buttonGO.transform, buttonText, 14, Color.black);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5,2); // Small padding for text
        textRect.offsetMax = new Vector2(-5,-2);

        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            TextMeshProUGUI tmpText = textGO.GetComponent<TextMeshProUGUI>();
            if(tmpText) {
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.color = new Color32(50, 50, 50, 255); // Standard dark text for UI
            }
            #endif
        }
        else
        {
            Text uiText = textGO.GetComponent<Text>();
            if(uiText) {
                uiText.alignment = TextAnchor.MiddleCenter;
                uiText.color = new Color32(50, 50, 50, 255); // Standard dark text for UI
            }
        }
        return buttonGO;
    }

    // Helper to determine good contrast text color (simple version)
    private Color ContrastColor(Color backgroundColor)
    {
        float luminance = (0.299f * backgroundColor.r + 0.587f * backgroundColor.g + 0.114f * backgroundColor.b);
        return luminance > 0.5f ? Color.black : Color.white;
    }

    #region UI Elements Management

    private void DrawUIElementsSection()
    {
        _showElementsSettings = EditorGUILayout.Foldout(_showElementsSettings, "UI Elements", true);
        if (_showElementsSettings)
        {
            EditorGUI.indentLevel++;

            // Content Layout Settings
            if (_currentTemplate != null)
            {
                // Убедимся, что layoutPadding инициализирован
                if (_currentTemplate.layoutPadding == null)
                {
                    _currentTemplate.layoutPadding = new RectOffset(10, 10, 10, 10);
                }

                EditorGUILayout.LabelField("Content Layout", EditorStyles.boldLabel);
                _currentTemplate.contentLayoutType = (LayoutType)EditorGUILayout.EnumPopup("Layout Type", _currentTemplate.contentLayoutType);
                
                if (_currentTemplate.contentLayoutType != LayoutType.Manual)
                {
                    _currentTemplate.layoutSpacing = EditorGUILayout.Vector2Field("Spacing", _currentTemplate.layoutSpacing);
                    _currentTemplate.layoutPadding.left = EditorGUILayout.IntField("Padding Left", _currentTemplate.layoutPadding.left);
                    _currentTemplate.layoutPadding.right = EditorGUILayout.IntField("Padding Right", _currentTemplate.layoutPadding.right);
                    _currentTemplate.layoutPadding.top = EditorGUILayout.IntField("Padding Top", _currentTemplate.layoutPadding.top);
                    _currentTemplate.layoutPadding.bottom = EditorGUILayout.IntField("Padding Bottom", _currentTemplate.layoutPadding.bottom);

                    if (_currentTemplate.contentLayoutType == LayoutType.GridLayout)
                    {
                        _currentTemplate.gridColumns = EditorGUILayout.IntField("Grid Columns", _currentTemplate.gridColumns);
                        _currentTemplate.gridCellSize = EditorGUILayout.Vector2Field("Cell Size", _currentTemplate.gridCellSize);
                    }
                }

                EditorGUILayout.Space();
            }

            // UI Elements List
            EditorGUILayout.LabelField("Elements List", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add New Element:", GUILayout.Width(120));
            _newElementType = (UIElementType)EditorGUILayout.EnumPopup(_newElementType, GUILayout.Width(120));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                AddNewUIElement(_newElementType);
            }
            EditorGUILayout.EndHorizontal();
            
            // Быстрые кнопки для популярных элементов
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quick Add:", GUILayout.Width(80));
            if (GUILayout.Button("Button", GUILayout.Width(60))) AddNewUIElement(UIElementType.Button);
            if (GUILayout.Button("Text", GUILayout.Width(50))) AddNewUIElement(UIElementType.Text);
            if (GUILayout.Button("Input", GUILayout.Width(55))) AddNewUIElement(UIElementType.InputField);
            if (GUILayout.Button("Image", GUILayout.Width(55))) AddNewUIElement(UIElementType.Image);
            if (GUILayout.Button("Slider", GUILayout.Width(55))) AddNewUIElement(UIElementType.Slider);
            if (GUILayout.Button("Toggle", GUILayout.Width(60))) AddNewUIElement(UIElementType.Toggle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);

            if (_currentTemplate != null && _currentTemplate.uiElements != null)
            {
                _elementsScrollPosition = EditorGUILayout.BeginScrollView(_elementsScrollPosition, GUILayout.Height(200));

                // Создаем копию массива для безопасной итерации
                UIElementData[] elementsArray = new UIElementData[_currentTemplate.uiElements.Length];
                System.Array.Copy(_currentTemplate.uiElements, elementsArray, _currentTemplate.uiElements.Length);

                for (int i = 0; i < elementsArray.Length; i++)
                {
                    // Проверяем, что индекс все еще валиден
                    if (i < _currentTemplate.uiElements.Length)
                    {
                        DrawUIElementEntry(i, _currentTemplate.uiElements[i]);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Load a template to edit UI elements, or save current settings as a new template.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }
    }

    private void DrawUIElementEntry(int index, UIElementData element)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        element.isActive = EditorGUILayout.Toggle(element.isActive, GUILayout.Width(20));
        
        bool isSelected = _selectedElementIndex == index;
        string elementIcon = GetElementIcon(element.elementType);
        string elementLabel = $"{elementIcon} {element.elementName} ({element.elementType})";
        if (GUILayout.Toggle(isSelected, elementLabel, "button"))
        {
            _selectedElementIndex = isSelected ? -1 : index;
        }

        if (GUILayout.Button("▲", GUILayout.Width(25)) && index > 0)
        {
            SwapUIElements(index, index - 1);
        }
        if (GUILayout.Button("▼", GUILayout.Width(25)) && index < _currentTemplate.uiElements.Length - 1)
        {
            SwapUIElements(index, index + 1);
        }
        bool shouldRemove = GUILayout.Button("×", GUILayout.Width(25));
        EditorGUILayout.EndHorizontal();

        if (!shouldRemove && _selectedElementIndex == index)
        {
            EditorGUI.indentLevel++;
            DrawUIElementDetails(element);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        
        if (shouldRemove)
        {
            RemoveUIElement(index);
        }
    }

    private void DrawUIElementDetails(UIElementData element)
    {
        EditorGUILayout.LabelField("Element Details", EditorStyles.boldLabel);
        
        element.elementName = EditorGUILayout.TextField("Name", element.elementName);
        element.elementType = (UIElementType)EditorGUILayout.EnumPopup("Type", element.elementType);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Position & Size", EditorStyles.boldLabel);
        element.anchorMin = EditorGUILayout.Vector2Field("Anchor Min", element.anchorMin);
        element.anchorMax = EditorGUILayout.Vector2Field("Anchor Max", element.anchorMax);
        element.anchoredPosition = EditorGUILayout.Vector2Field("Position", element.anchoredPosition);
        element.sizeDelta = EditorGUILayout.Vector2Field("Size", element.sizeDelta);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        element.backgroundColor = EditorGUILayout.ColorField("Background Color", element.backgroundColor);
        element.backgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background Sprite", element.backgroundSprite, typeof(Sprite), false);

        if (HasTextProperty(element.elementType))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            element.text = EditorGUILayout.TextField("Text", element.text);
            element.fontSize = EditorGUILayout.IntField("Font Size", element.fontSize);
            element.textColor = EditorGUILayout.ColorField("Text Color", element.textColor);
            element.textAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Text Alignment", element.textAlignment);
        }

        if (element.elementType == UIElementType.InputField)
        {
            element.inputPlaceholder = EditorGUILayout.TextField("Placeholder", element.inputPlaceholder);
            element.inputContentType = (InputField.ContentType)EditorGUILayout.EnumPopup("Content Type", element.inputContentType);
        }

        if (element.elementType == UIElementType.Slider)
        {
            element.sliderMinValue = EditorGUILayout.FloatField("Min Value", element.sliderMinValue);
            element.sliderMaxValue = EditorGUILayout.FloatField("Max Value", element.sliderMaxValue);
            element.sliderValue = EditorGUILayout.Slider("Value", element.sliderValue, element.sliderMinValue, element.sliderMaxValue);
        }

        if (element.elementType == UIElementType.Toggle)
        {
            element.toggleValue = EditorGUILayout.Toggle("Default Value", element.toggleValue);
        }

        if (element.elementType == UIElementType.Dropdown)
        {
            EditorGUILayout.LabelField("Dropdown Options");
            if (element.dropdownOptions == null) element.dropdownOptions = new string[1] { "Option 1" };
            
            for (int i = 0; i < element.dropdownOptions.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                element.dropdownOptions[i] = EditorGUILayout.TextField($"Option {i + 1}", element.dropdownOptions[i]);
                bool shouldRemoveOption = GUILayout.Button("-", GUILayout.Width(20)) && element.dropdownOptions.Length > 1;
                EditorGUILayout.EndHorizontal();
                
                if (shouldRemoveOption)
                {
                    var newOptions = new string[element.dropdownOptions.Length - 1];
                    System.Array.Copy(element.dropdownOptions, 0, newOptions, 0, i);
                    System.Array.Copy(element.dropdownOptions, i + 1, newOptions, i, element.dropdownOptions.Length - i - 1);
                    element.dropdownOptions = newOptions;
                    break;
                }
            }
            
            if (GUILayout.Button("Add Option"))
            {
                var newOptions = new string[element.dropdownOptions.Length + 1];
                System.Array.Copy(element.dropdownOptions, newOptions, element.dropdownOptions.Length);
                newOptions[element.dropdownOptions.Length] = $"Option {element.dropdownOptions.Length + 1}";
                element.dropdownOptions = newOptions;
            }
        }

        if (IsInteractiveElement(element.elementType))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interactive", EditorStyles.boldLabel);
            element.interactable = EditorGUILayout.Toggle("Interactable", element.interactable);
            if (element.elementType == UIElementType.Button)
            {
                element.onClickMethodName = EditorGUILayout.TextField("OnClick Method", element.onClickMethodName);
            }
        }
    }

    private bool HasTextProperty(UIElementType type)
    {
        return type == UIElementType.Button || type == UIElementType.Text || 
               type == UIElementType.InputField || type == UIElementType.Toggle ||
               type == UIElementType.Dropdown;
    }

    private bool IsInteractiveElement(UIElementType type)
    {
        return type == UIElementType.Button || type == UIElementType.InputField ||
               type == UIElementType.Slider || type == UIElementType.Toggle ||
               type == UIElementType.Dropdown;
    }

    private string GetElementIcon(UIElementType type)
    {
        switch (type)
        {
            case UIElementType.Button: return "🔘";
            case UIElementType.Text: return "📝";
            case UIElementType.InputField: return "📄";
            case UIElementType.Image: return "🖼️";
            case UIElementType.Slider: return "🎚️";
            case UIElementType.Toggle: return "☑️";
            case UIElementType.Dropdown: return "📋";
            case UIElementType.ScrollView: return "📜";
            case UIElementType.ProgressBar: return "📊";
            case UIElementType.Spacer: return "⬜";
            default: return "❓";
        }
    }

    private void AddNewUIElement(UIElementType elementType)
    {
        if (_currentTemplate == null)
        {
            EditorUtility.DisplayDialog("Error", "Please load a template first or save current settings as a new template.", "OK");
            return;
        }

        // Убедимся, что layoutPadding инициализирован
        if (_currentTemplate.layoutPadding == null)
        {
            _currentTemplate.layoutPadding = new RectOffset(10, 10, 10, 10);
        }

        var newElements = new UIElementData[_currentTemplate.uiElements.Length + 1];
        System.Array.Copy(_currentTemplate.uiElements, newElements, _currentTemplate.uiElements.Length);
        newElements[_currentTemplate.uiElements.Length] = new UIElementData(elementType);
        _currentTemplate.uiElements = newElements;
        _selectedElementIndex = _currentTemplate.uiElements.Length - 1;
        
        EditorUtility.SetDirty(_currentTemplate);
    }

    private void RemoveUIElement(int index)
    {
        if (_currentTemplate == null || index < 0 || index >= _currentTemplate.uiElements.Length) return;

        var newElements = new UIElementData[_currentTemplate.uiElements.Length - 1];
        System.Array.Copy(_currentTemplate.uiElements, 0, newElements, 0, index);
        System.Array.Copy(_currentTemplate.uiElements, index + 1, newElements, index, _currentTemplate.uiElements.Length - index - 1);
        _currentTemplate.uiElements = newElements;

        if (_selectedElementIndex >= index) _selectedElementIndex--;
        if (_selectedElementIndex >= _currentTemplate.uiElements.Length) _selectedElementIndex = -1;
        
        EditorUtility.SetDirty(_currentTemplate);
    }

    private void SwapUIElements(int indexA, int indexB)
    {
        if (_currentTemplate == null || indexA < 0 || indexB < 0 || 
            indexA >= _currentTemplate.uiElements.Length || indexB >= _currentTemplate.uiElements.Length) return;

        var temp = _currentTemplate.uiElements[indexA];
        _currentTemplate.uiElements[indexA] = _currentTemplate.uiElements[indexB];
        _currentTemplate.uiElements[indexB] = temp;

        if (_selectedElementIndex == indexA) _selectedElementIndex = indexB;
        else if (_selectedElementIndex == indexB) _selectedElementIndex = indexA;
        
        EditorUtility.SetDirty(_currentTemplate);
    }

    private void CreateUIElementsFromTemplate(GameObject contentArea, PanelTemplate template)
    {
        if (template == null || template.uiElements == null || template.uiElements.Length == 0)
            return;

        RectTransform contentRect = contentArea.GetComponent<RectTransform>();

        // Setup Layout Group if needed
        SetupContentLayout(contentArea, template);

        // Create UI Elements
        foreach (var elementData in template.uiElements)
        {
            if (elementData.isActive == false) continue;

            GameObject elementGO = CreateUIElementFromData(elementData, contentRect);
            if (elementGO != null)
            {
                // Setup position and size (for manual layout)
                if (template.contentLayoutType == LayoutType.Manual)
                {
                    SetupElementTransform(elementGO, elementData);
                }
            }
        }
    }

    private void SetupContentLayout(GameObject contentArea, PanelTemplate template)
    {
        switch (template.contentLayoutType)
        {
            case LayoutType.VerticalLayout:
                VerticalLayoutGroup vlg = contentArea.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = template.layoutSpacing.y;
                vlg.padding = template.layoutPadding;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                break;

            case LayoutType.HorizontalLayout:
                HorizontalLayoutGroup hlg = contentArea.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = template.layoutSpacing.x;
                hlg.padding = template.layoutPadding;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                break;

            case LayoutType.GridLayout:
                GridLayoutGroup glg = contentArea.AddComponent<GridLayoutGroup>();
                glg.spacing = template.layoutSpacing;
                glg.padding = template.layoutPadding;
                glg.cellSize = template.gridCellSize;
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = template.gridColumns;
                glg.childAlignment = TextAnchor.UpperLeft;
                break;

            case LayoutType.Manual:
            default:
                // Ручное позиционирование - ничего не добавляем
                break;
        }
    }

    private GameObject CreateUIElementFromData(UIElementData elementData, Transform parent)
    {
        GameObject elementGO = null;

        switch (elementData.elementType)
        {
            case UIElementType.Button:
                elementGO = CreateButtonFromData(elementData, parent);
                break;

            case UIElementType.Text:
                elementGO = CreateTextFromData(elementData, parent);
                break;

            case UIElementType.InputField:
                elementGO = CreateInputFieldFromData(elementData, parent);
                break;

            case UIElementType.Image:
                elementGO = CreateImageFromData(elementData, parent);
                break;

            case UIElementType.Slider:
                elementGO = CreateSliderFromData(elementData, parent);
                break;

            case UIElementType.Toggle:
                elementGO = CreateToggleFromData(elementData, parent);
                break;

            case UIElementType.Dropdown:
                elementGO = CreateDropdownFromData(elementData, parent);
                break;

            case UIElementType.ScrollView:
                elementGO = CreateScrollViewFromData(elementData, parent);
                break;

            case UIElementType.ProgressBar:
                elementGO = CreateProgressBarFromData(elementData, parent);
                break;

            case UIElementType.Spacer:
                elementGO = CreateSpacerFromData(elementData, parent);
                break;
        }

        if (elementGO != null)
        {
            elementGO.name = elementData.elementName;
            if (elementData.interactable == false && elementGO.GetComponent<Selectable>() != null)
            {
                elementGO.GetComponent<Selectable>().interactable = false;
            }
        }

        return elementGO;
    }

    private void SetupElementTransform(GameObject elementGO, UIElementData elementData)
    {
        RectTransform rectTransform = elementGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = elementData.anchorMin;
            rectTransform.anchorMax = elementData.anchorMax;
            rectTransform.anchoredPosition = elementData.anchoredPosition;
            rectTransform.sizeDelta = elementData.sizeDelta;
        }
    }

    private GameObject CreateButtonFromData(UIElementData elementData, Transform parent)
    {
        GameObject buttonGO = CreateButtonElement(elementData.elementName, parent, elementData.text);
        
        Image buttonImage = buttonGO.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = elementData.backgroundColor;
            if (elementData.backgroundSprite != null)
                buttonImage.sprite = elementData.backgroundSprite;
        }

        // Setup text appearance
        Transform textTransform = buttonGO.transform.Find("Text");
        if (textTransform != null)
        {
            if (_useTextMeshPro)
            {
                #if UNITY_TEXTMESHPRO
                TextMeshProUGUI tmpText = textTransform.GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = elementData.text;
                    tmpText.fontSize = elementData.fontSize;
                    tmpText.color = elementData.textColor;
                    tmpText.alignment = ConvertTextAnchorToTMP(elementData.textAlignment);
                }
                #endif
            }
            else
            {
                Text uiText = textTransform.GetComponent<Text>();
                if (uiText != null)
                {
                    uiText.text = elementData.text;
                    uiText.fontSize = elementData.fontSize;
                    uiText.color = elementData.textColor;
                    uiText.alignment = elementData.textAlignment;
                }
            }
        }

        return buttonGO;
    }

    private GameObject CreateTextFromData(UIElementData elementData, Transform parent)
    {
        GameObject textGO = CreateTextElement(elementData.elementName, parent, elementData.text, elementData.fontSize, elementData.textColor);
        
        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            TextMeshProUGUI tmpText = textGO.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.alignment = ConvertTextAnchorToTMP(elementData.textAlignment);
            }
            #endif
        }
        else
        {
            Text uiText = textGO.GetComponent<Text>();
            if (uiText != null)
            {
                uiText.alignment = elementData.textAlignment;
            }
        }

        return textGO;
    }

    private GameObject CreateInputFieldFromData(UIElementData elementData, Transform parent)
    {
        GameObject inputFieldGO = CreateUIElement(elementData.elementName, parent);
        
        Image backgroundImage = inputFieldGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;
        if (elementData.backgroundSprite != null)
            backgroundImage.sprite = elementData.backgroundSprite;

        InputField inputField = inputFieldGO.AddComponent<InputField>();
        inputField.contentType = elementData.inputContentType;

        // Создание текстового элемента для placeholder
        GameObject placeholderGO = CreateTextElement("Placeholder", inputFieldGO.transform, elementData.inputPlaceholder, elementData.fontSize, new Color(elementData.textColor.r, elementData.textColor.g, elementData.textColor.b, 0.5f));
        
        // Создание текстового элемента для текста
        GameObject textGO = CreateTextElement("Text", inputFieldGO.transform, "", elementData.fontSize, elementData.textColor);

        // Связывание с InputField
        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            inputField.textComponent = textGO.GetComponent<TextMeshProUGUI>();
            inputField.placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            #endif
        }
        else
        {
            inputField.textComponent = textGO.GetComponent<Text>();
            inputField.placeholder = placeholderGO.GetComponent<Text>();
        }

        return inputFieldGO;
    }

    private GameObject CreateImageFromData(UIElementData elementData, Transform parent)
    {
        GameObject imageGO = CreateUIElement(elementData.elementName, parent);
        
        Image image = imageGO.AddComponent<Image>();
        image.color = elementData.backgroundColor;
        if (elementData.backgroundSprite != null)
        {
            image.sprite = elementData.backgroundSprite;
            image.type = elementData.imageType;
        }

        return imageGO;
    }

    private GameObject CreateSliderFromData(UIElementData elementData, Transform parent)
    {
        GameObject sliderGO = CreateUIElement(elementData.elementName, parent);
        
        // Background
        Image backgroundImage = sliderGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = elementData.sliderMinValue;
        slider.maxValue = elementData.sliderMaxValue;
        slider.value = elementData.sliderValue;

        // Fill Area
        GameObject fillAreaGO = CreateUIElement("Fill Area", sliderGO.transform);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;

        // Fill
        GameObject fillGO = CreateUIElement("Fill", fillAreaGO.transform);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = elementData.textColor; // Use text color for fill
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        // Handle Slide Area
        GameObject handleSlideAreaGO = CreateUIElement("Handle Slide Area", sliderGO.transform);
        RectTransform handleSlideAreaRect = handleSlideAreaGO.GetComponent<RectTransform>();
        handleSlideAreaRect.anchorMin = Vector2.zero;
        handleSlideAreaRect.anchorMax = Vector2.one;
        handleSlideAreaRect.sizeDelta = new Vector2(-20, 0);
        handleSlideAreaRect.anchoredPosition = Vector2.zero;

        // Handle
        GameObject handleGO = CreateUIElement("Handle", handleSlideAreaGO.transform);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;
        handleRect.sizeDelta = new Vector2(20, 0);

        // Setup slider references
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;

        return sliderGO;
    }

    private GameObject CreateToggleFromData(UIElementData elementData, Transform parent)
    {
        GameObject toggleGO = CreateUIElement(elementData.elementName, parent);
        
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.isOn = elementData.toggleValue;

        // Background
        GameObject backgroundGO = CreateUIElement("Background", toggleGO.transform);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 1);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.sizeDelta = new Vector2(20, 20);
        backgroundRect.anchoredPosition = new Vector2(10, -10);

        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;

        // Checkmark
        GameObject checkmarkGO = CreateUIElement("Checkmark", backgroundGO.transform);
        RectTransform checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = Vector2.zero;
        checkmarkRect.anchoredPosition = Vector2.zero;

        Image checkmarkImage = checkmarkGO.AddComponent<Image>();
        checkmarkImage.color = elementData.textColor;

        // Label
        GameObject labelGO = CreateTextElement("Label", toggleGO.transform, elementData.text, elementData.fontSize, elementData.textColor);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(23, 0);
        labelRect.offsetMax = Vector2.zero;

        // Setup toggle references
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;

        return toggleGO;
    }

    private GameObject CreateDropdownFromData(UIElementData elementData, Transform parent)
    {
        GameObject dropdownGO = CreateUIElement(elementData.elementName, parent);
        
        Image backgroundImage = dropdownGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;

        Dropdown dropdown = dropdownGO.AddComponent<Dropdown>();
        
        // Label
        GameObject labelGO = CreateTextElement("Label", dropdownGO.transform, elementData.text, elementData.fontSize, elementData.textColor);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);

        // Arrow
        GameObject arrowGO = CreateUIElement("Arrow", dropdownGO.transform);
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        
        Image arrowImage = arrowGO.AddComponent<Image>();
        arrowImage.color = elementData.textColor;

        // Template не создаем здесь для простоты
        
        // Setup dropdown references
        if (_useTextMeshPro)
        {
            #if UNITY_TEXTMESHPRO
            dropdown.captionText = labelGO.GetComponent<TextMeshProUGUI>();
            #endif
        }
        else
        {
            dropdown.captionText = labelGO.GetComponent<Text>();
        }

        // Add options
        dropdown.options.Clear();
        foreach (string optionText in elementData.dropdownOptions)
        {
            dropdown.options.Add(new Dropdown.OptionData(optionText));
        }

        return dropdownGO;
    }

    private GameObject CreateScrollViewFromData(UIElementData elementData, Transform parent)
    {
        GameObject scrollViewGO = CreateUIElement(elementData.elementName, parent);
        
        Image backgroundImage = scrollViewGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;

        ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewportGO = CreateUIElement("Viewport", scrollViewGO.transform);
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;

        Image viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = Color.clear;
        Mask viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        // Content
        GameObject contentGO = CreateUIElement("Content", viewportGO.transform);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.sizeDelta = new Vector2(0, 300);
        contentRect.anchoredPosition = Vector2.zero;

        // Setup scroll rect references
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return scrollViewGO;
    }

    private GameObject CreateProgressBarFromData(UIElementData elementData, Transform parent)
    {
        GameObject progressBarGO = CreateUIElement(elementData.elementName, parent);
        
        // Background
        Image backgroundImage = progressBarGO.AddComponent<Image>();
        backgroundImage.color = elementData.backgroundColor;

        // Fill Area
        GameObject fillAreaGO = CreateUIElement("Fill Area", progressBarGO.transform);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;

        // Fill
        GameObject fillGO = CreateUIElement("Fill", fillAreaGO.transform);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = elementData.textColor;

        return progressBarGO;
    }

    private GameObject CreateSpacerFromData(UIElementData elementData, Transform parent)
    {
        GameObject spacerGO = CreateUIElement(elementData.elementName, parent);
        // Spacer - просто пустой объект для отступов
        return spacerGO;
    }

    #if UNITY_TEXTMESHPRO
    private TMPro.TextAlignmentOptions ConvertTextAnchorToTMP(TextAnchor textAnchor)
    {
        switch (textAnchor)
        {
            case TextAnchor.UpperLeft: return TMPro.TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TMPro.TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TMPro.TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TMPro.TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TMPro.TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TMPro.TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TMPro.TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TMPro.TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TMPro.TextAlignmentOptions.BottomRight;
            default: return TMPro.TextAlignmentOptions.Center;
        }
    }
    #endif

    #endregion
}