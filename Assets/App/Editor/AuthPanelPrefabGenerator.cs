using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Reflection;
using App.Develop.AppServices.Auth.UI;
using UnityEngine.EventSystems;

namespace App.Editor
{
    public class AuthPanelPrefabGenerator
    {
        private const string RESOURCES_FOLDER = "Assets/App/Resources";
        private const string UI_FOLDER = RESOURCES_FOLDER + "/UI";
        private const string PREFAB_PATH = UI_FOLDER + "/AuthPanel.prefab";

        [MenuItem("Tools/Generate Auth Panel Prefab")]
        public static void GeneratePrefab()
        {
            // 1) Убедимся, что папки есть
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
                AssetDatabase.CreateFolder("Assets/App", "Resources");
            if (!AssetDatabase.IsValidFolder(UI_FOLDER))
                AssetDatabase.CreateFolder(RESOURCES_FOLDER, "UI");

            // 2) Создаём базовый объект панели авторизации
            var authPanelGO = new GameObject("AuthPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = authPanelGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Устанавливаем высокое значение сортировки для приоритета отображения
            var scaler = authPanelGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Создаем EventSystem, если его нет на сцене
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            
            // Добавляем контроллер UI
            var authUIController = authPanelGO.AddComponent<global::App.Develop.CommonServices.Firebase.Auth.AuthUIController>();

            // 3) Создаём фон и структуру панелей
            // Создаем корневой объект для всего UI
            var mainContainer = CreateUIObject("MainContainer", authPanelGO.transform);
            var mainContainerRT = mainContainer.GetComponent<RectTransform>();
            mainContainerRT.anchorMin = Vector2.zero;
            mainContainerRT.anchorMax = Vector2.one;
            mainContainerRT.offsetMin = mainContainerRT.offsetMax = Vector2.zero;
            
            // Фоновое изображение (самый нижний слой)
            var backgroundGO = CreateUIObject("Background", mainContainer.transform);
            var backgroundRT = backgroundGO.GetComponent<RectTransform>();
            backgroundRT.anchorMin = Vector2.zero;
            backgroundRT.anchorMax = Vector2.one;
            backgroundRT.offsetMin = backgroundRT.offsetMax = Vector2.zero;
            backgroundRT.SetSiblingIndex(0); // Ставим на самый низ в иерархии
            var backgroundImg = backgroundGO.AddComponent<Image>();
            backgroundImg.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Цвет фона (бежевый/бумага)
            
            // Добавим фоновое изображение комнаты
            var roomBackgroundGO = CreateUIObject("RoomBackground", backgroundGO.transform);
            var roomBackgroundRT = roomBackgroundGO.GetComponent<RectTransform>();
            roomBackgroundRT.anchorMin = Vector2.zero;
            roomBackgroundRT.anchorMax = Vector2.one;
            roomBackgroundRT.offsetMin = roomBackgroundRT.offsetMax = Vector2.zero;
            var roomBackgroundImg = roomBackgroundGO.AddComponent<Image>();
            roomBackgroundImg.color = new Color(0.9f, 0.85f, 0.8f, 1f); // Более теплый оттенок фона

            // UI контейнер - все UI элементы будут в нем (поверх фона)
            var uiContainer = CreateUIObject("UIContainer", mainContainer.transform);
            var uiContainerRT = uiContainer.GetComponent<RectTransform>();
            uiContainerRT.anchorMin = Vector2.zero;
            uiContainerRT.anchorMax = Vector2.one;
            uiContainerRT.offsetMin = uiContainerRT.offsetMax = Vector2.zero;
            uiContainerRT.SetSiblingIndex(1); // Ставим поверх фона

            // 4) Создаём три панели: Авторизация, Верификация Email и Профиль
            // 4.1) Панель авторизации - стилизована как деревянная доска
            var authPanelContainer = CreatePanel("AuthPanelContainer", uiContainer.transform, new Vector2(700, 800));
            authPanelContainer.GetComponent<Image>().color = new Color(0.82f, 0.71f, 0.55f, 1f); // Более насыщенный цвет дерева
            // Активируем панель
            authPanelContainer.gameObject.SetActive(true);
            
            // Добавим рамку для доски
            var frameGO = CreateUIObject("WoodenFrame", authPanelContainer.transform);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = new Vector2(10, 10);
            frameRT.offsetMax = new Vector2(-10, -10);
            var frameImg = frameGO.AddComponent<Image>();
            frameImg.color = new Color(0.75f, 0.64f, 0.48f, 1f); // Более темное дерево для рамки
            
            var authPanelAnimator = authPanelContainer.AddComponent<UIAnimator>();
            var authPanelCanvasGroup = authPanelContainer.AddComponent<CanvasGroup>();
            // Настраиваем CanvasGroup для правильного показа/скрытия
            authPanelCanvasGroup.alpha = 1f;
            authPanelCanvasGroup.interactable = true;
            authPanelCanvasGroup.blocksRaycasts = true;
            
            // 4.2) Панель верификации email
            var emailVerificationContainer = CreatePanel("EmailVerificationContainer", uiContainer.transform, new Vector2(700, 600));
            emailVerificationContainer.GetComponent<Image>().color = new Color(0.82f, 0.71f, 0.55f, 1f);
            // Активируем панель для корректной инициализации компонентов
            emailVerificationContainer.gameObject.SetActive(true);
            
            // Добавим рамку для доски верификации
            var verifyFrameGO = CreateUIObject("WoodenFrameVerify", emailVerificationContainer.transform);
            var verifyFrameRT = verifyFrameGO.GetComponent<RectTransform>();
            verifyFrameRT.anchorMin = Vector2.zero;
            verifyFrameRT.anchorMax = Vector2.one;
            verifyFrameRT.offsetMin = new Vector2(10, 10);
            verifyFrameRT.offsetMax = new Vector2(-10, -10);
            var verifyFrameImg = verifyFrameGO.AddComponent<Image>();
            verifyFrameImg.color = new Color(0.75f, 0.64f, 0.48f, 1f);
            
            var emailVerificationAnimator = emailVerificationContainer.AddComponent<UIAnimator>();
            var emailVerificationCanvasGroup = emailVerificationContainer.AddComponent<CanvasGroup>();
            // Скрываем по умолчанию
            emailVerificationCanvasGroup.alpha = 0f;
            emailVerificationCanvasGroup.interactable = false;
            emailVerificationCanvasGroup.blocksRaycasts = false;
            emailVerificationContainer.SetActive(false);
            
            // 4.3) Панель профиля
            var profilePanelContainer = CreatePanel("ProfilePanelContainer", uiContainer.transform, new Vector2(700, 800));
            profilePanelContainer.GetComponent<Image>().color = new Color(0.82f, 0.71f, 0.55f, 1f);
            // Активируем панель для корректной инициализации компонентов
            profilePanelContainer.gameObject.SetActive(true);
            
            // Добавим рамку для доски профиля
            var profileFrameGO = CreateUIObject("WoodenFrameProfile", profilePanelContainer.transform);
            var profileFrameRT = profileFrameGO.GetComponent<RectTransform>();
            profileFrameRT.anchorMin = Vector2.zero;
            profileFrameRT.anchorMax = Vector2.one;
            profileFrameRT.offsetMin = new Vector2(10, 10);
            profileFrameRT.offsetMax = new Vector2(-10, -10);
            var profileFrameImg = profileFrameGO.AddComponent<Image>();
            profileFrameImg.color = new Color(0.75f, 0.64f, 0.48f, 1f);
            
            var profilePanelAnimator = profilePanelContainer.AddComponent<UIAnimator>();
            var profilePanelCanvasGroup = profilePanelContainer.AddComponent<CanvasGroup>();
            // Скрываем по умолчанию
            profilePanelCanvasGroup.alpha = 0f;
            profilePanelCanvasGroup.interactable = false;
            profilePanelCanvasGroup.blocksRaycasts = false;
            profilePanelContainer.SetActive(false);
            
            // Добавляем компоненты для настройки профиля пользователя
            CreateProfileSetupPanel(profileFrameGO, authPanelGO, authUIController);
            
            // 5) Наполняем панель авторизации
            // 5.1) Заголовок
            var titleGO = CreateTextObject("TitleLabel", frameGO.transform, "Авторизация", 36);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.sizeDelta = new Vector2(500, 80);
            titleRT.anchoredPosition = new Vector2(0, -60);
            var titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // 5.2) Поле ввода Email
            var emailLabel = CreateTextObject("EmailLabel", frameGO.transform, "Email:", 26);
            var emailLabelRT = emailLabel.GetComponent<RectTransform>();
            emailLabelRT.anchorMin = emailLabelRT.anchorMax = new Vector2(0.5f, 1f);
            emailLabelRT.sizeDelta = new Vector2(500, 40);
            emailLabelRT.anchoredPosition = new Vector2(0, -160);
            
            var emailInput = CreateInputField("EmailInput", frameGO.transform, "Введите email");
            var emailInputRT = emailInput.GetComponent<RectTransform>();
            emailInputRT.anchorMin = emailInputRT.anchorMax = new Vector2(0.5f, 1f);
            emailInputRT.sizeDelta = new Vector2(500, 60);
            emailInputRT.anchoredPosition = new Vector2(0, -210);
            
            // 5.3) Поле ввода пароля
            var passwordLabel = CreateTextObject("PasswordLabel", frameGO.transform, "Пароль:", 26);
            var passwordLabelRT = passwordLabel.GetComponent<RectTransform>();
            passwordLabelRT.anchorMin = passwordLabelRT.anchorMax = new Vector2(0.5f, 1f);
            passwordLabelRT.sizeDelta = new Vector2(500, 40);
            passwordLabelRT.anchoredPosition = new Vector2(0, -280);
            
            var passwordContainer = CreateUIObject("PasswordContainer", frameGO.transform);
            var passwordContainerRT = passwordContainer.GetComponent<RectTransform>();
            passwordContainerRT.anchorMin = passwordContainerRT.anchorMax = new Vector2(0.5f, 1f);
            passwordContainerRT.sizeDelta = new Vector2(500, 60);
            passwordContainerRT.anchoredPosition = new Vector2(0, -330);
            
            var passwordInput = CreateInputField("PasswordInput", passwordContainer.transform, "Введите пароль");
            var passwordInputRT = passwordInput.GetComponent<RectTransform>();
            passwordInputRT.anchorMin = new Vector2(0, 0);
            passwordInputRT.anchorMax = new Vector2(1, 1);
            passwordInputRT.sizeDelta = new Vector2(-40, 0); // Оставляем место для кнопки переключения
            passwordInputRT.anchoredPosition = new Vector2(-20, 0);
            var passwordInputField = passwordInput.GetComponent<TMP_InputField>();
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
            
            // Кнопка переключения видимости пароля
            var togglePasswordButton = CreateUIObject("TogglePasswordButton", passwordContainer.transform);
            var togglePasswordButtonRT = togglePasswordButton.GetComponent<RectTransform>();
            togglePasswordButtonRT.anchorMin = new Vector2(1, 0.5f);
            togglePasswordButtonRT.anchorMax = new Vector2(1, 0.5f);
            togglePasswordButtonRT.sizeDelta = new Vector2(40, 40);
            togglePasswordButtonRT.anchoredPosition = new Vector2(-20, 0);
            var togglePasswordButtonComp = togglePasswordButton.AddComponent<Button>();
            var togglePasswordBtnImage = togglePasswordButton.AddComponent<Image>();
            togglePasswordBtnImage.color = new Color(0.85f, 0.76f, 0.6f, 1f);
            
            // Добавляем иконку глаза для переключения
            var eyeIcon = CreateUIObject("EyeIcon", togglePasswordButton.transform);
            var eyeIconRT = eyeIcon.GetComponent<RectTransform>();
            eyeIconRT.anchorMin = Vector2.zero;
            eyeIconRT.anchorMax = Vector2.one;
            eyeIconRT.offsetMin = new Vector2(8, 8);
            eyeIconRT.offsetMax = new Vector2(-8, -8);
            var eyeIconImage = eyeIcon.AddComponent<Image>();
            eyeIconImage.color = new Color(0.3f, 0.2f, 0.15f, 1f);
            
            // Добавляем функциональность для переключения видимости пароля
            togglePasswordButtonComp.onClick.AddListener(delegate 
            {
                bool isPassword = passwordInputField.contentType == TMP_InputField.ContentType.Password;
                passwordInputField.contentType = isPassword ? 
                    TMP_InputField.ContentType.Standard : 
                    TMP_InputField.ContentType.Password;
                passwordInputField.ForceLabelUpdate();
                
                // Меняем цвет иконки для индикации состояния
                eyeIconImage.color = isPassword ? 
                    new Color(0.1f, 0.4f, 0.2f, 1f) : // Зеленоватый для видимого пароля
                    new Color(0.3f, 0.2f, 0.15f, 1f); // Стандартный для скрытого
            });
            
            // 5.4) Флажок "Запомнить меня"
            var rememberToggle = CreateToggle("RememberMeToggle", frameGO.transform, "Запомнить меня");
            var rememberToggleRT = rememberToggle.GetComponent<RectTransform>();
            rememberToggleRT.anchorMin = rememberToggleRT.anchorMax = new Vector2(0.5f, 1f);
            rememberToggleRT.sizeDelta = new Vector2(300, 40);
            rememberToggleRT.anchoredPosition = new Vector2(0, -400);
            
            // 5.5) Кнопки логина и регистрации
            var loginButton = CreateButton("LoginButton", frameGO.transform, "Войти");
            var loginButtonRT = loginButton.GetComponent<RectTransform>();
            loginButtonRT.anchorMin = loginButtonRT.anchorMax = new Vector2(0.5f, 0);
            loginButtonRT.sizeDelta = new Vector2(350, 70);
            loginButtonRT.anchoredPosition = new Vector2(0, 180);
            
            var registerButton = CreateButton("RegisterButton", frameGO.transform, "Зарегистрироваться");
            var registerButtonRT = registerButton.GetComponent<RectTransform>();
            registerButtonRT.anchorMin = registerButtonRT.anchorMax = new Vector2(0.5f, 0);
            registerButtonRT.sizeDelta = new Vector2(350, 70);
            registerButtonRT.anchoredPosition = new Vector2(0, 90);
            
            // 6) Наполняем панель верификации email
            var verifyTitle = CreateTextObject("VerifyTitle", verifyFrameGO.transform, "Подтверждение Email", 32);
            var verifyTitleRT = verifyTitle.GetComponent<RectTransform>();
            verifyTitleRT.anchorMin = verifyTitleRT.anchorMax = new Vector2(0.5f, 1f);
            verifyTitleRT.sizeDelta = new Vector2(500, 80);
            verifyTitleRT.anchoredPosition = new Vector2(0, -60);
            verifyTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            var verifyText = CreateTextObject("VerifyText", verifyFrameGO.transform, "Мы отправили письмо с подтверждением на ваш email.\nПожалуйста, откройте его и нажмите на ссылку для подтверждения.", 24);
            var verifyTextRT = verifyText.GetComponent<RectTransform>();
            verifyTextRT.anchorMin = verifyTextRT.anchorMax = new Vector2(0.5f, 1f);
            verifyTextRT.sizeDelta = new Vector2(500, 100);
            verifyTextRT.anchoredPosition = new Vector2(0, -150);
            verifyText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            var checkVerificationButton = CreateButton("CheckVerificationButton", verifyFrameGO.transform, "Проверить статус");
            var checkVerificationButtonRT = checkVerificationButton.GetComponent<RectTransform>();
            checkVerificationButtonRT.anchorMin = checkVerificationButtonRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkVerificationButtonRT.sizeDelta = new Vector2(350, 70);
            checkVerificationButtonRT.anchoredPosition = new Vector2(0, 40);
            
            var resendVerificationButton = CreateButton("ResendVerificationButton", verifyFrameGO.transform, "Отправить повторно");
            var resendVerificationButtonRT = resendVerificationButton.GetComponent<RectTransform>();
            resendVerificationButtonRT.anchorMin = resendVerificationButtonRT.anchorMax = new Vector2(0.5f, 0.5f);
            resendVerificationButtonRT.sizeDelta = new Vector2(350, 70);
            resendVerificationButtonRT.anchoredPosition = new Vector2(0, -50);
            
            // 7) Создаём всплывающую панель для обоих окон - поместим её в конец иерархии, чтобы была поверх всего
            var popupPanel = CreatePanel("PopupPanel", uiContainer.transform, new Vector2(550, 250));
            var popupPanelRT = popupPanel.GetComponent<RectTransform>();
            popupPanelRT.anchorMin = popupPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            popupPanelRT.anchoredPosition = Vector2.zero;
            popupPanel.transform.SetAsLastSibling(); // Ставим на самый верх в иерархии
            
            // Добавляем canvas group для всплывающей панели
            var popupCanvasGroup = popupPanel.AddComponent<CanvasGroup>();
            // Стилизация всплывающей панели как бумажная записка
            var popupImg = popupPanel.GetComponent<Image>();
            popupImg.color = new Color(0.98f, 0.96f, 0.90f, 1f); // Цвет бумаги
            
            // Добавим рамку для записки
            var popupFrameGO = CreateUIObject("NoteFrame", popupPanel.transform);
            var popupFrameRT = popupFrameGO.GetComponent<RectTransform>();
            popupFrameRT.anchorMin = Vector2.zero;
            popupFrameRT.anchorMax = Vector2.one;
            popupFrameRT.offsetMin = new Vector2(5, 5);
            popupFrameRT.offsetMax = new Vector2(-5, -5);
            var popupFrameImg = popupFrameGO.AddComponent<Image>();
            popupFrameImg.color = new Color(0.95f, 0.93f, 0.86f, 1f); // Цвет внутренней части записки
            
            var popupText = CreateTextObject("PopupText", popupFrameGO.transform, "Сообщение", 26);
            var popupTextRT = popupText.GetComponent<RectTransform>();
            popupTextRT.anchorMin = Vector2.zero;
            popupTextRT.anchorMax = Vector2.one;
            popupTextRT.offsetMin = new Vector2(20, 20);
            popupTextRT.offsetMax = new Vector2(-20, -20);
            var popupTextComponent = popupText.GetComponent<TextMeshProUGUI>();
            popupTextComponent.alignment = TextAlignmentOptions.Center;
            popupTextComponent.color = new Color(0.3f, 0.2f, 0.15f, 1f); // Темный цвет для текста
            
            // Скрываем всплывающую панель по умолчанию
            popupPanel.SetActive(false);
            
            // 8) Настройка event handlers
            var loginBtn = loginButton.GetComponent<Button>();
            var registerBtn = registerButton.GetComponent<Button>();
            var checkVerificationBtn = checkVerificationButton.GetComponent<Button>();
            var resendVerificationBtn = resendVerificationButton.GetComponent<Button>();
            
            UnityEditor.Events.UnityEventTools.AddPersistentListener(loginBtn.onClick, authUIController.OnLoginButtonClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(registerBtn.onClick, authUIController.OnRegisterButtonClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(checkVerificationBtn.onClick, authUIController.OnCheckEmailVerifiedButtonClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resendVerificationBtn.onClick, authUIController.OnSendVerificationEmailButtonClicked);
            
            // 9) Устанавливаем ссылки в контроллере
            var serializedController = new SerializedObject(authUIController);
            serializedController.FindProperty("_emailInput").objectReferenceValue = emailInput.GetComponent<TMP_InputField>();
            serializedController.FindProperty("_passwordInput").objectReferenceValue = passwordInputField;
            serializedController.FindProperty("_rememberMeToggle").objectReferenceValue = rememberToggle.GetComponent<Toggle>();
            serializedController.FindProperty("_authPanelAnimator").objectReferenceValue = authPanelAnimator;
            serializedController.FindProperty("_emailVerificationAnimator").objectReferenceValue = emailVerificationAnimator;
            serializedController.FindProperty("_profilePanelAnimator").objectReferenceValue = profilePanelAnimator;
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupTextComponent;
            serializedController.ApplyModifiedProperties();
            
            // Убедимся, что UIAnimator правильно настроен
            var authAnimatorField = typeof(UIAnimator).GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (authAnimatorField != null)
            {
                authAnimatorField.SetValue(authPanelAnimator, authPanelCanvasGroup);
                authAnimatorField.SetValue(emailVerificationAnimator, emailVerificationCanvasGroup);
                authAnimatorField.SetValue(profilePanelAnimator, profilePanelCanvasGroup);
            }
            
            // 10) Сохраняем в префаб
            PrefabUtility.SaveAsPrefabAsset(authPanelGO, PREFAB_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"✅ Auth Panel префаб сохранён в {PREFAB_PATH}");
            
            // 11) Чистим сцену
            GameObject.DestroyImmediate(authPanelGO);
            GameObject.DestroyImmediate(eventSystem);
        }
        
        private static void CreateProfileSetupPanel(GameObject parent, GameObject rootObject, global::App.Develop.CommonServices.Firebase.Auth.AuthUIController authUIController)
        {
            // Добавляем ProfileSetupUI
            var profileSetupUI = rootObject.AddComponent<global::App.Develop.CommonServices.Firebase.Auth.ProfileSetupUI>();
            
            // Создаем UI элементы для панели настройки профиля
            
            // Заголовок
            var titleGO = CreateTextObject("TitleLabel", parent.transform, "Настройка профиля", 36);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.sizeDelta = new Vector2(500, 80);
            titleRT.anchoredPosition = new Vector2(0, -60);
            var titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            
            // Поле ввода Nickname
            var nicknameLabel = CreateTextObject("NicknameLabel", parent.transform, "Никнейм:", 26);
            var nicknameLabelRT = nicknameLabel.GetComponent<RectTransform>();
            nicknameLabelRT.anchorMin = nicknameLabelRT.anchorMax = new Vector2(0.5f, 1f);
            nicknameLabelRT.sizeDelta = new Vector2(500, 40);
            nicknameLabelRT.anchoredPosition = new Vector2(0, -160);
            
            var nicknameInput = CreateInputField("NicknameInput", parent.transform, "Введите никнейм (только латиница)");
            var nicknameInputRT = nicknameInput.GetComponent<RectTransform>();
            nicknameInputRT.anchorMin = nicknameInputRT.anchorMax = new Vector2(0.5f, 1f);
            nicknameInputRT.sizeDelta = new Vector2(500, 60);
            nicknameInputRT.anchoredPosition = new Vector2(0, -210);
            
            // Выпадающее меню для пола
            var genderLabel = CreateTextObject("GenderLabel", parent.transform, "Пол:", 26);
            var genderLabelRT = genderLabel.GetComponent<RectTransform>();
            genderLabelRT.anchorMin = genderLabelRT.anchorMax = new Vector2(0.5f, 1f);
            genderLabelRT.sizeDelta = new Vector2(500, 40);
            genderLabelRT.anchoredPosition = new Vector2(0, -280);
            
            var genderDropdownGO = CreateUIObject("GenderDropdown", parent.transform);
            var genderDropdownRT = genderDropdownGO.GetComponent<RectTransform>();
            genderDropdownRT.anchorMin = genderDropdownRT.anchorMax = new Vector2(0.5f, 1f);
            genderDropdownRT.sizeDelta = new Vector2(500, 60);
            genderDropdownRT.anchoredPosition = new Vector2(0, -330);
            var genderDropdownImg = genderDropdownGO.AddComponent<Image>();
            genderDropdownImg.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Цвет бумаги
            
            var genderDropdown = genderDropdownGO.AddComponent<TMP_Dropdown>();
            
            // Настраиваем элементы выпадающего списка
            var dropdownItems = new System.Collections.Generic.List<string> { "Мужской", "Женский", "Другой" };
            var dropdownOptions = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var item in dropdownItems)
            {
                dropdownOptions.Add(new TMP_Dropdown.OptionData(item));
            }
            genderDropdown.options = dropdownOptions;
            
            // Создаем текст для отображения выбранного значения
            var labelGO = CreateTextObject("Label", genderDropdownGO.transform, "Выберите пол", 24);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0);
            labelRT.anchorMax = new Vector2(1, 1);
            labelRT.offsetMin = new Vector2(10, 0);
            labelRT.offsetMax = new Vector2(-35, 0);
            var labelText = labelGO.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Left;
            
            // Создаем иконку стрелки для выпадающего списка
            var arrowGO = CreateUIObject("Arrow", genderDropdownGO.transform);
            var arrowRT = arrowGO.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0.5f);
            arrowRT.anchorMax = new Vector2(1, 0.5f);
            arrowRT.sizeDelta = new Vector2(20, 20);
            arrowRT.anchoredPosition = new Vector2(-15, 0);
            var arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.color = new Color(0.3f, 0.2f, 0.15f, 1f); // Темно-коричневый
            
            // Шаблон элемента списка
            var templateGO = CreateUIObject("Template", genderDropdownGO.transform);
            var templateRT = templateGO.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.sizeDelta = new Vector2(0, 180); // Высота для трех элементов
            templateRT.anchoredPosition = new Vector2(0, 0);
            var templateImg = templateGO.AddComponent<Image>();
            templateImg.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Цвет бумаги
            
            // Создаем ScrollRect для шаблона
            var scrollView = templateGO.AddComponent<ScrollRect>();
            var viewportGO = CreateUIObject("Viewport", templateGO.transform);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.anchoredPosition = Vector2.zero;
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0.95f, 0.9f, 0.85f, 1f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            
            var contentGO = CreateUIObject("Content", viewportGO.transform);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.sizeDelta = new Vector2(0, 180); // Высота контента для трех элементов
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.pivot = new Vector2(0.5f, 1);
            
            // Настраиваем ScrollRect
            scrollView.content = contentRT;
            scrollView.viewport = viewportRT;
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.scrollSensitivity = 15;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            
            // Создаем элемент списка
            var itemGO = CreateUIObject("Item", contentGO.transform);
            var itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 1);
            itemRT.anchorMax = new Vector2(1, 1);
            itemRT.sizeDelta = new Vector2(0, 60);
            itemRT.anchoredPosition = new Vector2(0, 0);
            itemRT.pivot = new Vector2(0.5f, 1);
            var itemImg = itemGO.AddComponent<Image>();
            itemImg.color = new Color(0.95f, 0.9f, 0.85f, 0);
            
