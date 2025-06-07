using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Common;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.GameSystem;
using App.Develop.Configs.Common.Emotion;
using App.Develop.Utils.Logging;
using App.Develop.DI;

namespace App.Develop.CommonServices.Emotion
{
    /// <summary>
    /// Улучшенная версия EmotionService с интеграцией новых Firebase компонентов
    /// Демонстрирует использование PerformanceMonitor, BatchOperations, OfflineManager
    /// </summary>
    public class EnhancedEmotionService : IEmotionService, IInitializable, IDisposable
    {
        #region Fields

        private readonly Dictionary<EmotionTypes, EmotionData> _emotions;
        private readonly PlayerDataProvider _playerDataProvider;

        // Новые Firebase компоненты
        private readonly IFirebasePerformanceMonitor _performanceMonitor;
        private readonly IFirebaseBatchOperations _batchOperations;
        private readonly IOfflineManager _offlineManager;
        private readonly IDatabaseService _databaseService;

        // Флаги состояния
        private bool _isInitialized;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения эмоции
        /// </summary>
        public event EventHandler<EmotionEvent> OnEmotionEvent;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор для улучшенного EmotionService
        /// </summary>
        public EnhancedEmotionService(
            PlayerDataProvider playerDataProvider,
            IFirebasePerformanceMonitor performanceMonitor,
            IFirebaseBatchOperations batchOperations,
            IOfflineManager offlineManager,
            IDatabaseService databaseService)
        {
            _playerDataProvider = playerDataProvider ?? throw new ArgumentNullException(nameof(playerDataProvider));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _batchOperations = batchOperations ?? throw new ArgumentNullException(nameof(batchOperations));
            _offlineManager = offlineManager ?? throw new ArgumentNullException(nameof(offlineManager));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            _emotions = new Dictionary<EmotionTypes, EmotionData>();

            MyLogger.Log("✅ [EnhancedEmotionService] Инициализирован с новыми Firebase компонентами", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализирует сервис
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            InitializeEmotions();
            _isInitialized = true;

            MyLogger.Log("✅ [EnhancedEmotionService] Инициализация завершена", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Устанавливает значение эмоции с использованием Performance Monitor
        /// </summary>
        public async void SetEmotionValue(EmotionTypes type, float value, bool needSave = true)
        {
            await _performanceMonitor.TrackOperationAsync($"SetEmotionValue({type})", async () =>
            {
                if (!_emotions.ContainsKey(type))
                {
                    MyLogger.LogWarning($"❌ [EnhancedEmotionService] Эмоция {type} не найдена", MyLogger.LogCategory.Firebase);
                    return false;
                }

                var emotion = _emotions[type];
                var oldValue = emotion.Value;
                emotion.Value = Mathf.Clamp01(value);

                MyLogger.Log($"🎭 [EnhancedEmotionService] Эмоция {type}: {oldValue:F2} → {emotion.Value:F2}", MyLogger.LogCategory.Firebase);

                // Уведомляем о событии
                OnEmotionEvent?.Invoke(this, new EmotionEvent(type, EmotionEventType.ValueChanged, oldValue, emotion.Value));

                if (needSave)
                {
                    await SaveEmotionToFirebase(emotion);
                }

                return true;
            });
        }

        /// <summary>
        /// Обновляет множественные эмоции с использованием Batch Operations
        /// </summary>
        public async Task<bool> UpdateMultipleEmotionsAsync(Dictionary<EmotionTypes, float> emotionUpdates)
        {
            return await _performanceMonitor.TrackOperationAsync("UpdateMultipleEmotions", async () =>
            {
                var updates = new Dictionary<string, object>();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    MyLogger.LogError("❌ [EnhancedEmotionService] Пользователь не аутентифицирован", MyLogger.LogCategory.Firebase);
                    return false;
                }

                foreach (var update in emotionUpdates)
                {
                    if (!_emotions.ContainsKey(update.Key))
                        continue;

                    var emotion = _emotions[update.Key];
                    var oldValue = emotion.Value;
                    emotion.Value = Mathf.Clamp01(update.Value);

                    // Fan-out структура данных
                    updates[$"users/{userId}/emotions/{update.Key.ToString()}/value"] = emotion.Value;
                    updates[$"users/{userId}/emotions/{update.Key.ToString()}/lastUpdate"] = DateTime.UtcNow.ToString("O");
                    updates[$"emotion-stats/{userId}/lastActivity"] = DateTime.UtcNow.ToString("O");

                    // Уведомляем о событии
                    OnEmotionEvent?.Invoke(this, new EmotionEvent(update.Key, EmotionEventType.ValueChanged, oldValue, emotion.Value));
                }

                // Выполняем batch обновление
                var success = await _batchOperations.UpdateMultipleRecordsAsync(updates);

                if (success)
                {
                    MyLogger.Log($"✅ [EnhancedEmotionService] Batch обновление {emotionUpdates.Count} эмоций выполнено", MyLogger.LogCategory.Firebase);
                }

                return success;
            });
        }

        /// <summary>
        /// Получает статистику производительности эмоциональных операций
        /// </summary>
        public void ShowPerformanceStats()
        {
            var overallStats = _performanceMonitor.GetStats();
            var emotionStats = _performanceMonitor.GetStats("SetEmotionValue");

            MyLogger.Log("📊 === СТАТИСТИКА ПРОИЗВОДИТЕЛЬНОСТИ ЭМОЦИЙ ===", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Общие операции: {overallStats.TotalExecutions} (успешность: {overallStats.SuccessRate:F1}%)", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 SetEmotionValue: {emotionStats.TotalExecutions} выполнений, среднее время: {emotionStats.AverageExecutionTime.TotalMilliseconds:F2}ms", MyLogger.LogCategory.Firebase);
            MyLogger.Log("📊 ===============================================", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region IEmotionService Implementation

        public List<EmotionTypes> AvailableEmotions => _emotions.Keys.ToList();

        public EmotionData GetEmotion(EmotionTypes type)
        {
            return _emotions.TryGetValue(type, out var emotion) ? emotion : null;
        }

        public bool HasEnough(EmotionTypes type, float amount)
        {
            return _emotions.TryGetValue(type, out var emotion) && emotion.Value >= amount;
        }

        public void SpendEmotion(EmotionTypes type, float amount)
        {
            if (_emotions.TryGetValue(type, out var emotion))
            {
                emotion.Value = Mathf.Max(0, emotion.Value - amount);
                MyLogger.Log($"💸 [EnhancedEmotionService] Потрачено {amount} эмоции {type}, осталось: {emotion.Value}", MyLogger.LogCategory.Firebase);
            }
        }

        public void AddEmotion(EmotionTypes type, float amount)
        {
            if (_emotions.TryGetValue(type, out var emotion))
            {
                emotion.Value = Mathf.Min(1f, emotion.Value + amount);
                MyLogger.Log($"💰 [EnhancedEmotionService] Добавлено {amount} эмоции {type}, всего: {emotion.Value}", MyLogger.LogCategory.Firebase);
            }
        }

        public float GetEmotionValue(EmotionTypes type)
        {
            return _emotions.TryGetValue(type, out var emotion) ? emotion.Value : 0f;
        }

        public float GetEmotionIntensity(EmotionTypes type)
        {
            return _emotions.TryGetValue(type, out var emotion) ? emotion.Intensity : 0f;
        }

        public Color GetEmotionColor(EmotionTypes type)
        {
            return _emotions.TryGetValue(type, out var emotion) ? emotion.Color : Color.white;
        }

        public void LogEmotionEvent(EmotionTypes type, EmotionEventType eventType, string note = null)
        {
            MyLogger.Log($"📝 [EnhancedEmotionService] Событие эмоции: {type} - {eventType}", MyLogger.LogCategory.Firebase);
        }

        public EmotionData GetEmotionData(EmotionTypes type)
        {
            return GetEmotion(type);
        }

        public Dictionary<EmotionTypes, EmotionData> GetAllEmotions()
        {
            return new Dictionary<EmotionTypes, EmotionData>(_emotions);
        }

        public void SetEmotionIntensity(EmotionTypes type, float intensity, bool needSave = true)
        {
            if (_emotions.TryGetValue(type, out var emotion))
            {
                emotion.Intensity = Mathf.Clamp01(intensity);
                MyLogger.Log($"🎭 [EnhancedEmotionService] Интенсивность {type}: {emotion.Intensity:F2}", MyLogger.LogCategory.Firebase);

                if (needSave)
                {
                    _ = SaveEmotionToFirebase(emotion);
                }
            }
        }

        public void ResetAllEmotions(bool needSave = true)
        {
            foreach (var emotion in _emotions.Values)
            {
                emotion.Value = 0.5f;
                emotion.Intensity = 0.5f;
                emotion.LastUpdate = DateTime.UtcNow;
            }

            MyLogger.Log("🔄 [EnhancedEmotionService] Все эмоции сброшены", MyLogger.LogCategory.Firebase);

            if (needSave)
            {
                var updates = new Dictionary<EmotionTypes, float>();
                foreach (var kvp in _emotions)
                {
                    updates[kvp.Key] = kvp.Value.Value;
                }
                _ = UpdateMultipleEmotionsAsync(updates);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализирует базовые эмоции
        /// </summary>
        private void InitializeEmotions()
        {
            var emotionTypes = Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>();

            foreach (var type in emotionTypes)
            {
                _emotions[type] = new EmotionData
                {
                    Type = type.ToString(),
                    Value = 0.5f,
                    Intensity = 0.5f,
                    Color = GetDefaultColor(type),
                    LastUpdate = DateTime.UtcNow
                };
            }

            MyLogger.Log($"✅ [EnhancedEmotionService] Инициализировано {_emotions.Count} эмоций", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Возвращает цвет по умолчанию для эмоции
        /// </summary>
        private Color GetDefaultColor(EmotionTypes type)
        {
            return type switch
            {
                EmotionTypes.Joy => Color.yellow,
                EmotionTypes.Sadness => Color.blue,
                EmotionTypes.Anger => Color.red,
                EmotionTypes.Fear => Color.black,
                EmotionTypes.Love => Color.magenta,
                EmotionTypes.Disgust => Color.green,
                _ => Color.gray
            };
        }

        /// <summary>
        /// Получает ID текущего пользователя
        /// </summary>
        private string GetCurrentUserId()
        {
            // Упрощенная версия - в реальности нужно получить из _databaseService
            return "test_user_123";
        }

        /// <summary>
        /// Сохраняет эмоцию в Firebase с обработкой ошибок
        /// </summary>
        private async Task SaveEmotionToFirebase(EmotionData emotion)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return;

                var emotionKey = emotion.Type.ToString();
                var updates = new Dictionary<string, object>
                {
                    [$"users/{userId}/emotions/{emotionKey}/value"] = emotion.Value,
                    [$"users/{userId}/emotions/{emotionKey}/intensity"] = emotion.Intensity,
                    [$"users/{userId}/emotions/{emotionKey}/lastUpdate"] = DateTime.UtcNow.ToString("O")
                };

                await _batchOperations.UpdateMultipleRecordsAsync(updates);
                MyLogger.Log($"💾 [EnhancedEmotionService] Эмоция {emotion.Type} сохранена в Firebase", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [EnhancedEmotionService] Ошибка сохранения эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            OnEmotionEvent = null;
            _isDisposed = true;
            MyLogger.Log("🗑️ [EnhancedEmotionService] Disposed", MyLogger.LogCategory.Firebase);
        }

        #endregion
    }
}