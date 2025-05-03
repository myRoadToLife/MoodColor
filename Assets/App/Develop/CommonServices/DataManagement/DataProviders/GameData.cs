using System;
using System.Collections.Generic;
using App.Develop.CommonServices.GameSystem;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    /// <summary>
    /// Данные игровой системы
    /// </summary>
    [Serializable]
    public class GameData
    {
        /// <summary>
        /// Текущее количество очков
        /// </summary>
        public int Points;
        
        /// <summary>
        /// Общее количество заработанных очков за всё время
        /// </summary>
        public int TotalEarnedPoints;
        
        /// <summary>
        /// История транзакций с очками
        /// </summary>
        public List<PointsTransaction> PointsTransactions;
        
        /// <summary>
        /// Последняя дата получения ежедневного бонуса
        /// </summary>
        public DateTime LastDailyBonusDate;
        
        public GameData()
        {
            Points = 0;
            TotalEarnedPoints = 0;
            PointsTransactions = new List<PointsTransaction>();
            LastDailyBonusDate = DateTime.MinValue;
        }
    }
} 