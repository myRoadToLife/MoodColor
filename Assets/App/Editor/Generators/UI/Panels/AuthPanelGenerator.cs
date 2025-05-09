using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Reflection;
using App.Develop.AppServices.Auth.UI;
using UnityEngine.EventSystems;
using System.IO; // Добавлено для Path.Combine и Directory

namespace App.Editor.Generators.UI.Panels // Изменено пространство имен для соответствия новой структуре
{
    public static class AuthPanelGenerator // Класс сделан статическим, так как все методы статические
    {
        // private const string RESOURCES_FOLDER = "Assets/App/Resources"; // Больше не используется напрямую для сохранения
        // private const string UI_FOLDER = RESOURCES_FOLDER + "/UI"; // Больше не используется напрямую для сохранения
        private const string PREFAB_SAVE_FOLDER_PATH = "Assets/App/Prefabs/Generated/UI/Panels/Auth/";
        private const string PREFAB_NAME = "AuthPanel";
        private const string DEFAULT_AUTH_STYLE_PATH = "Assets/App/Data/UIStyles/Configs/DefaultAuthPanelStyle.asset"; // Путь к нашему SO

        // Путь к ресурсам для UI (шрифты, спрайты), если они будут загружаться динамически
        // private const string UI_RESOURCES_PATH = "Assets/App/Resources/UI"; // Пока закомментировано, если понадобится

