using System;
using System.Threading.Tasks;
using Firebase.Database;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Common.Examples
{
    /// <summary>
    /// Простая реализация IDatabaseOperation для демонстрации работы OfflineManager
    /// </summary>
    public class SimpleDatabaseOperation : IDatabaseOperation
    {
        #region Fields

        private readonly DatabaseReference _reference;
        private readonly object _data;
        private readonly string _path;

        #endregion

        #region Properties

        /// <summary>
        /// Описание операции для логирования
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Приоритет операции (чем выше число, тем важнее)
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Идентификатор операции для предотвращения дублирования
        /// </summary>
        public string OperationId { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор для операции записи данных
        /// </summary>
        /// <param name="reference">Ссылка на узел базы данных</param>
        /// <param name="data">Данные для записи</param>
        /// <param name="priority">Приоритет операции</param>
        public SimpleDatabaseOperation(DatabaseReference reference, object data, int priority = 1)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
            _data = data;
            _path = reference.Key ?? "root";

            Priority = priority;
            Description = $"Запись данных в {_path}";
            OperationId = $"write_{_path}_{DateTime.Now.Ticks}";
        }

        /// <summary>
        /// Конструктор для операции записи данных по пути
        /// </summary>
        /// <param name="path">Путь к данным</param>
        /// <param name="data">Данные для записи</param>
        /// <param name="priority">Приоритет операции</param>
        public SimpleDatabaseOperation(string path, object data, int priority = 1)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Путь не может быть пустым", nameof(path));

            _path = path;
            _data = data;
            Priority = priority;
            Description = $"Запись данных в {_path}";
            OperationId = $"write_{_path.Replace("/", "_")}_{DateTime.Now.Ticks}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Выполняет операцию записи в Firebase Database
        /// </summary>
        /// <returns>True, если операция выполнена успешно</returns>
        public async Task<bool> ExecuteAsync()
        {
            try
            {
                MyLogger.Log($"📝 [SimpleDatabaseOperation] Выполняем операцию: {Description}", MyLogger.LogCategory.Firebase);

                DatabaseReference reference = _reference;
                if (reference == null)
                {
                    // Если reference не задан, используем путь
                    reference = FirebaseDatabase.DefaultInstance.GetReference(_path);
                }

                // Выполняем запись данных
                await reference.SetValueAsync(_data);

                MyLogger.Log($"✅ [SimpleDatabaseOperation] Операция выполнена успешно: {Description}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SimpleDatabaseOperation] Ошибка выполнения операции: {Description}, ошибка: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw; // Перебрасываем исключение для обработки в OfflineManager
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Создает операцию записи данных пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="userData">Данные пользователя</param>
        /// <returns>Операция базы данных</returns>
        public static SimpleDatabaseOperation CreateUserDataOperation(string userId, object userData)
        {
            return new SimpleDatabaseOperation($"users/{userId}", userData, priority: 5);
        }

        /// <summary>
        /// Создает операцию записи эмоции
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="emotionData">Данные эмоции</param>
        /// <returns>Операция базы данных</returns>
        public static SimpleDatabaseOperation CreateEmotionOperation(string userId, object emotionData)
        {
            var emotionId = Guid.NewGuid().ToString();
            return new SimpleDatabaseOperation($"emotions/{userId}/{emotionId}", emotionData, priority: 3);
        }

        /// <summary>
        /// Создает операцию записи статистики
        /// </summary>
        /// <param name="statsData">Данные статистики</param>
        /// <returns>Операция базы данных</returns>
        public static SimpleDatabaseOperation CreateStatsOperation(object statsData)
        {
            return new SimpleDatabaseOperation("stats", statsData, priority: 1);
        }

        #endregion
    }
}