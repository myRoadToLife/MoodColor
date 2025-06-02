using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.GameSystem;
using App.Develop.Utils.Logging;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Управляет загрузкой и отображением статистики пользователя
    /// Отвечает только за работу со статистикой (очки, записи)
    /// </summary>
    public class PersonalAreaStatisticsManager : IDisposable
    {
        private readonly IPersonalAreaView _view;
        private readonly IPointsService _pointsService;
        private bool _isDisposed;

        public PersonalAreaStatisticsManager(IPersonalAreaView view, IPointsService pointsService = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pointsService = pointsService; // Может быть null, если сервис недоступен
        }

        /// <summary>
        /// Инициализирует статистику пользователя
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_pointsService == null)
            {
                // Устанавливаем нулевые значения, если сервис недоступен
                _view.SetPoints(0);
                _view.SetEntries(0);
                MyLogger.Log("PointsService недоступен, используются нулевые значения статистики", MyLogger.LogCategory.UI);
                return;
            }

            try
            {
                // Отображаем текущие данные сразу
                UpdateStatisticsView();
                
                // Подписываемся на обновления
                _pointsService.OnPointsChanged += HandlePointsChanged;
                
                // Загружаем актуальные данные из Firebase
                await _pointsService.InitializeAsync();
                
                // Обновляем представление после загрузки
                UpdateStatisticsView();
                
                MyLogger.Log("Статистика пользователя инициализирована", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка инициализации статистики: {ex.Message}", MyLogger.LogCategory.UI);
                
                // Все равно отображаем доступные данные
                UpdateStatisticsView();
                
                // Не бросаем исключение, так как приложение может работать без статистики
            }
        }

        /// <summary>
        /// Обновляет отображение статистики
        /// </summary>
        private void UpdateStatisticsView()
        {
            if (_pointsService == null)
            {
                _view.SetPoints(0);
                _view.SetEntries(0);
                return;
            }

            try
            {
                int currentPoints = _pointsService.CurrentPoints;
                int entriesCount = _pointsService.GetTransactionsHistory()?.Count ?? 0;

                _view.SetPoints(currentPoints);
                _view.SetEntries(entriesCount);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления статистики: {ex.Message}", MyLogger.LogCategory.UI);
                
                // В случае ошибки показываем нулевые значения
                _view.SetPoints(0);
                _view.SetEntries(0);
            }
        }

        /// <summary>
        /// Обработчик изменения очков
        /// </summary>
        private void HandlePointsChanged(int newPointsValue)
        {
            if (_isDisposed) return;
            
            try
            {
                UpdateStatisticsView();
                MyLogger.Log($"Очки обновлены: {newPointsValue}", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обработки изменения очков: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        /// <summary>
        /// Принудительно обновляет статистику
        /// </summary>
        public async Task RefreshAsync()
        {
            if (_pointsService == null) return;

            try
            {
                await _pointsService.InitializeAsync();
                UpdateStatisticsView();
                MyLogger.Log("Статистика обновлена принудительно", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка принудительного обновления статистики: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                if (_pointsService != null)
                {
                    _pointsService.OnPointsChanged -= HandlePointsChanged;
                }
                
                _isDisposed = true;
                MyLogger.Log("PersonalAreaStatisticsManager освобожден", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка освобождения ресурсов StatisticsManager: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
    }
} 