        [MenuItem("MoodColor/Generate/UI Panels/Auth/Auth Panel")]
        public static void GeneratePrefab()
        {
            AuthPanelStyleSO styleSO = AssetDatabase.LoadAssetAtPath<AuthPanelStyleSO>(DEFAULT_AUTH_STYLE_PATH);
            if (styleSO == null)
            {
                Debug.LogError($"[AuthPanelGenerator] Не удалось загрузить AuthPanelStyleSO по пути: {DEFAULT_AUTH_STYLE_PATH}. Убедитесь, что ассет существует и путь указан верно.");
                return;
            }

            // Создаем EventSystem, если его нет на сцене, чтобы UI работал корректно
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                 var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // 1. Создание корневого GameObject и настройка Canvas
            var authRootGO = new GameObject("AuthRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = authRootGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Отображение поверх всего
            canvas.sortingOrder = 100; // Порядок сортировки, чтобы был поверх других Canvas (если есть)
            var scaler = authRootGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // Масштабирование с размером экрана
            scaler.referenceResolution = new Vector2(1080, 1920); // Базовое разрешение (портретное)
            scaler.matchWidthOrHeight = 0.5f; // Баланс между шириной и высотой при масштабировании

            // 2. Добавление AuthUIController и инициализация SerializedObject для связывания полей
            var authUIController = authRootGO.AddComponent<global::App.Develop.CommonServices.Firebase.Auth.AuthUIController>();
            SerializedObject serializedAuthUIController = new SerializedObject(authUIController);

            // 3. Создание основных контейнеров (MainContainer, Background, UIContainer)
            var mainContainer = CreateUIObject("MainContainer", authRootGO.transform);
            var mainContainerRT = mainContainer.GetComponent<RectTransform>();
            mainContainerRT.anchorMin = Vector2.zero; // Растягиваем на весь родительский элемент (AuthRoot)
            mainContainerRT.anchorMax = Vector2.one;
            mainContainerRT.offsetMin = mainContainerRT.offsetMax = Vector2.zero;

            var backgroundGO = CreateUIObject("Background", mainContainer.transform);
            var backgroundRT = backgroundGO.GetComponent<RectTransform>();
            backgroundRT.anchorMin = Vector2.zero; // Растягиваем на весь MainContainer
            backgroundRT.anchorMax = Vector2.one;
            backgroundRT.offsetMin = backgroundRT.offsetMax = Vector2.zero;
            backgroundRT.SetSiblingIndex(0); // Фон должен быть позади других элементов UI
            var backgroundImg = backgroundGO.AddComponent<Image>();
            backgroundImg.color = styleSO.GlobalBackgroundColor; // Используем цвет из SO

            var uiContainer = CreateUIObject("UIContainer", mainContainer.transform);
            var uiContainerRT = uiContainer.GetComponent<RectTransform>();
            uiContainerRT.anchorMin = Vector2.zero; // Контейнер для активных UI элементов, также растянут
            uiContainerRT.anchorMax = Vector2.one;
            uiContainerRT.offsetMin = uiContainerRT.offsetMax = Vector2.zero;
            uiContainerRT.SetSiblingIndex(1); // Поверх фона

            // 4. Создание панели Входа (LoginPanel)
            var loginPanelGO = CreatePanel("LoginPanel", uiContainer.transform, new Vector2(700, 0)); // Высота 0 для автоподгонки
            var loginPanelImage = loginPanelGO.GetComponent<Image>();
            loginPanelImage.color = styleSO.LoginPanelStyle.BackgroundColor; // Используем цвет из SO
            // Если в styleSO.LoginPanelStyle.BackgroundSprite есть спрайт, используем его
            if (styleSO.LoginPanelStyle.BackgroundSprite != null)
            {
                loginPanelImage.sprite = styleSO.LoginPanelStyle.BackgroundSprite;
                loginPanelImage.type = styleSO.LoginPanelStyle.BackgroundImageType;
            }
            var loginPanelAnimator = loginPanelGO.AddComponent<UIAnimator>();
            SetupAnimatorAndCanvasGroup(loginPanelGO, loginPanelAnimator, true); // Панель входа активна и видима по умолчанию
            TrySetObjectReference(serializedAuthUIController, "_loginPanel", loginPanelGO);
            TrySetObjectReference(serializedAuthUIController, "_authPanelAnimator", loginPanelAnimator); // Основной аниматор панели авторизации теперь ссылается на аниматор панели входа

            // 5. Создание панели Регистрации (RegisterPanel)
            var registerPanelGO = CreatePanel("RegisterPanel", uiContainer.transform, new Vector2(700, 0)); // Высота 0 для автоподгонки
            var registerPanelImage = registerPanelGO.GetComponent<Image>();
            registerPanelImage.color = styleSO.RegisterPanelStyle.BackgroundColor;
            if (styleSO.RegisterPanelStyle.BackgroundSprite != null)
            {
                registerPanelImage.sprite = styleSO.RegisterPanelStyle.BackgroundSprite;
                registerPanelImage.type = styleSO.RegisterPanelStyle.BackgroundImageType;
            }
            var registerPanelAnimator = registerPanelGO.AddComponent<UIAnimator>();
            SetupAnimatorAndCanvasGroup(registerPanelGO, registerPanelAnimator, false); // Скрыта по умолчанию
            TrySetObjectReference(serializedAuthUIController, "_registerPanel", registerPanelGO);

            // 6. Создание панели Сброса пароля (ResetPasswordPanel)
            var resetPasswordPanelGO = CreatePanel("ResetPasswordPanel", uiContainer.transform, new Vector2(700, 0)); // Высота 0 для автоподгонки
            var resetPasswordPanelImage = resetPasswordPanelGO.GetComponent<Image>();
            resetPasswordPanelImage.color = styleSO.ResetPasswordPanelStyle.BackgroundColor;
            if (styleSO.ResetPasswordPanelStyle.BackgroundSprite != null)
            {
                resetPasswordPanelImage.sprite = styleSO.ResetPasswordPanelStyle.BackgroundSprite;
                resetPasswordPanelImage.type = styleSO.ResetPasswordPanelStyle.BackgroundImageType;
            }
            var resetPasswordPanelAnimator = resetPasswordPanelGO.AddComponent<UIAnimator>();
            SetupAnimatorAndCanvasGroup(resetPasswordPanelGO, resetPasswordPanelAnimator, false); // Скрыта по умолчанию
            TrySetObjectReference(serializedAuthUIController, "_resetPasswordPanel", resetPasswordPanelGO);

            // 7. Создание общего текстового поля для сообщений (ошибки, информация)
            var messageTextGO = CreateTextObject("MessageText", uiContainer.transform, string.Empty, 24, styleSO);
            var messageTextRT = messageTextGO.GetComponent<RectTransform>();
            messageTextRT.anchorMin = new Vector2(0.5f, 0f); // Якорь внизу по центру uiContainer
            messageTextRT.anchorMax = new Vector2(0.5f, 0f);
            messageTextRT.pivot = new Vector2(0.5f, 0f); // Точка вращения там же
            messageTextRT.sizeDelta = new Vector2(680, 80); // Шире и выше для многострочных сообщений
            messageTextRT.anchoredPosition = new Vector2(0, 30); // Расположено под основной областью панелей
            var messageTextComp = messageTextGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(messageTextComp, styleSO.MessageTextStyle, styleSO);
            messageTextComp.enableWordWrapping = true; // Перенос слов оставим здесь, т.к. это свойство компонента, а не стиля
            TrySetObjectReference(serializedAuthUIController, "_messageText", messageTextComp);

            // --- Создание контента для каждой панели ---
            CreateLoginPanelContent(loginPanelGO, serializedAuthUIController, styleSO);
            CreateRegisterPanelContent(registerPanelGO, serializedAuthUIController, styleSO);
            CreateResetPasswordPanelContent(resetPasswordPanelGO, serializedAuthUIController, styleSO);

            // --- Панели подтверждения Email и настройки профиля (Пока заглушки/наследие) ---
            // Сохраняются, так как AuthUIController имеет поля аниматоров для них.
            // Их содержимое и интеграция могут потребовать пересмотра.
            var emailVerificationContainer = CreatePanel("EmailVerificationContainer", uiContainer.transform, new Vector2(700, 0)); // Высота 0
            var emailVerificationImage = emailVerificationContainer.GetComponent<Image>();
            emailVerificationImage.color = styleSO.EmailVerificationPanelStyle.BackgroundColor;
            if (styleSO.EmailVerificationPanelStyle.BackgroundSprite != null)
            {
                emailVerificationImage.sprite = styleSO.EmailVerificationPanelStyle.BackgroundSprite;
                emailVerificationImage.type = styleSO.EmailVerificationPanelStyle.BackgroundImageType;
            }
            var emailVerificationAnimator = emailVerificationContainer.AddComponent<UIAnimator>();
            // CreateEmailVerificationPanelContent(emailVerificationContainer, serializedAuthUIController); // Содержимое пока не определено
            TrySetObjectReference(serializedAuthUIController, "_emailVerificationAnimator", emailVerificationAnimator);

            var profilePanelContainer = CreatePanel("ProfilePanelContainer", uiContainer.transform, new Vector2(700, 0)); // Высота 0
            var profilePanelImage = profilePanelContainer.GetComponent<Image>();
            profilePanelImage.color = styleSO.ProfilePanelStyle.BackgroundColor;
            if (styleSO.ProfilePanelStyle.BackgroundSprite != null)
            {
                profilePanelImage.sprite = styleSO.ProfilePanelStyle.BackgroundSprite;
                profilePanelImage.type = styleSO.ProfilePanelStyle.BackgroundImageType;
            }
            var profilePanelAnimator = profilePanelContainer.AddComponent<UIAnimator>();
            // CreateProfileSetupPanelContent(profilePanelContainer, serializedAuthUIController); // Содержимое пока не определено
            TrySetObjectReference(serializedAuthUIController, "_profilePanelAnimator", profilePanelAnimator);

            // --- Применение изменений SerializedObject и сохранение префаба ---
            serializedAuthUIController.ApplyModifiedProperties(); // Применяем все установленные ссылки

            string fullPrefabPath = Path.Combine(PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME + ".prefab");
            // Убедимся, что директория для сохранения существует
            if (!Directory.Exists(PREFAB_SAVE_FOLDER_PATH))
            {
                Directory.CreateDirectory(PREFAB_SAVE_FOLDER_PATH);
                AssetDatabase.Refresh(); // Обновляем базу данных ассетов, чтобы Unity "увидела" новую папку
            }

            bool success;
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPrefabPath);
            if (existingPrefab != null)
            {
                // Если префаб уже существует, заменяем его
                PrefabUtility.SaveAsPrefabAssetAndConnect(authRootGO, fullPrefabPath, InteractionMode.AutomatedAction);
                success = true;
            }
            else
            {
                // Иначе создаем новый префаб
                PrefabUtility.SaveAsPrefabAsset(authRootGO, fullPrefabPath, out success);
            }

            if (success)
                Debug.Log($"[AuthPanelGenerator] Префаб AuthPanel успешно сгенерирован по пути: {fullPrefabPath}");
            else
                Debug.LogError("[AuthPanelGenerator] Не удалось сгенерировать префаб AuthPanel.");

            // Уничтожаем временный GameObject со сцены, если мы не в режиме Play Mode
            if (!Application.isPlaying)
            {
                GameObject.DestroyImmediate(authRootGO);
            }
        }

        /// <summary>
        /// Настраивает CanvasGroup и UIAnimator для панели, устанавливает начальную видимость.
        /// </summary>
        private static void SetupAnimatorAndCanvasGroup(GameObject panelGO, UIAnimator animator, bool startVisible)
        {
            var canvasGroup = panelGO.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panelGO.AddComponent<CanvasGroup>();

            // Предполагается, что UIAnimator сам инициализирует свои значения по умолчанию для анимаций.
            // AuthUIController будет вызывать Show()/Hide() на этих аниматорах.

            if (startVisible)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                panelGO.SetActive(true);
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                panelGO.SetActive(false); // Также деактивируем GameObject
            }
        }

