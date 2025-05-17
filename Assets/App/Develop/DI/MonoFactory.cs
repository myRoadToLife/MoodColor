using UnityEngine;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.DI
{
    public class MonoFactory
    {
        private readonly DIContainer _container;

        public MonoFactory(DIContainer container)
        {
            _container = container;
        }

        public T Create <T>(GameObject prefab, Transform parent = null) where T : MonoBehaviour
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            T component = instance.GetComponent<T>();

            if (component is IInjectable injectable)
            {
                injectable.Inject(_container);
            }

            return component;
        }

        public T CreateOn <T>(GameObject existingObject) where T : MonoBehaviour
        {
            // Этот метод может быть не совсем корректен, если компонент T находится не на existingObject, а на его дочернем элементе.
            // Для Addressables панелей, PanelManager использует GetComponentInChildren, что правильно.
            T component = existingObject.GetComponent<T>(); 

            if (component is IInjectable injectable)
            {
                injectable.Inject(_container);
            }

            return component;
        }

        /// <summary>
        /// Внедряет зависимости в уже существующий MonoBehaviour компонент.
        /// </summary>
        public void InjectDependencies<T>(T component) where T : MonoBehaviour
        {
            if (component == null)
            {
                Logger.LogError("[MonoFactory] Попытка внедрить зависимости в null компонент.");
                return;
            }

            if (component is IInjectable injectable)
            {
                injectable.Inject(_container);
                Logger.Log($"[MonoFactory] Зависимости успешно внедрены в {component.GetType().Name} на объекте {component.gameObject.name}");
            }
            else
            {
                Logger.LogWarning($"[MonoFactory] Компонент {component.GetType().Name} на объекте {component.gameObject.name} не реализует IInjectable. Инъекция не выполнена.");
            }
        }
    }
}
