using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            Debug.Log("[PointsService] Синхронная часть Initialize вызвана.");
        }

        public async Task InitializeAsync()
        {
            await _playerDataProvider.Load();
            var playerData = _playerDataProvider.GetData();
            
            if (playerData == null)
            {
                Debug.LogError("[PointsService] PlayerData is null after Load. Это не должно происходить.");
                playerData = _playerDataProvider.GetData();
                 if (playerData == null) {
                    Debug.LogError("[PointsService] PlayerData все еще null. Сервис не может быть инициализирован.");
                    return;
                 }
            }

            if (playerData.GameData == null)
            {
                Debug.LogWarning("[PointsService] GameData is null. Создаем новый GameData.");
                playerData.GameData = new GameData();
                await _playerDataProvider.Save();
            }
            
            _playerData = playerData.GameData;
            
            Debug.Log($"[PointsService] Асинхронная инициализация системы очков завершена. Текущее количество: {CurrentPoints}");
        }

        #endregion

        #region Public Methods

        public async Task AddPoints(int amount, PointsSource source, string description = null)
        {
            Debug.Log($"[PointsService] AddPoints вызван. Amount: {amount}, Source: {source}, Description: '{description}'"); 
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка добавить отрицательное число очков: {amount}");
                return;
            }
            
            if (_playerData == null) 
            {
                 Debug.LogError("[PointsService] _playerData is null in AddPoints. Убедитесь, что InitializeAsync был вызван.");
                 return;
            }

            int finalAmount = ApplyMultiplier(amount, source);
            _playerData.Points += finalAmount;
            _playerData.TotalEarnedPoints += finalAmount;
            
            _playerData.PointsTransactions.Add(new PointsTransaction
            {
                Amount = finalAmount, 
                Source = source,
                Timestamp = DateTime.Now,
                Description = description
            });
            
            await _playerDataProvider.Save();
            
            OnPointsChanged?.Invoke(CurrentPoints);
            OnPointsEarned?.Invoke(finalAmount, source);
            
            Debug.Log($"Добавлено {finalAmount} очков из источника {source}. Текущее количество: {CurrentPoints}");
        }

        public async Task<bool> SpendPoints(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка списать отрицательное число очков: {amount}");
                return false;
            }

            if (_playerData == null) 
            {
                 Debug.LogError("[PointsService] _playerData is null in SpendPoints. Убедитесь, что InitializeAsync был вызван.");
                 return false;
            }

            if (CurrentPoints < amount)
            {
                Debug.LogWarning($"Недостаточно очков для списания: {CurrentPoints}/{amount}");
                return false;
            }

            _playerData.Points -= amount;
            
            _playerData.PointsTransactions.Add(new PointsTransaction
            {
                Amount = -amount, 
                Source = PointsSource.Spending,
                Timestamp = DateTime.Now
            });
            
            await _playerDataProvider.Save();
            
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

        public async Task AddPointsForEmotion()
        {
            if (_playerData == null) 
            {
                 Debug.LogError("[PointsService] _playerData is null in AddPointsForEmotion. Убедитесь, что InitializeAsync был вызван.");
                 return;
            }
            await AddPoints(c_BaseEmotionPoints, PointsSource.EmotionMarked, "Emotion Marked");
        }

        public async Task AddDailyBonus()
        {
            if (_playerData == null) 
            {
                 Debug.LogError("[PointsService] _playerData is null in AddDailyBonus. Убедитесь, что InitializeAsync был вызван.");
                 return;
            }
            var today = DateTime.Today;
            if (_playerData.LastDailyBonusDate.Date == today)
            {
                Debug.Log("Ежедневный бонус уже был получен сегодня");
                return;
            }
            
            await AddPoints(c_DefaultDailyBonus, PointsSource.DailyBonus);
            _playerData.LastDailyBonusDate = today;
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