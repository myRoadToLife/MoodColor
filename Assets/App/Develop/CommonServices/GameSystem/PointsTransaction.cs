using System;

namespace App.Develop.CommonServices.GameSystem
{
    /// <summary>
    /// Класс для отслеживания транзакций с очками
    /// </summary>
    [Serializable]
    public class PointsTransaction
    {
        /// <summary>
        /// Количество очков в транзакции (положительное - начисление, отрицательное - списание)
        /// </summary>
        public int Amount;
        
        /// <summary>
        /// Источник транзакции
        /// </summary>
        public PointsSource Source;
        
        /// <summary>
        /// Время транзакции
        /// </summary>
        public DateTime Timestamp;
        
        /// <summary>
        /// Дополнительное описание транзакции
        /// </summary>
        public string Description;
    }
} 