            // Добавляем toggle для элемента списка
            var itemToggle = itemGO.AddComponent<Toggle>();
            var itemTextGO = CreateTextObject("ItemText", itemGO.transform, "Опция", 24);
            var itemTextRT = itemTextGO.GetComponent<RectTransform>();
            itemTextRT.anchorMin = Vector2.zero;
            itemTextRT.anchorMax = Vector2.one;
            itemTextRT.offsetMin = new Vector2(10, 0);
            itemTextRT.offsetMax = Vector2.zero;
            var itemText = itemTextGO.GetComponent<TextMeshProUGUI>();
            itemText.alignment = TextAlignmentOptions.Left;
            
            // Настраиваем toggle
            itemToggle.targetGraphic = itemImg;
            var toggleColors = itemToggle.colors;
            toggleColors.normalColor = new Color(0.95f, 0.9f, 0.85f, 0);
            toggleColors.highlightedColor = new Color(0.9f, 0.85f, 0.8f, 1f);
            toggleColors.pressedColor = new Color(0.85f, 0.8f, 0.75f, 1f);
            toggleColors.selectedColor = new Color(0.9f, 0.85f, 0.8f, 1f);
            itemToggle.colors = toggleColors;
            
            // Связываем все компоненты выпадающего списка
            genderDropdown.captionText = labelText;
            genderDropdown.template = templateRT;
            genderDropdown.itemText = itemText;
            
