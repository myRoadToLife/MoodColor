using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Сервис для отправки email-уведомлений пользователям
    /// </summary>
    public class EmailNotificationService : MonoBehaviour, INotificationService
    {
        #region Inspector Fields
        
        [Header("Email Settings")]
        [SerializeField] private string _smtpServer = "smtp.gmail.com";
        [SerializeField] private int _smtpPort = 587;
        [SerializeField] private string _senderEmail = "noreply@moodcolor.com";
        [SerializeField] private string _senderDisplayName = "MoodColor";
        
        [Header("Authentication")]
        [SerializeField] private string _username;
        [SerializeField] private string _password;
        
        [Header("Templates")]
        [SerializeField] private TextAsset _defaultTemplate;
        [SerializeField] private TextAsset _reminderTemplate;
        [SerializeField] private TextAsset _achievementTemplate;
        [SerializeField] private TextAsset _welcomeTemplate;
        
        #endregion
        
        #region Private Fields
        
        private SmtpClient _smtpClient;
        private Dictionary<NotificationCategory, string> _templateCache = new Dictionary<NotificationCategory, string>();
        private bool _isInitialized = false;
        private Queue<EmailTask> _emailQueue = new Queue<EmailTask>();
        private bool _isSending = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // В новой архитектуре жизненный цикл сервиса управляется через DI контейнер
            // Убираем зависимость от старого Singleton NotificationManager
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            if (_smtpClient != null)
            {
                _smtpClient.Dispose();
            }
        }
        
        #endregion
        
        #region INotificationService Implementation
        
        /// <summary>
        /// Имя сервиса для идентификации
        /// </summary>
        public string ServiceName => "EmailNotifications";
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Инициализация сервиса с параметрами
        /// </summary>
        public Task Initialize()
        {
            if (_isInitialized) return Task.CompletedTask;
            
            // Загружаем шаблоны
            CacheTemplates();
            
            // Читаем настройки из конфига, если необходимо
            ReadConfigSettings();
            
            // Создаем SMTP клиент
            CreateSmtpClient();
            
            _isInitialized = true;
            MyLogger.Log("EmailNotificationService initialized successfully", MyLogger.LogCategory.Default);
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Отправляет email-уведомление пользователю
        /// </summary>
        public async void SendEmailNotification(NotificationData notification, string recipientEmail)
        {
            if (!_isInitialized)
            {
                await Initialize();
            }
            
            if (string.IsNullOrEmpty(recipientEmail))
            {
                MyLogger.LogError("Cannot send email notification: recipient email is empty", MyLogger.LogCategory.Default);
                return;
            }
            
            // Добавляем задачу в очередь
            EmailTask task = new EmailTask
            {
                Notification = notification,
                RecipientEmail = recipientEmail
            };
            
            _emailQueue.Enqueue(task);
            
            // Запускаем обработку очереди, если еще не запущена
            if (!_isSending)
            {
                StartCoroutine(ProcessEmailQueue());
            }
        }
        
        /// <summary>
        /// Устанавливает настройки SMTP-сервера программно
        /// </summary>
        public void SetSmtpSettings(string server, int port, string username, string password)
        {
            _smtpServer = server;
            _smtpPort = port;
            _username = username;
            _password = password;
            
            // Пересоздаем SMTP клиент с новыми настройками
            if (_smtpClient != null)
            {
                _smtpClient.Dispose();
            }
            
            CreateSmtpClient();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Кэширует шаблоны писем для быстрого доступа
        /// </summary>
        private void CacheTemplates()
        {
            if (_defaultTemplate != null)
            {
                string defaultTemplateContent = _defaultTemplate.text;
                _templateCache[NotificationCategory.System] = defaultTemplateContent;
                _templateCache[NotificationCategory.Update] = defaultTemplateContent;
                _templateCache[NotificationCategory.Promotion] = defaultTemplateContent;
            }
            
            if (_reminderTemplate != null)
            {
                _templateCache[NotificationCategory.Reminder] = _reminderTemplate.text;
            }
            
            if (_achievementTemplate != null)
            {
                _templateCache[NotificationCategory.Achievement] = _achievementTemplate.text;
            }
            
            if (_welcomeTemplate != null)
            {
                _templateCache[NotificationCategory.Activity] = _welcomeTemplate.text;
            }
        }
        
        /// <summary>
        /// Читает настройки из конфига
        /// </summary>
        private void ReadConfigSettings()
        {
            // Если используется Firebase Remote Config или другая система конфигурации,
            // здесь можно загрузить настройки оттуда
            
            // Пример:
            // _smtpServer = FirebaseRemoteConfig.GetValue("email_smtp_server").StringValue;
            // _smtpPort = (int)FirebaseRemoteConfig.GetValue("email_smtp_port").LongValue;
        }
        
        /// <summary>
        /// Создает и настраивает SMTP клиент
        /// </summary>
        private void CreateSmtpClient()
        {
            try
            {
                _smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_username, _password)
                };
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Failed to create SMTP client: {ex.Message}", MyLogger.LogCategory.Default);
            }
        }
        
        /// <summary>
        /// Обрабатывает очередь email-задач
        /// </summary>
        private IEnumerator ProcessEmailQueue()
        {
            _isSending = true;
            
            while (_emailQueue.Count > 0)
            {
                EmailTask task = _emailQueue.Dequeue();
                
                // Запускаем асинхронную отправку, чтобы не блокировать основной поток
                SendEmailAsync(task);
                
                // Добавляем задержку между отправками писем
                yield return new WaitForSeconds(0.5f);
            }
            
            _isSending = false;
        }
        
        /// <summary>
        /// Асинхронно отправляет email
        /// </summary>
        private async void SendEmailAsync(EmailTask task)
        {
            try
            {
                // Готовим сообщение
                MailMessage message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderDisplayName),
                    Subject = task.Notification.Title,
                    IsBodyHtml = true
                };
                
                message.To.Add(task.RecipientEmail);
                
                // Формируем тело письма на основе шаблона
                string template;
                if (!_templateCache.TryGetValue(task.Notification.Category, out template))
                {
                    template = _templateCache[NotificationCategory.System];
                }
                
                string body = ApplyTemplate(template, task.Notification);
                message.Body = body;
                
                // Отправляем асинхронно
                await Task.Run(() => {
                    try
                    {
                        _smtpClient.Send(message);
                        MyLogger.Log($"Email notification sent to {task.RecipientEmail}", MyLogger.LogCategory.Default);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"Failed to send email: {ex.Message}", MyLogger.LogCategory.Default);
                    }
                });
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Error preparing email: {ex.Message}", MyLogger.LogCategory.Default);
            }
        }
        
        /// <summary>
        /// Применяет шаблон к уведомлению
        /// </summary>
        private string ApplyTemplate(string template, NotificationData notification)
        {
            // Заменяем плейсхолдеры на реальные данные
            string result = template
                .Replace("{TITLE}", notification.Title)
                .Replace("{MESSAGE}", notification.Message)
                .Replace("{TIMESTAMP}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                .Replace("{CATEGORY}", notification.Category.ToString());
            
            // Если есть дополнительные данные в JSON формате, можно парсить их и применять
            if (!string.IsNullOrEmpty(notification.ExtraData))
            {
                try
                {
                    // Пример с простой заменой ключей в формате {KEY}
                    Dictionary<string, string> extraData = JsonUtility.FromJson<Dictionary<string, string>>(notification.ExtraData);
                    foreach (var kvp in extraData)
                    {
                        result = result.Replace("{" + kvp.Key + "}", kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogWarning($"Failed to parse extra data: {ex.Message}", MyLogger.LogCategory.Default);
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// Задача отправки email
        /// </summary>
        private class EmailTask
        {
            public NotificationData Notification { get; set; }
            public string RecipientEmail { get; set; }
        }
        
        #endregion
    }
} 