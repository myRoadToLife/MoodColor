using System;
using System.Collections.Generic;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Реализация системы уровней
    /// </summary>
    public class LevelSystem : ILevelSystem, IInitializable
    {
        #region Constants

        private const int BaseXpForLevel = 100; // Базовый опыт для первого уровня
        private const float LevelMultiplier = 1.5f; // Множитель для каждого следующего уровня
        private const int MaxLevel = 100; // Максимальный уровень

        #endregion

        #region Events

        /// <summary>
        /// Событие повышения уровня
        /// </summary>
        public event Action<int> OnLevelUp;

        /// <summary>
        /// Событие изменения опыта (текущий опыт, полученный опыт)
        /// </summary>
        public event Action<int, int> OnXPChanged;

        #endregion

        #region Dependencies

        private readonly PlayerDataProvider _playerDataProvider;
        private readonly IPointsService _pointsService;

        #endregion

        #region Properties

        /// <summary>
        /// Текущий уровень пользователя
        /// </summary>
        public int CurrentLevel => _gameData?.Level ?? 1;

        /// <summary>
        /// Текущее количество опыта
        /// </summary>
        public int CurrentXP => _gameData?.XP ?? 0;

        /// <summary>
        /// Требуемое количество опыта для следующего уровня
        /// </summary>
        public int RequiredXPForNextLevel => CalculateRequiredXP(CurrentLevel + 1);

        /// <summary>
        /// Прогресс к следующему уровню (0.0 - 1.0)
        /// </summary>
        public float LevelProgress
        {
            get
            {
                if (CurrentLevel >= MaxLevel) return 1f;
                
                int currentLevelXP = CalculateRequiredXP(CurrentLevel);
                int nextLevelXP = RequiredXPForNextLevel;
                int xpDifference = nextLevelXP - currentLevelXP;
                
                if (xpDifference <= 0) return 1f;
                
                float progress = (float)(CurrentXP - currentLevelXP) / xpDifference;
                return Mathf.Clamp01(progress);
            }
        }

        private GameData _gameData;
        private readonly Dictionary<XPSource, float> _xpMultipliers;

        #endregion

        #region Constructor

        public LevelSystem(
            PlayerDataProvider playerDataProvider,
            IPointsService pointsService)
        {
            _playerDataProvider = playerDataProvider;
            _pointsService = pointsService;
            _xpMultipliers = new Dictionary<XPSource, float>
            {
                { XPSource.EmotionMarked, 1.0f },
                { XPSource.DailyBonus, 2.0f },
                { XPSource.Achievement, 3.0f },
                { XPSource.ConsecutiveUse, 1.5f },
                { XPSource.SpecialAction, 2.5f }
            };
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
            
            Debug.Log($"Инициализирована система уровней. Текущий уровень: {CurrentLevel}, XP: {CurrentXP}/{RequiredXPForNextLevel}");
        }

        #endregion

        #region ILevelSystem Implementation

        /// <summary>
        /// Добавить опыт
        /// </summary>
        /// <param name="amount">Количество опыта</param>
        /// <param name="source">Источник опыта</param>
        public void AddXP(int amount, XPSource source)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка добавить отрицательное или нулевое количество опыта: {amount}");
                return;
            }

            if (CurrentLevel >= MaxLevel)
            {
                Debug.Log("Достигнут максимальный уровень, опыт не начисляется");
                return;
            }

            // Применяем множитель в зависимости от источника
            int finalAmount = ApplyMultiplier(amount, source);
            
            // Запоминаем текущий уровень
            int oldLevel = _gameData.Level;
            
            // Начисляем опыт
            _gameData.XP += finalAmount;
            _gameData.LastXpUpdateDate = DateTime.Now;
            
            // Проверяем, достигнут ли новый уровень
            while (_gameData.XP >= CalculateRequiredXP(_gameData.Level + 1) && _gameData.Level < MaxLevel)
            {
                _gameData.Level++;
                
                // Бонус за новый уровень
                int reward = CalculateLevelUpReward(_gameData.Level);
                _pointsService.AddPoints(reward, PointsSource.LevelUp);
                
                // Вызываем событие повышения уровня
                OnLevelUp?.Invoke(_gameData.Level);
                
                Debug.Log($"Достигнут новый уровень: {_gameData.Level}, награда: {reward} очков");
                
                if (_gameData.Level >= MaxLevel)
                {
                    Debug.Log("Достигнут максимальный уровень!");
                    break;
                }
            }
            
            // Сохраняем изменения
            _playerDataProvider.Save();
            
            // Вызываем событие изменения опыта
            OnXPChanged?.Invoke(_gameData.XP, finalAmount);
            
            Debug.Log($"Добавлено {finalAmount} опыта из источника {source}. Текущий уровень: {CurrentLevel}, XP: {CurrentXP}/{RequiredXPForNextLevel}");
        }

        /// <summary>
        /// Рассчитать необходимое количество опыта для указанного уровня
        /// </summary>
        /// <param name="level">Уровень</param>
        /// <returns>Количество опыта</returns>
        public int CalculateRequiredXP(int level)
        {
            if (level <= 1) return 0;
            if (level > MaxLevel) level = MaxLevel;
            
            return (int)(BaseXpForLevel * Math.Pow(level - 1, LevelMultiplier));
        }

        /// <summary>
        /// Получить множитель опыта для указанного источника
        /// </summary>
        /// <param name="source">Источник опыта</param>
        /// <returns>Множитель</returns>
        public float GetXPMultiplier(XPSource source)
        {
            if (_xpMultipliers.TryGetValue(source, out float multiplier))
            {
                return multiplier;
            }
            
            return 1.0f; // По умолчанию множитель 1
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Применяет множитель к количеству опыта в зависимости от источника
        /// </summary>
        /// <param name="amount">Базовое количество опыта</param>
        /// <param name="source">Источник опыта</param>
        /// <returns>Конечное количество опыта с применённым множителем</returns>
        private int ApplyMultiplier(int amount, XPSource source)
        {
            float multiplier = GetXPMultiplier(source);
            
            // Если у пользователя высокий уровень, добавляем небольшой дополнительный множитель
            if (CurrentLevel > 10)
            {
                float levelBonus = 1.0f + ((CurrentLevel - 10) * 0.01f); // +1% за каждый уровень выше 10
                multiplier *= levelBonus;
            }
            
            return Mathf.RoundToInt(amount * multiplier);
        }

        /// <summary>
        /// Рассчитывает награду за повышение уровня
        /// </summary>
        /// <param name="level">Достигнутый уровень</param>
        /// <returns>Количество очков в награду</returns>
        private int CalculateLevelUpReward(int level)
        {
            // Базовая награда увеличивается с уровнем
            return 50 * level;
        }

        #endregion
    }
} 