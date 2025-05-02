using App.Develop.DI;

namespace App.Develop.DI
{
    /// <summary>
    /// Интерфейс для регистрации группы связанных сервисов в контейнере зависимостей
    /// </summary>
    public interface IServiceRegistrator
    {
        /// <summary>
        /// Регистрирует группу сервисов в контейнере
        /// </summary>
        /// <param name="container">Контейнер зависимостей</param>
        void Register(DIContainer container);
    }
} 