        /// <summary>
        /// Создает содержимое для панели входа.
        /// </summary>
        private static void CreateLoginPanelContent(GameObject loginPanelGO, SerializedObject serializedAuthUIController, AuthPanelStyleSO styleSO)
        {
            var frameGO = CreateUIObject("WoodenFrame_Login", loginPanelGO.transform);
            SetupFrame(frameGO, styleSO.LoginPanelStyle, styleSO);

            // Добавляем VerticalLayoutGroup и ContentSizeFitter к frameGO
            var frameVLG = frameGO.AddComponent<VerticalLayoutGroup>();
            frameVLG.padding = new RectOffset(
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding,
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding
            );
            frameVLG.spacing = styleSO.ItemSpacing;
            frameVLG.childAlignment = TextAnchor.UpperCenter; 
            frameVLG.childControlWidth = true;    
            frameVLG.childControlHeight = true;   
            frameVLG.childForceExpandWidth = true; 
            frameVLG.childForceExpandHeight = false; 

            var frameCSF = frameGO.AddComponent<ContentSizeFitter>();
            frameCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Элементы теперь будут добавляться как дочерние к frameGO и их порядок и размеры будет контролировать VLG
            SetupTitle(CreateTextObject("TitleLabel_Login", frameGO.transform, "Вход", 36, styleSO), styleSO);
            SetupLabel(CreateTextObject("EmailLabel_Login", frameGO.transform, "Email:", 26, styleSO), styleSO);
            
            var emailInputGO = CreateInputField("EmailInput_Login", frameGO.transform, "Введите email", styleSO);
            SetupInputFieldRT(emailInputGO, styleSO); // SetupInputFieldRT теперь настраивает LayoutElement
            TrySetObjectReference(serializedAuthUIController, "_emailInput", emailInputGO.GetComponent<TMP_InputField>());

            SetupLabel(CreateTextObject("PasswordLabel_Login", frameGO.transform, "Пароль:", 26, styleSO), styleSO);
            // Контейнер для поля пароля и кнопки "глаза" также должен быть частью VLG
            var passwordContainer = CreateUIObject("PasswordContainer_Login", frameGO.transform);
            // Для passwordContainer тоже нужен LayoutElement, чтобы он имел высоту DefaultInputSize.y
            var passwordContainerLE = passwordContainer.AddComponent<LayoutElement>();
            passwordContainerLE.preferredHeight = styleSO.DefaultInputSize.y;
            // SetupInputFieldRT(passwordContainer, styleSO); // Не вызываем для простого контейнера, он сам не InputField
            // У passwordContainer должен быть HorizontalLayoutGroup для поля ввода и кнопки "глаз"
            var passwordContainerHLG = passwordContainer.AddComponent<HorizontalLayoutGroup>();
            passwordContainerHLG.childControlHeight = true;
            passwordContainerHLG.childForceExpandHeight = true; // Растянуть по высоте контейнера
            // passwordContainerHLG.spacing = 5; // Небольшой отступ между полем и кнопкой
            // Можно добавить padding, если нужно
            
            var passwordInputGO = CreateInputField("PasswordInput_Login", passwordContainer.transform, "Введите пароль", styleSO);
            // Убираем ручную настройку RectTransform, т.к. HLG будет управлять
            // var passwordInputRT = passwordInputGO.GetComponent<RectTransform>();
            // passwordInputRT.anchorMin = Vector2.zero; 
            // passwordInputRT.anchorMax = Vector2.one;
            // passwordInputRT.offsetMin = new Vector2(0, 0); 
            // passwordInputRT.offsetMax = new Vector2(-55, 0); 
            var passwordInputLE = passwordInputGO.GetComponent<LayoutElement>(); // У InputField уже есть LayoutElement от SetupInputFieldRT
            if(passwordInputLE == null) passwordInputLE = passwordInputGO.AddComponent<LayoutElement>();
            passwordInputLE.flexibleWidth = 1; // Поле ввода занимает все доступное место в HLG

            var passwordInputField = passwordInputGO.GetComponent<TMP_InputField>();
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
            TrySetObjectReference(serializedAuthUIController, "_passwordInput", passwordInputField);
            // CreatePasswordToggle теперь должен добавлять кнопку "глаз" как элемент HLG
            CreatePasswordToggle(passwordContainer.transform, passwordInputField, styleSO); 

            var rememberToggleGO = CreateToggle("RememberMeToggle_Login", frameGO.transform, "Запомнить меня", styleSO);
            // Убираем ручную настройку RectTransform для rememberToggleGO
            // var rememberToggleRT = rememberToggleGO.GetComponent<RectTransform>();
            // rememberToggleRT.anchorMin = new Vector2(0.5f, 1f); ...
            // rememberToggleRT.anchoredPosition = new Vector2(0, -400);
            var rememberToggleLE = rememberToggleGO.GetComponent<LayoutElement>();
            if(rememberToggleLE == null) rememberToggleLE = rememberToggleGO.AddComponent<LayoutElement>();
            // Предпочтительную высоту для Toggle можно задать, если текст не помещается или для выравнивания
            // rememberToggleLE.preferredHeight = 40; // Например
            TrySetObjectReference(serializedAuthUIController, "_rememberMeToggle", rememberToggleGO.GetComponent<Toggle>());

            // Кнопки добавляются в frameGO, их порядок и высота управляются VLG и LayoutElement (из SetupButtonRT)
            var loginButtonGO = CreateButton("LoginButton", frameGO.transform, "Войти", styleSO);
            SetupButtonRT(loginButtonGO, styleSO);
            TrySetObjectReference(serializedAuthUIController, "_loginButton", loginButtonGO.GetComponent<Button>());

            var switchToRegisterButtonGO = CreateButton("SwitchToRegisterButton", frameGO.transform, "Создать аккаунт", styleSO);
            SetupButtonRT(switchToRegisterButtonGO, styleSO);
            TrySetObjectReference(serializedAuthUIController, "_switchToRegisterButton", switchToRegisterButtonGO.GetComponent<Button>());
            
            var forgotPasswordButtonGO = CreateButton("ForgotPasswordButton", frameGO.transform, "Забыли пароль?", styleSO);
            // SetupButtonRT(forgotPasswordButtonGO, styleSO); // SetupButtonRT уже вызван внутри CreateButton, если он там есть
            // Для кнопки "Забыли пароль?" с особым размером, если он не из DefaultButtonSize
            var forgotPasswordButtonLE = forgotPasswordButtonGO.GetComponent<LayoutElement>();
            if (forgotPasswordButtonLE != null) // LayoutElement уже добавлен в SetupButtonRT
            {
                 forgotPasswordButtonLE.preferredHeight = 50; // Кастомная высота
            }
            // var forgotPasswordButtonRT = forgotPasswordButtonGO.GetComponent<RectTransform>(); ... // Удаляем ручную настройку RT
            var forgotPasswordText = forgotPasswordButtonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (forgotPasswordText != null) ApplyTextStyle(forgotPasswordText, styleSO.ButtonTextStyle, styleSO); // Применяем стиль текста
            // if (forgotPasswordText != null) forgotPasswordText.fontSize = 22; // Управляется ButtonTextStyle
            TrySetObjectReference(serializedAuthUIController, "_forgotPasswordButton", forgotPasswordButtonGO.GetComponent<Button>());
        }

