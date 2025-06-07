using System.Collections.Generic;
using UnityEngine;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Пул объектов для переиспользования UI элементов
    /// </summary>
    public class UIObjectPool<T> where T : Component
    {
        #region Private Fields
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый пул объектов
        /// </summary>
        /// <param name="prefab">Префаб для создания объектов</param>
        /// <param name="parent">Родительский объект для размещения</param>
        /// <param name="initialSize">Начальный размер пула</param>
        public UIObjectPool(GameObject prefab, Transform parent, int initialSize = 5)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;

            InitializePool();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Получает объект из пула или создает новый
        /// </summary>
        public T Get()
        {
            T item;

            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
            }
            else
            {
                item = CreateNewItem();
            }

            item.gameObject.SetActive(true);
            return item;
        }

        /// <summary>
        /// Возвращает объект в пул
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }

        /// <summary>
        /// Возвращает все активные объекты в пул
        /// </summary>
        public void ReturnAll()
        {
            if (_parent == null) return;

            for (int i = 0; i < _parent.childCount; i++)
            {
                var child = _parent.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    var component = child.GetComponent<T>();
                    if (component != null)
                    {
                        Return(component);
                    }
                }
            }
        }

        /// <summary>
        /// Очищает пул и уничтожает все объекты
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var item = _pool.Dequeue();
                if (item != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(item.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(item.gameObject);
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void InitializePool()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var item = CreateNewItem();
                item.gameObject.SetActive(false);
                _pool.Enqueue(item);
            }
        }

        private T CreateNewItem()
        {
            var gameObject = Object.Instantiate(_prefab, _parent);
            var component = gameObject.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"Префаб {_prefab.name} не содержит компонент {typeof(T).Name}");
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
        #endregion
    }
} 