            // Кнопки завершения настройки профиля
            var continueButton = CreateButton("ContinueButton", parent.transform, "Продолжить");
            var continueButtonRT = continueButton.GetComponent<RectTransform>();
            continueButtonRT.anchorMin = continueButtonRT.anchorMax = new Vector2(0.5f, 0);
            continueButtonRT.sizeDelta = new Vector2(350, 70);
            continueButtonRT.anchoredPosition = new Vector2(0, 180);
            
            var skipButton = CreateButton("SkipButton", parent.transform, "Пропустить");
            var skipButtonRT = skipButton.GetComponent<RectTransform>();
            skipButtonRT.anchorMin = skipButtonRT.anchorMax = new Vector2(0.5f, 0);
            skipButtonRT.sizeDelta = new Vector2(350, 70);
            skipButtonRT.anchoredPosition = new Vector2(0, 90);
            
            // Всплывающая панель для ошибок настройки профиля
            var profilePopupPanel = CreatePanel("ProfilePopupPanel", parent.transform, new Vector2(550, 250));
            profilePopupPanel.transform.SetAsLastSibling(); // Поверх всех элементов
            var profilePopupPanelRT = profilePopupPanel.GetComponent<RectTransform>();
            profilePopupPanelRT.anchorMin = profilePopupPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            profilePopupPanelRT.anchoredPosition = Vector2.zero;
            