        /// <summary>
        /// Создает содержимое для панели регистрации.
        /// </summary>
        private static void CreateRegisterPanelContent(GameObject registerPanelGO, SerializedObject serializedAuthUIController, AuthPanelStyleSO styleSO)
        {
            var frameGO = CreateUIObject("WoodenFrame_Register", registerPanelGO.transform);
            SetupFrame(frameGO, styleSO.RegisterPanelStyle, styleSO);

            // Добавляем VerticalLayoutGroup и ContentSizeFitter к frameGO
            var frameVLG = frameGO.AddComponent<VerticalLayoutGroup>();
            frameVLG.padding = new RectOffset(
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding,
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding
            );
            frameVLG.spacing = styleSO.ItemSpacing;
            frameVLG.childAlignment = TextAnchor.UpperCenter;
            frameVLG.childControlWidth = true;
            frameVLG.childControlHeight = true;
            frameVLG.childForceExpandWidth = true;
            frameVLG.childForceExpandHeight = false;

            var frameCSF = frameGO.AddComponent<ContentSizeFitter>();
            frameCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetupTitle(CreateTextObject("TitleLabel_Register", frameGO.transform, "Регистрация", 36, styleSO), styleSO);
            
            SetupLabel(CreateTextObject("EmailLabel_Register", frameGO.transform, "Email:", 26, styleSO), styleSO);
            var emailPlaceholder = CreateUIObject("EmailInput_Placeholder_Register", frameGO.transform);
            SetupInputFieldRT(emailPlaceholder, styleSO); // Настроит LayoutElement для высоты
            AddPlaceholderImage(emailPlaceholder, styleSO); 

            SetupLabel(CreateTextObject("PasswordLabel_Register", frameGO.transform, "Пароль:", 26, styleSO), styleSO);
            var passwordPlaceholder = CreateUIObject("PasswordInput_Placeholder_Register", frameGO.transform);
            SetupInputFieldRT(passwordPlaceholder, styleSO); // Настроит LayoutElement для высоты
            AddPlaceholderImage(passwordPlaceholder, styleSO);

            SetupLabel(CreateTextObject("ConfirmPasswordLabel_Register", frameGO.transform, "Повторите пароль:", 26, styleSO), styleSO);
            
            // Контейнер для поля "Повторите пароль" и кнопки "глаза"
            var confirmPasswordContainer = CreateUIObject("ConfirmPasswordContainer_Register", frameGO.transform);
            var confirmPasswordContainerLE = confirmPasswordContainer.AddComponent<LayoutElement>();
            confirmPasswordContainerLE.preferredHeight = styleSO.DefaultInputSize.y; // Такая же высота, как у обычного поля ввода
            
            var confirmPasswordContainerHLG = confirmPasswordContainer.AddComponent<HorizontalLayoutGroup>();
            confirmPasswordContainerHLG.childControlHeight = true;
            confirmPasswordContainerHLG.childForceExpandHeight = true;
            // confirmPasswordContainerHLG.spacing = 5; // Если нужен отступ

            var confirmPasswordInputGO = CreateInputField("ConfirmPasswordInput_Register", confirmPasswordContainer.transform, "Повторите пароль", styleSO);
            var confirmPasswordInputLE = confirmPasswordInputGO.GetComponent<LayoutElement>();
            if(confirmPasswordInputLE == null) confirmPasswordInputLE = confirmPasswordInputGO.AddComponent<LayoutElement>();
            confirmPasswordInputLE.flexibleWidth = 1; // Поле ввода занимает все доступное место
            // Убираем ручную настройку RectTransform для confirmPasswordInputGO, HLG управляет
            // var confirmPasswordInputRT = confirmPasswordInputGO.GetComponent<RectTransform>();
            // confirmPasswordInputRT.anchorMin = Vector2.zero; ...

            var confirmPasswordInputField = confirmPasswordInputGO.GetComponent<TMP_InputField>();
            confirmPasswordInputField.contentType = TMP_InputField.ContentType.Password;
            TrySetObjectReference(serializedAuthUIController, "_confirmPasswordInput", confirmPasswordInputField);
            CreatePasswordToggle(confirmPasswordContainer.transform, confirmPasswordInputField, styleSO, "ToggleConfirmPasswordButton_Register");

            var registerButtonGO = CreateButton("RegisterButton", frameGO.transform, "Зарегистрироваться", styleSO);
            SetupButtonRT(registerButtonGO, styleSO);
            TrySetObjectReference(serializedAuthUIController, "_registerButton", registerButtonGO.GetComponent<Button>());

            var switchToLoginButtonGO = CreateButton("SwitchToLoginButton_FromRegister", frameGO.transform, "Уже есть аккаунт? Войти", styleSO);
            // SetupButtonRT(switchToLoginButtonGO, styleSO); // SetupButtonRT уже вызван внутри CreateButton
            // Для кнопки с особым размером (шире из-за текста)
            var switchToLoginButtonLE = switchToLoginButtonGO.GetComponent<LayoutElement>();
            if (switchToLoginButtonLE != null) // LayoutElement уже добавлен в SetupButtonRT
            {
                // switchToLoginButtonLE.preferredWidth = 450; // Если нужна фиксированная ширина, а не растягивание
                // Пока VLG будет растягивать по ширине фрейма. Высота из DefaultButtonSize.
            }
            // var switchToLoginButtonRT = switchToLoginButtonGO.GetComponent<RectTransform>(); // Удаляем ручную настройку
            TrySetObjectReference(serializedAuthUIController, "_switchToLoginButton", switchToLoginButtonGO.GetComponent<Button>());
        }

