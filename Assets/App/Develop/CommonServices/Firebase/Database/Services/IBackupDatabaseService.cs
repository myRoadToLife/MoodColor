using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с резервными копиями в Firebase Database
    /// </summary>
    public interface IBackupDatabaseService
    {
        /// <summary>
        /// Создает резервную копию данных пользователя
        /// </summary>
        /// <returns>ID созданной резервной копии</returns>
        Task<string> CreateBackup();
        
        /// <summary>
        /// Восстанавливает данные из резервной копии
        /// </summary>
        /// <param name="backupId">Идентификатор резервной копии</param>
        /// <returns>True, если данные успешно восстановлены</returns>
        Task<bool> RestoreFromBackup(string backupId);
        
        /// <summary>
        /// Получает список доступных резервных копий
        /// </summary>
        /// <returns>Массив ID доступных резервных копий</returns>
        Task<string[]> GetAvailableBackups();
    }
} 