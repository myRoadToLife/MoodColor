using App.Develop.DI;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Интерфейс для регистрации группы связанных сервисов в контейнере зависимостей
    /// </summary>
    public interface IServiceInstaller
    {
        /// <summary>
        /// Регистрирует группу сервисов в контейнере
        /// </summary>
        /// <param name="container">Контейнер зависимостей</param>
        void RegisterServices(DIContainer container);
        
        /// <summary>
        /// Название installer'а для логирования
        /// </summary>
        string InstallerName { get; }
    }
} 