        /// <summary>
        /// Создает содержимое для панели сброса пароля.
        /// </summary>
        private static void CreateResetPasswordPanelContent(GameObject resetPanelGO, SerializedObject serializedAuthUIController, AuthPanelStyleSO styleSO)
        {
            var frameGO = CreateUIObject("WoodenFrame_Reset", resetPanelGO.transform);
            SetupFrame(frameGO, styleSO.ResetPasswordPanelStyle, styleSO);

            // Добавляем VerticalLayoutGroup и ContentSizeFitter к frameGO
            var frameVLG = frameGO.AddComponent<VerticalLayoutGroup>();
            frameVLG.padding = new RectOffset(
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding,
                (int)styleSO.DefaultPadding, (int)styleSO.DefaultPadding
            );
            frameVLG.spacing = styleSO.ItemSpacing;
            frameVLG.childAlignment = TextAnchor.UpperCenter;
            frameVLG.childControlWidth = true;
            frameVLG.childControlHeight = true;
            frameVLG.childForceExpandWidth = true;
            frameVLG.childForceExpandHeight = false;

            var frameCSF = frameGO.AddComponent<ContentSizeFitter>();
            frameCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetupTitle(CreateTextObject("TitleLabel_Reset", frameGO.transform, "Сброс пароля", 36, styleSO), styleSO);

            SetupLabel(CreateTextObject("EmailLabel_Reset", frameGO.transform, "Email для сброса:", 26, styleSO), styleSO);
            var resetEmailInputGO = CreateInputField("ResetEmailInput", frameGO.transform, "Введите email", styleSO);
            SetupInputFieldRT(resetEmailInputGO, styleSO);
            TrySetObjectReference(serializedAuthUIController, "_resetPasswordEmailInput", resetEmailInputGO.GetComponent<TMP_InputField>());

            var sendResetEmailButtonGO = CreateButton("SendResetEmailButton", frameGO.transform, "Отправить ссылку", styleSO);
            SetupButtonRT(sendResetEmailButtonGO, styleSO);
            // Для этой кнопки раньше был размер (400, 70). Если DefaultButtonSize отличается, 
            // и нужен именно такой размер, можно настроить LayoutElement этой кнопки:
            // var sendResetEmailButtonLE = sendResetEmailButtonGO.GetComponent<LayoutElement>();
            // if (sendResetEmailButtonLE != null) sendResetEmailButtonLE.preferredWidth = 400;
            TrySetObjectReference(serializedAuthUIController, "_sendResetEmailButton", sendResetEmailButtonGO.GetComponent<Button>());

            var backToLoginButtonGO = CreateButton("BackToLoginButton_FromReset", frameGO.transform, "Вернуться ко входу", styleSO);
            SetupButtonRT(backToLoginButtonGO, styleSO);
            TrySetObjectReference(serializedAuthUIController, "_backToLoginButton", backToLoginButtonGO.GetComponent<Button>());
        }

        #region Вспомогательные методы для настройки UI элементов

