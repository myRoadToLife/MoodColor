using System.Collections.Generic;
using UnityEngine;
using App.Develop.CommonServices.Social;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Базовый класс для генераторов элементов интерфейса
    /// </summary>
    public abstract class BaseItemGenerator : MonoBehaviour
    {
        [SerializeField] protected Transform _itemContainer;
        [SerializeField] protected GameObject _itemPrefab;
        [SerializeField] protected GameObject _noItemsMessage;
        [SerializeField] protected float _itemSpacing = 5f;

        protected ISocialService _socialService;
        protected List<GameObject> _instantiatedItems = new List<GameObject>();

        public virtual void Initialize(ISocialService socialService)
        {
            _socialService = socialService;
        }

        /// <summary>
        /// Очистка контейнера элементов
        /// </summary>
        protected virtual void ClearContainer()
        {
            foreach (var item in _instantiatedItems)
            {
                Destroy(item);
            }
            
            _instantiatedItems.Clear();
        }

        /// <summary>
        /// Показать сообщение об отсутствии элементов
        /// </summary>
        protected virtual void ShowNoItemsMessage(bool show)
        {
            if (_noItemsMessage != null)
            {
                _noItemsMessage.SetActive(show);
            }
        }

        /// <summary>
        /// Добавление элемента в контейнер
        /// </summary>
        protected virtual GameObject AddItem()
        {
            var item = Instantiate(_itemPrefab, _itemContainer);
            _instantiatedItems.Add(item);
            return item;
        }
    }
}