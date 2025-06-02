using System;
using System.Collections.Generic;
using App.Develop.DI;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Менеджер для координации работы всех installer'ов сервисов
    /// </summary>
    public class ServiceInstallerManager
    {
        private readonly List<IServiceInstaller> _installers;

        public ServiceInstallerManager()
        {
            _installers = new List<IServiceInstaller>();
        }

        /// <summary>
        /// Добавляет installer в список для регистрации
        /// </summary>
        public void AddInstaller(IServiceInstaller installer)
        {
            if (installer == null)
            {
                MyLogger.LogError("Попытка добавить null installer", MyLogger.LogCategory.Bootstrap);
                return;
            }

            _installers.Add(installer);
            MyLogger.Log($"📦 Добавлен installer: {installer.InstallerName}", MyLogger.LogCategory.Bootstrap);
        }

        /// <summary>
        /// Регистрирует все сервисы через добавленные installer'ы
        /// </summary>
        public void RegisterAllServices(DIContainer container)
        {
            if (container == null)
            {
                MyLogger.LogError("DIContainer не может быть null", MyLogger.LogCategory.Bootstrap);
                return;
            }

            MyLogger.Log($"🚀 Начинаем регистрацию сервисов через {_installers.Count} installer'ов", MyLogger.LogCategory.Bootstrap);

            foreach (var installer in _installers)
            {
                try
                {
                    installer.RegisterServices(container);
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ Ошибка в installer {installer.InstallerName}: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Bootstrap);
                    throw;
                }
            }

            MyLogger.Log("✅ Все сервисы успешно зарегистрированы", MyLogger.LogCategory.Bootstrap);
        }

        /// <summary>
        /// Очищает список installer'ов
        /// </summary>
        public void Clear()
        {
            _installers.Clear();
        }

        /// <summary>
        /// Возвращает количество зарегистрированных installer'ов
        /// </summary>
        public int InstallerCount => _installers.Count;
    }
} 