            // Добавляем canvas group для всплывающей панели
            var profilePopupCanvasGroup = profilePopupPanel.AddComponent<CanvasGroup>();
            // Стилизация всплывающей панели как бумажная записка
            var profilePopupImg = profilePopupPanel.GetComponent<Image>();
            profilePopupImg.color = new Color(0.98f, 0.96f, 0.90f, 1f); // Цвет бумаги
            
            // Добавим рамку для записки
            var profilePopupFrameGO = CreateUIObject("NoteFrame", profilePopupPanel.transform);
            var profilePopupFrameRT = profilePopupFrameGO.GetComponent<RectTransform>();
            profilePopupFrameRT.anchorMin = Vector2.zero;
            profilePopupFrameRT.anchorMax = Vector2.one;
            profilePopupFrameRT.offsetMin = new Vector2(5, 5);
            profilePopupFrameRT.offsetMax = new Vector2(-5, -5);
            var profilePopupFrameImg = profilePopupFrameGO.AddComponent<Image>();
            profilePopupFrameImg.color = new Color(0.95f, 0.93f, 0.86f, 1f); // Цвет внутренней части записки
            
            var profilePopupText = CreateTextObject("ProfilePopupText", profilePopupFrameGO.transform, "Сообщение", 26);
            var profilePopupTextRT = profilePopupText.GetComponent<RectTransform>();
            profilePopupTextRT.anchorMin = Vector2.zero;
            profilePopupTextRT.anchorMax = Vector2.one;
            profilePopupTextRT.offsetMin = new Vector2(20, 20);
            profilePopupTextRT.offsetMax = new Vector2(-20, -20);
            var profilePopupTextComponent = profilePopupText.GetComponent<TextMeshProUGUI>();
            profilePopupTextComponent.alignment = TextAlignmentOptions.Center;
            profilePopupTextComponent.color = new Color(0.3f, 0.2f, 0.15f, 1f); // Темный цвет для текста
            
