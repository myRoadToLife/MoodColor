using System;
using System.Collections.Generic;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Сервис для управления очками пользователя
    /// </summary>
    public class PointsService : IPointsService, IInitializable
    {
        #region Constants

        private const int c_DefaultDailyBonus = 5;
        private const int c_BaseEmotionPoints = 10;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения очков
        /// </summary>
        public event Action<int> OnPointsChanged;

        /// <summary>
        /// Событие получения очков
        /// </summary>
        public event Action<int, PointsSource> OnPointsEarned;

        #endregion

        #region Dependencies

        private readonly PlayerDataProvider _playerDataProvider;

        #endregion

        #region Properties

        /// <summary>
        /// Текущее количество очков
        /// </summary>
        public int CurrentPoints => _playerData?.Points ?? 0;

        private GameData _playerData;

        #endregion

        #region Constructor

        public PointsService(PlayerDataProvider playerDataProvider)
        {
            _playerDataProvider = playerDataProvider;
        }

        #endregion

        #region IInitializable Implementation

        public void Initialize()
        {
            var playerData = _playerDataProvider.GetData();
            
            // Проверяем наличие данных игры, создаем если еще нет
            if (playerData.GameData == null)
            {
                playerData.GameData = new GameData();
                _playerDataProvider.Save();
            }
            
            _playerData = playerData.GameData;
            
            Debug.Log($"Инициализирована система очков. Текущее количество: {CurrentPoints}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавить очки пользователю
        /// </summary>
        /// <param name="amount">Количество очков</param>
        /// <param name="source">Источник очков</param>
        public void AddPoints(int amount, PointsSource source)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка добавить отрицательное число очков: {amount}");
                return;
            }

            // Применяем множитель в зависимости от источника
            int finalAmount = ApplyMultiplier(amount, source);
            
            _playerData.Points += finalAmount;
            _playerData.TotalEarnedPoints += finalAmount;
            
            // Записываем транзакцию
            _playerData.PointsTransactions.Add(new PointsTransaction
            {
                Amount = finalAmount, 
                Source = source,
                Timestamp = DateTime.Now
            });
            
            // Сохраняем изменения
            _playerDataProvider.Save();
            
            // Вызываем события
            OnPointsChanged?.Invoke(CurrentPoints);
            OnPointsEarned?.Invoke(finalAmount, source);
            
            Debug.Log($"Добавлено {finalAmount} очков из источника {source}. Текущее количество: {CurrentPoints}");
        }

        /// <summary>
        /// Использовать очки (списать)
        /// </summary>
        /// <param name="amount">Количество очков</param>
        /// <returns>True, если списание выполнено успешно</returns>
        public bool SpendPoints(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка списать отрицательное число очков: {amount}");
                return false;
            }

            if (CurrentPoints < amount)
            {
                Debug.LogWarning($"Недостаточно очков для списания: {CurrentPoints}/{amount}");
                return false;
            }

            _playerData.Points -= amount;
            
            // Записываем транзакцию
            _playerData.PointsTransactions.Add(new PointsTransaction
            {
                Amount = -amount, 
                Source = PointsSource.Spending,
                Timestamp = DateTime.Now
            });
            
            // Сохраняем изменения
            _playerDataProvider.Save();
            
            // Вызываем событие
            OnPointsChanged?.Invoke(CurrentPoints);
            
            Debug.Log($"Списано {amount} очков. Текущее количество: {CurrentPoints}");
            return true;
        }

        /// <summary>
        /// Получить историю транзакций очков
        /// </summary>
        /// <param name="count">Количество последних транзакций (0 - все)</param>
        /// <returns>Список транзакций</returns>
        public List<PointsTransaction> GetTransactionsHistory(int count = 0)
        {
            if (_playerData.PointsTransactions == null || _playerData.PointsTransactions.Count == 0)
            {
                return new List<PointsTransaction>();
            }

            var transactions = new List<PointsTransaction>(_playerData.PointsTransactions);
            transactions.Reverse(); // От новых к старым

            if (count > 0 && count < transactions.Count)
            {
                return transactions.GetRange(0, count);
            }

            return transactions;
        }

        /// <summary>
        /// Добавить очки за отметку эмоции
        /// </summary>
        public void AddPointsForEmotion()
        {
            AddPoints(c_BaseEmotionPoints, PointsSource.EmotionMarked);
        }

        /// <summary>
        /// Добавить бонусные очки за ежедневное использование
        /// </summary>
        public void AddDailyBonus()
        {
            // Проверяем, получал ли пользователь бонус сегодня
            var today = DateTime.Today;
            if (_playerData.LastDailyBonusDate.Date == today)
            {
                Debug.Log("Ежедневный бонус уже был получен сегодня");
                return;
            }
            
            AddPoints(c_DefaultDailyBonus, PointsSource.DailyBonus);
            _playerData.LastDailyBonusDate = today;
            _playerDataProvider.Save();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Применить множитель к очкам в зависимости от источника
        /// </summary>
        private int ApplyMultiplier(int amount, PointsSource source)
        {
            float multiplier = 1.0f;
            
            switch (source)
            {
                case PointsSource.Achievement:
                    multiplier = 1.5f;
                    break;
                case PointsSource.DailyBonus:
                    multiplier = 1.2f;
                    break;
                case PointsSource.LevelUp:
                    multiplier = 2.0f;
                    break;
            }

            return Mathf.RoundToInt(amount * multiplier);
        }

        #endregion
    }
} 