        /// <summary>
        /// Настраивает RectTransform для стандартной кнопки.
        /// Теперь высота кнопки будет задаваться через LayoutElement, а не напрямую.
        /// </summary>
        private static void SetupButtonRT(GameObject buttonGO, AuthPanelStyleSO styleSO)
        {
            var buttonRT = buttonGO.GetComponent<RectTransform>();
            // Для VerticalLayoutGroup обычно лучше, чтобы дочерние элементы имели стандартные якоря (например, stretch)
            // но т.к. VLG управляет позицией и размером (если childControlSize = true), эти значения могут быть не так важны.
            // Оставим пока так, VLG должен справиться.
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f); // Центр, если VLG не управляет якорями
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);
            // buttonRT.sizeDelta = styleSO.DefaultButtonSize; // Размер теперь будет через LayoutElement
            // buttonRT.anchoredPosition = anchoredPosition; // Управляется VLG

            var layoutElement = buttonGO.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = styleSO.DefaultButtonSize.y;
            // layoutElement.preferredWidth = styleSO.DefaultButtonSize.x; // Ширина будет управляться VLG (childForceExpandWidth)
            // Если нужно, чтобы кнопка не растягивалась по ширине, а имела DefaultButtonSize.x,
            // то в VLG childForceExpandWidth=false, а здесь layoutElement.preferredWidth = styleSO.DefaultButtonSize.x
        }
        
        /// <summary>
        /// Добавляет полупрозрачное изображение к GameObject, чтобы визуально обозначить место для поля ввода.
        /// </summary>
        private static void AddPlaceholderImage(GameObject go, AuthPanelStyleSO styleSO)
        {
            var img = go.AddComponent<Image>();
            img.color = styleSO.DefaultInputFieldStyle.PlaceholderBackgroundColor; // Полупрозрачный цвет фона поля ввода из SO
        }


        /// <summary>
        /// Настраивает общую "деревянную" рамку для панелей.
        /// </summary>
        private static void SetupFrame(GameObject frameGO, AuthPanelStyleSO.PanelStyle panelStyle, AuthPanelStyleSO mainStyleSO)
        {
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero; // Растягиваем на всю родительскую панель
            frameRT.anchorMax = Vector2.one;
            float padding = mainStyleSO.DefaultPadding;
            frameRT.offsetMin = new Vector2(padding, padding); // Отступы внутри панели из SO
            frameRT.offsetMax = new Vector2(-padding, -padding);
            var frameImg = frameGO.AddComponent<Image>();
            
            if (panelStyle != null && panelStyle.FrameSprite != null)
            {
                frameImg.sprite = panelStyle.FrameSprite;
                frameImg.type = panelStyle.FrameImageType;
                frameImg.color = Color.white; // Сбрасываем цвет, если есть спрайт
            }
            else if (panelStyle != null)
            {
                frameImg.color = panelStyle.FrameColor;
            }
            else // Фоллбэк на какой-то дефолтный цвет, если стиль панели не передан
            {
                frameImg.color = new Color(0.75f, 0.64f, 0.48f, 1f); 
            }
            // frameImg.color = new Color(0.75f, 0.64f, 0.48f, 1f); // Темно-деревянный цвет - УДАЛЕНО
        }

        /// <summary>
        /// Настраивает общий заголовок для панелей.
        /// </summary>
        private static void SetupTitle(GameObject titleGO, AuthPanelStyleSO styleSO)
        {
            var titleRT = titleGO.GetComponent<RectTransform>();
            // Настройки якорей и pivot для VLG не так критичны, но для standalone можно оставить
            titleRT.anchorMin = new Vector2(0.5f, 1f); 
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f); 
            // titleRT.sizeDelta = new Vector2(600, 80); // Ширина будет управляться VLG, высота - предпочтительная от текста
            // titleRT.anchoredPosition = new Vector2(0, yPos); // Управляется VLG
            var titleTextComp = titleGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(titleTextComp, styleSO.TitleTextStyle, styleSO);

            var layoutElement = titleGO.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = titleGO.AddComponent<LayoutElement>();
            // Предпочтительную высоту для заголовка можно не задавать, она возьмется из текста
            // layoutElement.preferredHeight = styleSO.TitleTextStyle.FontSize + N; // где N - некий padding
        }
        
        /// <summary>
        /// Настраивает общую метку (Label) для полей ввода.
        /// </summary>
        private static void SetupLabel(GameObject labelGO, AuthPanelStyleSO styleSO)
        {
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 1f);
            labelRT.anchorMax = new Vector2(0.5f, 1f);
            labelRT.pivot = new Vector2(0.5f, 1f);
            // labelRT.sizeDelta = new Vector2(480, 40); // Аналогично заголовку
            // labelRT.anchoredPosition = new Vector2(0, yPos); // Управляется VLG
            var labelTextComp = labelGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(labelTextComp, styleSO.LabelTextStyle, styleSO);

            var layoutElement = labelGO.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = labelGO.AddComponent<LayoutElement>();
            // Предпочтительную высоту для метки можно не задавать
        }

        /// <summary>
        /// Настраивает RectTransform для общего поля ввода.
        /// Высота будет задаваться через LayoutElement.
        /// </summary>
        private static void SetupInputFieldRT(GameObject inputGO, AuthPanelStyleSO styleSO)
        {
            var inputRT = inputGO.GetComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0.5f, 1f); 
            inputRT.anchorMax = new Vector2(0.5f, 1f);
            inputRT.pivot = new Vector2(0.5f, 1f); 
            // inputRT.sizeDelta = styleSO.DefaultInputSize; // Размер теперь через LayoutElement
            // inputRT.anchoredPosition = new Vector2(0, yPos); // Управляется VLG

            var layoutElement = inputGO.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = inputGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = styleSO.DefaultInputSize.y;
            // layoutElement.preferredWidth = styleSO.DefaultInputSize.x; // Ширина будет управляться VLG
        }
        
        /// <summary>
        /// Создает кнопку "глаз" для переключения видимости пароля.
        /// </summary>
        private static void CreatePasswordToggle(Transform parentContainerTransform, TMP_InputField passwordField, AuthPanelStyleSO styleSO, string buttonName = "TogglePasswordButton")
        {
            var togglePasswordButton = CreateUIObject(buttonName, parentContainerTransform); // Теперь добавляется к HLG
            var togglePasswordButtonRT = togglePasswordButton.GetComponent<RectTransform>();
            // Настройки RT для элемента HLG (можно не задавать явно, HLG управляет)
            // togglePasswordButtonRT.anchorMin = new Vector2(1, 0.5f); 
            // togglePasswordButtonRT.anchorMax = new Vector2(1, 0.5f);
            // togglePasswordButtonRT.pivot = new Vector2(1, 0.5f); 
            // togglePasswordButtonRT.sizeDelta = new Vector2(50, 50); // Размер кнопки "глаз"
            // togglePasswordButtonRT.anchoredPosition = new Vector2(-5, 0); 

            var toggleButtonLE = togglePasswordButton.AddComponent<LayoutElement>();
            toggleButtonLE.preferredWidth = 50; // Фиксированная ширина для кнопки "глаз"
            toggleButtonLE.preferredHeight = 50; // Фиксированная высота (или можно взять от InputField)

            var togglePasswordButtonComp = togglePasswordButton.AddComponent<Button>();
            var togglePasswordBtnImage = togglePasswordButton.AddComponent<Image>();
            // Можно сделать кнопку прозрачной и полагаться на иконку, или использовать очень легкий фон
            togglePasswordBtnImage.color = new Color(1f, 1f, 1f, 0.0f); // Полностью прозрачный фон кнопки

            var eyeIcon = CreateUIObject("EyeIcon", togglePasswordButton.transform); // Иконка внутри кнопки
            var eyeIconRT = eyeIcon.GetComponent<RectTransform>();
            eyeIconRT.anchorMin = Vector2.zero; // Растягиваем иконку на всю кнопку
            eyeIconRT.anchorMax = Vector2.one;
            eyeIconRT.offsetMin = new Vector2(10, 10); // Отступы для иконки внутри кнопки
            eyeIconRT.offsetMax = new Vector2(-10, -10);
            var eyeIconImage = eyeIcon.AddComponent<Image>();
            // Устанавливаем иконку "закрытого глаза" по умолчанию
            if (styleSO.EyeIconClosed != null)
            {
                eyeIconImage.sprite = styleSO.EyeIconClosed;
                eyeIconImage.color = Color.white; // Если есть спрайт, цвет не нужен
            }
            else
            {
                eyeIconImage.color = new Color(0.3f, 0.2f, 0.15f, 0.7f); // Цвет иконки по умолчанию (темный, "закрытый глаз")
            }

            togglePasswordButtonComp.onClick.AddListener(() => 
            {
                bool isPasswordCurrentlyHidden = passwordField.contentType == TMP_InputField.ContentType.Password;
                passwordField.contentType = isPasswordCurrentlyHidden ? 
                    TMP_InputField.ContentType.Standard : 
                    TMP_InputField.ContentType.Password;
                passwordField.ForceLabelUpdate(); // Важно для обновления отображения поля ввода
                
                // Меняем иконку (спрайт или цвет) в зависимости от состояния
                if (isPasswordCurrentlyHidden) // Пароль стал видимым, нужна иконка "открытого глаза"
                {
                    if (styleSO.EyeIconOpened != null)
                    {
                        eyeIconImage.sprite = styleSO.EyeIconOpened;
                        eyeIconImage.color = Color.white;
                    }
                    else
                    {
                        eyeIconImage.sprite = null; // Убираем спрайт, если для открытого глаза нет, но для закрытого был
                        eyeIconImage.color = new Color(0.1f, 0.6f, 0.2f, 0.9f); // Цвет "открытого глаза"
                    }
                }
                else // Пароль стал скрытым, нужна иконка "закрытого глаза"
                {
                    if (styleSO.EyeIconClosed != null)
                    {
                        eyeIconImage.sprite = styleSO.EyeIconClosed;
                        eyeIconImage.color = Color.white;
                    }
                    else
                    {
                        eyeIconImage.sprite = null;
                        eyeIconImage.color = new Color(0.3f, 0.2f, 0.15f, 0.7f); // Цвет "закрытого глаза"
                    }
                }
            });
        }

        /// <summary>
        /// Применяет текстовый стиль (шрифт, размер, цвет, стиль, выравнивание) к компоненту TextMeshProUGUI.
        /// </summary>
        private static void ApplyTextStyle(TextMeshProUGUI textComp, AuthPanelStyleSO.TextStyle textStyleToApply, AuthPanelStyleSO mainStyleSO)
        {
            if (textComp == null || textStyleToApply == null || mainStyleSO == null) 
            {
                if (textComp != null && mainStyleSO != null) // Если textStyleToApply == null, применяем хотя бы базовые стили
                {
                    if (mainStyleSO.DefaultFont != null) textComp.font = mainStyleSO.DefaultFont;
                    textComp.color = mainStyleSO.DefaultTextColor;
                }
                return;
            }

            // Шрифт
            if (textStyleToApply.Font != null)
            {
                textComp.font = textStyleToApply.Font;
            }
            else if (mainStyleSO.DefaultFont != null) // Фоллбэк на DefaultFont из SO
            {
                textComp.font = mainStyleSO.DefaultFont;
            }

            // Размер шрифта
            if (textStyleToApply.FontSize > 0) 
            {
                textComp.fontSize = textStyleToApply.FontSize;
            }

            // Цвет текста
            // Если в textStyleToApply цвет не задан (прозрачный), используем DefaultTextColor из mainStyleSO
            if (textStyleToApply.TextColor.a > 0.001f) // Сравниваем с небольшим значением, т.к. альфа может быть не точно 0
            {
                textComp.color = textStyleToApply.TextColor;
            }
            else
            {
                textComp.color = mainStyleSO.DefaultTextColor;
            }

            // Стиль шрифта (Bold, Italic, etc.)
            textComp.fontStyle = textStyleToApply.FontStyle;

            // Выравнивание
            textComp.alignment = textStyleToApply.Alignment;
        }
        #endregion

        #region Основные методы создания UI элементов (Core UI Element Creation)

        /// <summary>
        /// Пытается установить ссылку на объект в SerializedObject. Выводит предупреждение, если свойство не найдено.
        /// </summary>
        private static void TrySetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"[AuthPanelGenerator] Свойство '{propertyName}' не найдено в AuthUIController. " +
                                 "Убедитесь, что оно определено с атрибутом [SerializeField] и имя совпадает (включая ведущее подчеркивание для приватных полей).");
            }
        }

        /// <summary>
        /// Создает базовый GameObject с RectTransform и устанавливает родителя.
        /// </summary>
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false); // false, чтобы не менять локальные координаты при установке родителя
            return uiObject;
        }

        /// <summary>
        /// Создает панель (GameObject с RectTransform и Image) заданного размера, с якорем и точкой вращения в центре.
        /// Теперь панель будет также иметь ContentSizeFitter для подгонки высоты, если size.y <= 0.
        /// </summary>
        private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var panel = CreateUIObject(name, parent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); // Якорь и Pivot в центре
            panel.AddComponent<Image>(); // Добавляем компонент Image по умолчанию

            if (size.y > 0) // Если высота задана, используем ее
            {
                rt.sizeDelta = size;
            }
            else // Иначе, позволяем ContentSizeFitter управлять высотой (ширину пока оставляем из size.x или она будет от родителя)
            {
                rt.sizeDelta = new Vector2(size.x, 100); // Начальная небольшая высота, CSF ее изменит
                var csf = panel.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                // csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // Если и ширину нужно подгонять
            }
            // Цвет Image будет установлен вызывающим кодом
            return panel;
        }

        /// <summary>
        /// Создает текстовый объект с TextMeshProUGUI.
        /// </summary>
        private static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize, AuthPanelStyleSO styleSO)
        {
            var textGO = CreateUIObject(name, parent);
            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            
            // Применяем шрифт и цвет из styleSO
            if (styleSO.DefaultFont != null)
            {
                textComp.font = styleSO.DefaultFont;
            }
            textComp.color = styleSO.DefaultTextColor; // Используем цвет по умолчанию из SO

            // TMP_FontAsset globalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(UI_RESOURCES_PATH, "MainTextFont_TMP.asset"));
            // if (globalFont) textComp.font = globalFont;
            // textComp.color = new Color(0.2f, 0.15f, 0.1f, 1f); // Темно-коричневый цвет текста по умолчанию - УДАЛЕНО
            textComp.alignment = TextAlignmentOptions.Left; // Выравнивание по умолчанию, может быть переопределено
            return textGO;
        }

        /// <summary>
        /// Создает поле ввода TMP_InputField со стилизованным фоном, плейсхолдером и текстовым компонентом.
        /// </summary>
        private static GameObject CreateInputField(string name, Transform parent, string placeholder, AuthPanelStyleSO styleSO)
        {
            var inputGO = CreateUIObject(name, parent); // Родительский GameObject для поля ввода
            var inputField = inputGO.AddComponent<TMP_InputField>();
            var img = inputGO.AddComponent<Image>(); // Фон поля ввода
            
            // Применяем стиль фона из SO
            if (styleSO.DefaultInputFieldStyle.BackgroundSprite != null)
            {
                img.sprite = styleSO.DefaultInputFieldStyle.BackgroundSprite;
                img.type = styleSO.DefaultInputFieldStyle.BackgroundImageType;
            }
            else
            {
                img.color = styleSO.DefaultInputFieldStyle.BackgroundColor;
            }
            
            inputField.targetGraphic = img; // Фон будет реагировать на состояния поля ввода (выделение и т.д.)

            // Текст-плейсхолдер
            var placeholderGO = CreateTextObject("Placeholder", inputGO.transform, placeholder, 24, styleSO); // Размер шрифта плейсхолдера
            var placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero; // Растягиваем на все поле ввода
            placeholderRT.anchorMax = Vector2.one;
            // Используем DefaultPadding для горизонтальных отступов, и половину ItemSpacing для вертикальных (или можно ввести новый параметр)
            float horizontalPadding = styleSO.DefaultPadding;
            float verticalPadding = styleSO.ItemSpacing / 2f; 
            placeholderRT.offsetMin = new Vector2(horizontalPadding, verticalPadding); 
            placeholderRT.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
            var placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(placeholderText, styleSO.InputPlaceholderTextStyle, styleSO);
            inputField.placeholder = placeholderText;

            // Компонент для отображения вводимого текста
            var textGO = CreateTextObject("Text", inputGO.transform, string.Empty, 26, styleSO); // Размер шрифта для вводимого текста
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; // Также растягиваем
            textRT.anchorMax = Vector2.one;
            // Те же отступы, что и у плейсхолдера
            textRT.offsetMin = new Vector2(horizontalPadding, verticalPadding); 
            textRT.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
            var inputValueText = textGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(inputValueText, styleSO.InputValueTextStyle, styleSO);
            inputField.textComponent = inputValueText;
            
            // Настройка цветов для различных состояний поля ввода
            inputField.colors = styleSO.DefaultInputFieldStyle.InputStateColors;

            return inputGO;
        }

        /// <summary>
        /// Создает UI элемент Toggle (чекбокс) с фоном, галочкой и текстовой меткой.
        /// </summary>
        private static GameObject CreateToggle(string name, Transform parent, string label, AuthPanelStyleSO styleSO)
        {
            // Родительский GameObject для Toggle, его RectTransform будет определять область клика для текста тоже
            var toggleGO = CreateUIObject(name, parent); 
            var toggle = toggleGO.AddComponent<Toggle>();

            // Фон для самого чекбокса (квадратик)
            var background = CreateUIObject("Background", toggleGO.transform); 
            var bgRT = background.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.5f); // Якорь слева по центру родительского toggleGO
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.pivot = new Vector2(0, 0.5f); // Точка вращения там же
            bgRT.sizeDelta = new Vector2(30, 30); // Размер квадратика чекбокса
            bgRT.anchoredPosition = Vector2.zero; // В левой части toggleGO
            var bgImage = background.AddComponent<Image>();
            // bgImage.color = new Color(1f, 0.98f, 0.95f, 1f); // - УДАЛЕНО
            toggle.targetGraphic = bgImage; // Этот Image будет менять цвет при взаимодействии

            // Применяем стиль фона для Box из SO
            if (styleSO.DefaultToggleStyle.BoxBackgroundSprite != null)
            {
                bgImage.sprite = styleSO.DefaultToggleStyle.BoxBackgroundSprite;
                // bgImage.type можно будет добавить в ToggleStyle, если понадобятся sliced спрайты для фона
                bgImage.color = Color.white; // Сбрасываем цвет, если есть спрайт
            }
            else
            {
                bgImage.color = styleSO.DefaultToggleStyle.BoxBackgroundColor;
            }

            // Галочка внутри чекбокса
            var checkmark = CreateUIObject("Checkmark", background.transform); // Дочерний элемент фона квадратика
            var checkmarkRT = checkmark.GetComponent<RectTransform>();
            checkmarkRT.anchorMin = Vector2.zero; // Растягиваем галочку на весь фон квадратика
            checkmarkRT.anchorMax = Vector2.one;
            checkmarkRT.offsetMin = new Vector2(5, 5); // Отступы, чтобы галочка была чуть меньше фона
            checkmarkRT.offsetMax = new Vector2(-5, -5);
            var checkmarkImage = checkmark.AddComponent<Image>();
            // checkmarkImage.color = new Color(0.3f, 0.2f, 0.15f, 1f); // - УДАЛЕНО
            toggle.graphic = checkmarkImage; // Этот Image (галочка) будет появляться/исчезать

            // Применяем стиль галочки из SO
            if (styleSO.DefaultToggleStyle.CheckmarkSprite != null)
            {
                checkmarkImage.sprite = styleSO.DefaultToggleStyle.CheckmarkSprite;
                checkmarkImage.color = Color.white; // Сбрасываем цвет, если есть спрайт
            }
            else if (styleSO.CheckmarkIcon != null) // Фоллбэк на глобальную иконку галочки из SO
            {
                 checkmarkImage.sprite = styleSO.CheckmarkIcon;
                 checkmarkImage.color = Color.white;
            }
            else
            {
                checkmarkImage.color = styleSO.DefaultToggleStyle.CheckmarkColor;
                // Если нет ни спрайта в стиле, ни глобального спрайта, можно скрыть Image или оставить цвет
                // checkmarkImage.enabled = false; // Например, если без спрайта галочка не нужна
            }

            // Текстовая метка для Toggle
            var labelGO = CreateTextObject("Label", toggleGO.transform, label, 24, styleSO); // Размер шрифта метки
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.5f); // Якорь слева по центру родительского toggleGO
            labelRT.anchorMax = new Vector2(1, 0.5f); // Растягиваем по ширине до правого края (если нужно)
            labelRT.pivot = new Vector2(0, 0.5f); // Точка вращения слева по центру
            // Размер по высоте как у чекбокса, ширина подстраивается или задается.
            // Отступ слева, чтобы текст не налезал на чекбокс.
            labelRT.offsetMin = new Vector2(bgRT.sizeDelta.x + styleSO.ItemSpacing, -labelRT.sizeDelta.y / 2); // (ширина_чекбокса + ItemSpacing, смещение_Y_для_центрирования)
            labelRT.offsetMax = new Vector2(0, labelRT.sizeDelta.y / 2); // Правый край упирается в родителя
            var toggleLabelText = labelGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(toggleLabelText, styleSO.LabelTextStyle, styleSO);

            return toggleGO;
        }

        /// <summary>
        /// Создает стилизованную кнопку с текстом.
        /// </summary>
        private static GameObject CreateButton(string name, Transform parent, string text, AuthPanelStyleSO styleSO)
        {
            var buttonGO = CreateUIObject(name, parent);
            var button = buttonGO.AddComponent<Button>();
            var image = buttonGO.AddComponent<Image>(); // Фон кнопки

            // Применяем стиль фона из SO
            // Пока используем DefaultButtonStyle, но в будущем можно будет передавать конкретный стиль (например, DestructiveButtonStyle)
            AuthPanelStyleSO.ButtonStyle activeButtonStyle = styleSO.DefaultButtonStyle; 

            if (activeButtonStyle.BackgroundSprite != null)
            {
                image.sprite = activeButtonStyle.BackgroundSprite;
                image.type = activeButtonStyle.BackgroundImageType;
                image.color = Color.white; // Сбрасываем цвет, если есть спрайт, чтобы он не перекрывался
            }
            else
            {
                image.color = activeButtonStyle.BackgroundColor;
            }
            // image.color = new Color(0.75f, 0.64f, 0.48f, 1f); // - УДАЛЕНО
            button.targetGraphic = image; // Этот Image будет менять цвет при взаимодействии

            // Текст на кнопке
            var textGO = CreateTextObject("Text", buttonGO.transform, text, 28, styleSO); // Размер шрифта на кнопке
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; // Растягиваем текст на всю кнопку
            textRT.anchorMax = Vector2.one;
            // Используем DefaultPadding для горизонтальных отступов, и половину ItemSpacing для вертикальных
            float btnHorizontalPadding = styleSO.DefaultPadding;
            float btnVerticalPadding = styleSO.ItemSpacing / 2f;
            textRT.offsetMin = new Vector2(btnHorizontalPadding, btnVerticalPadding); 
            textRT.offsetMax = new Vector2(-btnHorizontalPadding, -btnVerticalPadding);
            var buttonTextComp = textGO.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(buttonTextComp, styleSO.ButtonTextStyle, styleSO);

            // Настройка цветов для различных состояний кнопки
            button.colors = activeButtonStyle.ButtonStateColors;
            // var colors = button.colors; // - УДАЛЕНО
            // colors.normalColor = new Color(1f,1f,1f,1f); ... // - УДАЛЕНО
            // button.colors = colors; // - УДАЛЕНО
            
            // Можно добавить эффект ButtonPressEffect, если он есть и настроен
            // buttonGO.AddComponent<ButtonPressEffect>();

            return buttonGO;
        }

        #endregion
    }
} 