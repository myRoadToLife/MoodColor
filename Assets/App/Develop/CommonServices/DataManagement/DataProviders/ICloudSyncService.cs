using System;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public interface ICloudSyncService : IDisposable
    {
        /// <summary>
        /// Синхронизирует данные с облаком
        /// </summary>
        /// <returns>True, если синхронизация успешна, иначе False</returns>
        Task<bool> SyncDataToCloud();
        
        /// <summary>
        /// Загружает данные из облака
        /// </summary>
        /// <returns>True, если загрузка успешна, иначе False</returns>
        Task<bool> LoadDataFromCloud();
        
        /// <summary>
        /// Получает статус последней синхронизации
        /// </summary>
        /// <returns>Статус последней синхронизации</returns>
        SyncStatusData GetLastSyncStatus();
        
        /// <summary>
        /// Сохраняет статус синхронизации
        /// </summary>
        /// <param name="isSuccessful">Была ли синхронизация успешной</param>
        /// <param name="errorMessage">Сообщение об ошибке (если синхронизация не удалась)</param>
        void SaveSyncStatus(bool isSuccessful, string errorMessage = "");
    }
} 