using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Сервис для отображения внутриигровых уведомлений
    /// </summary>
    public class InGameNotificationService : MonoBehaviour, INotificationService
    {
        #region Inspector Fields
        
        [Header("Prefab References")]
        [SerializeField] private GameObject _notificationPrefab;
        [SerializeField] private Transform _notificationContainer;
        
        [Header("Notification Settings")]
        [SerializeField] private float _defaultDuration = 5f;
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private int _maxVisibleNotifications = 3;
        [SerializeField] private Vector2 _notificationSize = new Vector2(300f, 80f);
        
        #endregion
        
        #region Private Fields
        
        private Queue<NotificationData> _pendingNotifications = new Queue<NotificationData>();
        private List<GameObject> _activeNotifications = new List<GameObject>();
        private bool _isInitialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (NotificationManager.Instance != null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            ClearAllNotifications();
        }
        
        #endregion
        
        #region INotificationService Implementation
        
        /// <summary>
        /// Имя сервиса для идентификации
        /// </summary>
        public string ServiceName => "InGameNotifications";
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Инициализация сервиса с параметрами
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            // Создаем контейнер для уведомлений, если его нет
            if (_notificationContainer == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.Log("Canvas не найден, создаем новый для уведомлений");
                    // Создаем новый Canvas
                    GameObject canvasObject = new GameObject("NotificationsCanvas");
                    canvas = canvasObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObject.AddComponent<CanvasScaler>();
                    canvasObject.AddComponent<GraphicRaycaster>();
                    
                    // Не уничтожаем при переходе между сценами
                    DontDestroyOnLoad(canvasObject);
                }
                
                _notificationContainer = new GameObject("NotificationContainer").transform;
                _notificationContainer.SetParent(canvas.transform, false);
                RectTransform containerRect = _notificationContainer.gameObject.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(1, 1);
                containerRect.anchorMax = new Vector2(1, 1);
                containerRect.pivot = new Vector2(1, 1);
                containerRect.anchoredPosition = new Vector2(-20, -20);
            }
            
            // Загружаем префаб уведомления, если он не указан
            if (_notificationPrefab == null)
            {
                _notificationPrefab = Resources.Load<GameObject>("Prefabs/UI/InGameNotification");
                if (_notificationPrefab == null)
                {
                    Debug.Log("Префаб уведомления не найден, создаем простой префаб");
                    // Создаем простой префаб уведомления программно
                    _notificationPrefab = new GameObject("InGameNotification");
                    RectTransform prefabRect = _notificationPrefab.AddComponent<RectTransform>();
                    prefabRect.sizeDelta = _notificationSize;
                    
                    // Добавляем фон
                    GameObject background = new GameObject("Background");
                    RectTransform bgRect = background.AddComponent<RectTransform>();
                    background.AddComponent<CanvasRenderer>();
                    Image bgImage = background.AddComponent<Image>();
                    bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                    bgRect.SetParent(prefabRect, false);
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;
                    
                    // Добавляем текст заголовка
                    GameObject titleObj = new GameObject("TitleText");
                    RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                    titleObj.AddComponent<CanvasRenderer>();
                    Text titleText = titleObj.AddComponent<Text>();
                    titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    titleText.fontSize = 14;
                    titleText.color = Color.white;
                    titleText.alignment = TextAnchor.MiddleLeft;
                    titleRect.SetParent(prefabRect, false);
                    titleRect.anchorMin = new Vector2(0, 0.6f);
                    titleRect.anchorMax = new Vector2(0.9f, 1);
                    titleRect.offsetMin = new Vector2(10, 0);
                    titleRect.offsetMax = new Vector2(-10, 0);
                    
                    // Добавляем текст сообщения
                    GameObject msgObj = new GameObject("MessageText");
                    RectTransform msgRect = msgObj.AddComponent<RectTransform>();
                    msgObj.AddComponent<CanvasRenderer>();
                    Text msgText = msgObj.AddComponent<Text>();
                    msgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    msgText.fontSize = 12;
                    msgText.color = Color.white;
                    msgText.alignment = TextAnchor.MiddleLeft;
                    msgRect.SetParent(prefabRect, false);
                    msgRect.anchorMin = new Vector2(0, 0);
                    msgRect.anchorMax = new Vector2(0.9f, 0.6f);
                    msgRect.offsetMin = new Vector2(10, 0);
                    msgRect.offsetMax = new Vector2(-10, 0);
                    
                    // Добавляем кнопку закрытия
                    GameObject closeBtn = new GameObject("CloseButton");
                    RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
                    closeBtn.AddComponent<CanvasRenderer>();
                    Image closeBtnImage = closeBtn.AddComponent<Image>();
                    closeBtnImage.color = new Color(0.8f, 0.2f, 0.2f, 1);
                    Button closeButton = closeBtn.AddComponent<Button>();
                    closeButton.targetGraphic = closeBtnImage;
                    closeBtnRect.SetParent(prefabRect, false);
                    closeBtnRect.anchorMin = new Vector2(0.9f, 0.9f);
                    closeBtnRect.anchorMax = new Vector2(1, 1);
                    closeBtnRect.offsetMin = new Vector2(-5, -5);
                    closeBtnRect.offsetMax = new Vector2(-5, -5);
                }
            }
            
            _isInitialized = true;
            Debug.Log("InGameNotificationService initialized successfully");
        }
        
        /// <summary>
        /// Показывает внутриигровое уведомление
        /// </summary>
        public void ShowNotification(NotificationData notification)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            if (_notificationPrefab == null)
            {
                Debug.LogError("Cannot show notification: notification prefab is missing");
                return;
            }
            
            // Если достигнут лимит видимых уведомлений, добавляем в очередь
            if (_activeNotifications.Count >= _maxVisibleNotifications)
            {
                _pendingNotifications.Enqueue(notification);
                return;
            }
            
            // Создаем и настраиваем объект уведомления
            GameObject notificationObject = Instantiate(_notificationPrefab, _notificationContainer);
            RectTransform rectTransform = notificationObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = _notificationSize;
                // Располагаем уведомления снизу вверх
                rectTransform.anchoredPosition = new Vector2(0, -(_notificationSize.y + 10) * _activeNotifications.Count);
            }
            
            // Настраиваем текст и контент
            Text titleText = notificationObject.transform.Find("TitleText")?.GetComponent<Text>();
            Text messageText = notificationObject.transform.Find("MessageText")?.GetComponent<Text>();
            Image categoryIcon = notificationObject.transform.Find("CategoryIcon")?.GetComponent<Image>();
            Button closeButton = notificationObject.transform.Find("CloseButton")?.GetComponent<Button>();
            
            if (titleText != null) titleText.text = notification.Title;
            if (messageText != null) messageText.text = notification.Message;
            
            // Устанавливаем иконку в зависимости от категории
            if (categoryIcon != null)
            {
                Sprite iconSprite = GetIconForCategory(notification.Category);
                if (iconSprite != null)
                {
                    categoryIcon.sprite = iconSprite;
                }
            }
            
            // Настраиваем кнопку закрытия
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => {
                    CloseNotification(notificationObject);
                });
            }
            
            // Анимация появления
            StartCoroutine(AnimateNotification(notificationObject, true));
            
            // Добавляем в список активных
            _activeNotifications.Add(notificationObject);
            
            // Запускаем таймер автоматического скрытия
            float duration = notification.Priority == NotificationPriority.Critical ? 
                _defaultDuration * 2 : _defaultDuration;
                
            StartCoroutine(AutoHideNotification(notificationObject, duration));
        }
        
        /// <summary>
        /// Скрывает все активные уведомления
        /// </summary>
        public void ClearAllNotifications()
        {
            foreach (var notification in _activeNotifications)
            {
                if (notification != null)
                {
                    Destroy(notification);
                }
            }
            
            _activeNotifications.Clear();
            _pendingNotifications.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Получает иконку для категории уведомления
        /// </summary>
        private Sprite GetIconForCategory(NotificationCategory category)
        {
            string iconPath = $"Icons/Notifications/{category.ToString()}";
            return Resources.Load<Sprite>(iconPath);
        }
        
        /// <summary>
        /// Закрывает уведомление и показывает следующее из очереди, если есть
        /// </summary>
        private void CloseNotification(GameObject notification)
        {
            StartCoroutine(AnimateNotification(notification, false));
        }
        
        /// <summary>
        /// Анимирует появление или исчезновение уведомления
        /// </summary>
        private IEnumerator AnimateNotification(GameObject notification, bool isShowing)
        {
            RectTransform rect = notification.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = notification.AddComponent<CanvasGroup>();
            }
            
            float startAlpha = isShowing ? 0f : 1f;
            float targetAlpha = isShowing ? 1f : 0f;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = isShowing ? 
                startPos : 
                new Vector2(startPos.x + 50, startPos.y);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _animationDuration;
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, normalizedTime);
                
                yield return null;
            }
            
            // Если это анимация скрытия, удаляем объект
            if (!isShowing)
            {
                _activeNotifications.Remove(notification);
                Destroy(notification);
                
                // Перестраиваем позиции оставшихся уведомлений
                RearrangeActiveNotifications();
                
                // Проверяем, есть ли уведомления в очереди
                if (_pendingNotifications.Count > 0 && _activeNotifications.Count < _maxVisibleNotifications)
                {
                    NotificationData nextNotification = _pendingNotifications.Dequeue();
                    ShowNotification(nextNotification);
                }
            }
        }
        
        /// <summary>
        /// Перестраивает позиции активных уведомлений
        /// </summary>
        private void RearrangeActiveNotifications()
        {
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                RectTransform rect = _activeNotifications[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 targetPos = new Vector2(0, -(_notificationSize.y + 10) * i);
                    StartCoroutine(AnimateRectPosition(rect, targetPos));
                }
            }
        }
        
        /// <summary>
        /// Анимирует перемещение RectTransform
        /// </summary>
        private IEnumerator AnimateRectPosition(RectTransform rect, Vector2 targetPos)
        {
            Vector2 startPos = rect.anchoredPosition;
            float elapsedTime = 0f;
            
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _animationDuration;
                
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, normalizedTime);
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Автоматически скрывает уведомление через указанное время
        /// </summary>
        private IEnumerator AutoHideNotification(GameObject notification, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (notification != null && _activeNotifications.Contains(notification))
            {
                CloseNotification(notification);
            }
        }
        
        #endregion
    }
} 