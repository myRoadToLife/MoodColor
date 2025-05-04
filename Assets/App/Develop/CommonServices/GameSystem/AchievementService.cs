using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.GameSystem.Conditions;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Сервис управления достижениями
    /// </summary>
    public class AchievementService : IAchievementService, IInitializable
    {
        #region Events

        /// <summary>
        /// Событие при выполнении достижения
        /// </summary>
        public event Action<Achievement> OnAchievementCompleted;

        /// <summary>
        /// Событие при обновлении прогресса достижения
        /// </summary>
        public event Action<Achievement, float> OnAchievementProgressUpdated;

        #endregion

        #region Dependencies

        private readonly PlayerDataProvider _playerDataProvider;
        private readonly IPointsService _pointsService;
        private readonly Dictionary<string, IAchievementCondition> _achievementConditions;

        #endregion

        #region Properties

        private GameData _gameData;

        #endregion

        #region Constructor

        public AchievementService(
            PlayerDataProvider playerDataProvider,
            IPointsService pointsService)
        {
            _playerDataProvider = playerDataProvider;
            _pointsService = pointsService;
            _achievementConditions = new Dictionary<string, IAchievementCondition>();
        }

        #endregion

        #region IInitializable Implementation

        public void Initialize()
        {
            var playerData = _playerDataProvider.GetData();
            
            // Проверяем наличие данных игры
            if (playerData.GameData == null)
            {
                playerData.GameData = new GameData();
                _playerDataProvider.Save();
            }
            
            _gameData = playerData.GameData;
            
            // Заполняем словарь для быстрого доступа
            _gameData.AchievementsMap = new Dictionary<string, Achievement>();
            foreach (var achievement in _gameData.Achievements)
            {
                _gameData.AchievementsMap[achievement.Id] = achievement;
            }
            
            // Загружаем и регистрируем условия достижений
            RegisterAchievementConditions();
            
            // Создаем стандартные достижения, если их нет
            if (_gameData.Achievements.Count == 0)
            {
                CreateDefaultAchievements();
                _playerDataProvider.Save();
            }
            
            Debug.Log($"Инициализирована система достижений. Всего достижений: {_gameData.Achievements.Count}");
        }

        #endregion

        #region IAchievementService Implementation

        /// <summary>
        /// Получить все достижения
        /// </summary>
        /// <returns>Список всех достижений</returns>
        public List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>(_gameData.Achievements);
        }

        /// <summary>
        /// Получить завершенные достижения
        /// </summary>
        /// <returns>Список завершенных достижений</returns>
        public List<Achievement> GetCompletedAchievements()
        {
            return _gameData.Achievements.Where(a => a.IsCompleted).ToList();
        }

        /// <summary>
        /// Получить незавершенные достижения
        /// </summary>
        /// <returns>Список незавершенных достижений</returns>
        public List<Achievement> GetInProgressAchievements()
        {
            return _gameData.Achievements.Where(a => !a.IsCompleted).ToList();
        }

        /// <summary>
        /// Получить прогресс выполнения достижения
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <returns>Прогресс от 0.0f до 1.0f</returns>
        public float GetAchievementProgress(string achievementId)
        {
            if (_gameData.AchievementsMap.TryGetValue(achievementId, out var achievement))
            {
                return achievement.Progress;
            }
            
            Debug.LogWarning($"Достижение с ID {achievementId} не найдено");
            return 0f;
        }

        /// <summary>
        /// Обновить прогресс достижения
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <param name="progress">Новый прогресс</param>
        public void UpdateAchievementProgress(string achievementId, float progress)
        {
            if (!_gameData.AchievementsMap.TryGetValue(achievementId, out var achievement))
            {
                Debug.LogWarning($"Достижение с ID {achievementId} не найдено");
                return;
            }
            
            if (achievement.IsCompleted)
            {
                return; // Уже выполнено
            }
            
            // Ограничиваем прогресс между 0 и 1
            progress = Mathf.Clamp01(progress);
            
            // Если прогресс не изменился, выходим
            if (Mathf.Approximately(achievement.Progress, progress))
            {
                return;
            }
            
            // Обновляем прогресс
            achievement.Progress = progress;
            
            // Вызываем событие
            OnAchievementProgressUpdated?.Invoke(achievement, progress);
            
            // Если достижение выполнено
            if (Mathf.Approximately(progress, 1f))
            {
                CompleteAchievement(achievement);
            }
            
            // Сохраняем изменения
            _playerDataProvider.Save();
        }

        /// <summary>
        /// Проверить выполнение всех достижений
        /// </summary>
        public void CheckAllAchievements()
        {
            var playerData = _playerDataProvider.GetData();
            
            foreach (var achievement in _gameData.Achievements.Where(a => !a.IsCompleted))
            {
                if (_achievementConditions.TryGetValue(achievement.Id, out var condition))
                {
                    // Вычисляем прогресс
                    float progress = condition.CalculateProgress(playerData);
                    
                    // Если прогресс изменился, обновляем
                    if (!Mathf.Approximately(achievement.Progress, progress))
                    {
                        UpdateAchievementProgress(achievement.Id, progress);
                    }
                    
                    // Проверяем условие полного выполнения
                    if (!achievement.IsCompleted && condition.CheckCondition(playerData))
                    {
                        UpdateAchievementProgress(achievement.Id, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// Получить достижение по идентификатору
        /// </summary>
        /// <param name="achievementId">Идентификатор достижения</param>
        /// <returns>Достижение или null, если не найдено</returns>
        public Achievement GetAchievement(string achievementId)
        {
            if (_gameData.AchievementsMap.TryGetValue(achievementId, out var achievement))
            {
                return achievement;
            }
            
            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Регистрирует условия достижений
        /// </summary>
        private void RegisterAchievementConditions()
        {
            // Эмоциональные достижения
            _achievementConditions["emotional_spectrum"] = new EmotionSpectrumCondition();
            
            // Ежедневные паттерны
            _achievementConditions["daily_mindfulness"] = new DailyMindfulnessCondition();
            
            // Для каждого условия будет своя реализация
            // В будущем можно добавить и другие условия
            
            Debug.Log($"Зарегистрировано {_achievementConditions.Count} условий достижений");
        }

        /// <summary>
        /// Создает стандартные достижения
        /// </summary>
        private void CreateDefaultAchievements()
        {
            // Эмоциональные достижения
            AddAchievement("emotional_spectrum", "Эмоциональный спектр", 
                "Испытать каждую эмоцию хотя бы раз", AchievementType.Emotional, 50);
            
            AddAchievement("emotional_stability", "Стабильность", 
                "Поддерживать одну эмоцию несколько дней подряд", AchievementType.Emotional, 30);
            
            AddAchievement("emotional_balance", "Эмоциональное равновесие", 
                "Иметь опыт всех эмоций в равных пропорциях", AchievementType.Emotional, 100);
            
            // Ежедневные паттерны
            AddAchievement("daily_mindfulness", "Рутина осознанности", 
                "Отмечать настроение 7 дней подряд", AchievementType.Routine, 25);
            
            AddAchievement("persistence_30", "Настойчивость I", 
                "Отмечать настроение 30 дней подряд", AchievementType.Routine, 50);
            
            AddAchievement("persistence_100", "Настойчивость II", 
                "Отмечать настроение 100 дней подряд", AchievementType.Routine, 100);
            
            // Прогресс и управление эмоциями
            AddAchievement("path_to_happiness", "Путь к счастью", 
                "Перейти от негативных к позитивным эмоциям за неделю", AchievementType.Progress, 40);
            
            AddAchievement("overcoming", "Преодоление", 
                "Выйти из продолжительного периода отрицательных эмоций", AchievementType.Progress, 50);
            
            // Исследовательские достижения
            AddAchievement("color_theorist", "Цветовой теоретик", 
                "Открыть все цветовые ассоциации с эмоциями", AchievementType.Explorer, 35);
            
            AddAchievement("chronologist", "Хронолог", 
                "Отслеживать эмоции в определённые времена дня/недели", AchievementType.Explorer, 30);
        }

        /// <summary>
        /// Добавляет новое достижение
        /// </summary>
        private void AddAchievement(string id, string name, string description, 
            AchievementType type, int pointsReward)
        {
            var achievement = new Achievement
            {
                Id = id,
                Name = name,
                Description = description,
                Type = type,
                PointsReward = pointsReward,
                Progress = 0f,
                IsCompleted = false,
                CompletionDate = null,
                IconPath = $"Achievements/{id}"
            };
            
            _gameData.Achievements.Add(achievement);
            _gameData.AchievementsMap[id] = achievement;
            
            Debug.Log($"Добавлено достижение: {name}");
        }

        /// <summary>
        /// Отмечает достижение как выполненное
        /// </summary>
        private void CompleteAchievement(Achievement achievement)
        {
            if (achievement.IsCompleted)
            {
                return; // Уже выполнено
            }
            
            achievement.IsCompleted = true;
            achievement.Progress = 1f;
            achievement.CompletionDate = DateTime.Now;
            
            // Начисляем награду
            _pointsService.AddPoints(achievement.PointsReward, PointsSource.Achievement);
            
            // Вызываем событие
            OnAchievementCompleted?.Invoke(achievement);
            
            Debug.Log($"Достижение выполнено: {achievement.Name}, награда: {achievement.PointsReward} очков");
        }

        #endregion
    }
} 