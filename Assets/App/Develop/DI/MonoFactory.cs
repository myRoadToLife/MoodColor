using UnityEngine;

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
            T component = existingObject.GetComponent<T>();

            if (component is IInjectable injectable)
            {
                injectable.Inject(_container);
            }

            return component;
        }
    }
}