            // Скрываем всплывающую панель по умолчанию
            profilePopupPanel.SetActive(false);
            
            // Назначаем обработчики кнопок
            var continueBtn = continueButton.GetComponent<Button>();
            var skipBtn = skipButton.GetComponent<Button>();
            
            UnityEditor.Events.UnityEventTools.AddPersistentListener(continueBtn.onClick, profileSetupUI.OnContinueProfile);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(skipBtn.onClick, profileSetupUI.OnSkipProfile);
            
            // Устанавливаем ссылки в ProfileSetupUI
            var serializedProfileUI = new SerializedObject(profileSetupUI);
            serializedProfileUI.FindProperty("_nicknameInput").objectReferenceValue = nicknameInput.GetComponent<TMP_InputField>();
            serializedProfileUI.FindProperty("_genderDropdown").objectReferenceValue = genderDropdown;
            serializedProfileUI.FindProperty("_popupPanel").objectReferenceValue = profilePopupPanel;
            serializedProfileUI.FindProperty("_popupText").objectReferenceValue = profilePopupTextComponent;
            serializedProfileUI.FindProperty("_authUIController").objectReferenceValue = authUIController;
            serializedProfileUI.ApplyModifiedProperties();
        }
        
        #region Helper Methods
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
        
        private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var panel = CreateUIObject(name, parent);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = size;
            panelRT.anchoredPosition = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.8f, 0.7f, 0.5f, 1f); // Цвет дерева
            return panel;
        }
        
        private static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
        {
            var textObject = CreateUIObject(name, parent);
            var textComp = textObject.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = new Color(0.3f, 0.2f, 0.15f, 1f); // Темный текст
            return textObject;
        }
        
        private static GameObject CreateInputField(string name, Transform parent, string placeholder)
        {
            var inputGO = CreateUIObject(name, parent);
            var inputImg = inputGO.AddComponent<Image>();
            inputImg.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Цвет бумаги
            
            // Добавим более стильную рамку для поля ввода
            var inputFrameGO = CreateUIObject("InputFrame", inputGO.transform);
            var inputFrameRT = inputFrameGO.GetComponent<RectTransform>();
            inputFrameRT.anchorMin = Vector2.zero;
            inputFrameRT.anchorMax = Vector2.one;
            inputFrameRT.offsetMin = new Vector2(2, 2);
            inputFrameRT.offsetMax = new Vector2(-2, -2);
            var inputFrameImg = inputFrameGO.AddComponent<Image>();
            inputFrameImg.color = new Color(0.98f, 0.94f, 0.9f, 1f); // Немного светлее основного цвета
            
            var inputField = inputGO.AddComponent<TMP_InputField>();
            
            // Создаем текстовое поле для ввода
            var textGO = CreateUIObject("Text", inputFrameGO.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 5);
            textRT.offsetMax = new Vector2(-10, -5);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24;
            text.color = new Color(0.3f, 0.2f, 0.15f, 1f);
            text.alignment = TextAlignmentOptions.Left;
            
            // Создаем плейсхолдер
            var placeholderGO = CreateUIObject("Placeholder", inputFrameGO.transform);
            var placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(10, 5);
            placeholderRT.offsetMax = new Vector2(-10, -5);
            var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 22;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = new Color(0.3f, 0.2f, 0.15f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            
            // Привязываем все к InputField
            inputField.textViewport = textRT;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            
            return inputGO;
        }
        
        private static GameObject CreateToggle(string name, Transform parent, string label)
        {
            var toggleGO = CreateUIObject(name, parent);
            var toggle = toggleGO.AddComponent<Toggle>();
            
            // Создаем фон для чекбокса как деревянную коробочку
            var background = CreateUIObject("Background", toggleGO.transform);
            var backgroundRT = background.GetComponent<RectTransform>();
            backgroundRT.anchorMin = new Vector2(0, 0.5f);
            backgroundRT.anchorMax = new Vector2(0, 0.5f);
            backgroundRT.sizeDelta = new Vector2(32, 32);
            backgroundRT.anchoredPosition = new Vector2(16, 0);
            var backgroundImg = background.AddComponent<Image>();
            backgroundImg.color = new Color(0.85f, 0.76f, 0.6f, 1f); // Цвет дерева
            
            // Внутренняя часть чекбокса
            var checkboxInner = CreateUIObject("Inner", background.transform);
            var checkboxInnerRT = checkboxInner.GetComponent<RectTransform>();
            checkboxInnerRT.anchorMin = Vector2.zero;
            checkboxInnerRT.anchorMax = Vector2.one;
            checkboxInnerRT.offsetMin = new Vector2(2, 2);
            checkboxInnerRT.offsetMax = new Vector2(-2, -2);
            var checkboxInnerImg = checkboxInner.AddComponent<Image>();
            checkboxInnerImg.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Цвет бумаги внутри
            
            // Создаем галочку
            var checkmark = CreateUIObject("Checkmark", checkboxInner.transform);
            var checkmarkRT = checkmark.GetComponent<RectTransform>();
            checkmarkRT.anchorMin = Vector2.zero;
            checkmarkRT.anchorMax = Vector2.one;
            checkmarkRT.offsetMin = new Vector2(5, 5);
            checkmarkRT.offsetMax = new Vector2(-5, -5);
            var checkmarkImg = checkmark.AddComponent<Image>();
            checkmarkImg.color = new Color(0.3f, 0.2f, 0.15f, 1f); // Темный цвет галочки
            
            // Создаем текст метки
            var labelGO = CreateTextObject("Label", toggleGO.transform, label, 22);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.5f);
            labelRT.anchorMax = new Vector2(1, 0.5f);
            labelRT.offsetMin = new Vector2(50, -15);
            labelRT.offsetMax = new Vector2(0, 15);
            labelGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            
            // Настраиваем Toggle
            toggle.targetGraphic = checkboxInnerImg;
            toggle.graphic = checkmarkImg;
            
            return toggleGO;
        }
        
        private static GameObject CreateButton(string name, Transform parent, string text)
        {
            var buttonGO = CreateUIObject(name, parent);
            var buttonImg = buttonGO.AddComponent<Image>();
            buttonImg.color = new Color(0.8f, 0.7f, 0.5f, 1f); // Цвет дерева
            
            // Добавим внутреннюю часть кнопки для эффекта деревянной таблички
            var buttonInnerGO = CreateUIObject("ButtonInner", buttonGO.transform);
            var buttonInnerRT = buttonInnerGO.GetComponent<RectTransform>();
            buttonInnerRT.anchorMin = Vector2.zero;
            buttonInnerRT.anchorMax = Vector2.one;
            buttonInnerRT.offsetMin = new Vector2(5, 5);
            buttonInnerRT.offsetMax = new Vector2(-5, -5);
            var buttonInnerImg = buttonInnerGO.AddComponent<Image>();
            buttonInnerImg.color = new Color(0.85f, 0.76f, 0.6f, 1f); // Чуть светлее основного
            
            var button = buttonGO.AddComponent<Button>();
            
            // Настраиваем цвета для разных состояний кнопки
            var colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.76f, 0.6f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.83f, 0.7f, 1f);
            colors.pressedColor = new Color(0.75f, 0.66f, 0.5f, 1f);
            button.colors = colors;
            
            // Создаем текст кнопки
            var textGO = CreateTextObject("Text", buttonInnerGO.transform, text, 26);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 5);
            textRT.offsetMax = new Vector2(-5, -5);
            var buttonText = textGO.GetComponent<TextMeshProUGUI>();
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;
            
            return buttonGO;
        }
        #endregion
    }
} 