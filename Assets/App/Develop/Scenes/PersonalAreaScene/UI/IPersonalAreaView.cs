using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Интерфейс для View в MVP паттерне личного кабинета
    /// Определяет контракт для отображения данных и обработки пользовательского ввода
    /// </summary>
    public interface IPersonalAreaView
    {
        #region События пользовательского ввода
        /// <summary>
        /// Событие запроса на запись эмоции
        /// </summary>
        event Func<Task> OnLogEmotionRequested;

        /// <summary>
        /// Событие запроса на открытие истории
        /// </summary>
        event Func<Task> OnHistoryRequested;

        /// <summary>
        /// Событие запроса на открытие списка друзей
        /// </summary>
        event Func<Task> OnFriendsRequested;

        /// <summary>
        /// Событие запроса на открытие мастерской
        /// </summary>
        event Func<Task> OnWorkshopRequested;

        /// <summary>
        /// Событие запроса на открытие настроек
        /// </summary>
        event Func<Task> OnSettingsRequested;

        /// <summary>
        /// Событие запроса на выход из приложения
        /// </summary>
        event Func<Task> OnQuitRequested;
        #endregion

        #region Методы отображения данных
        /// <summary>
        /// Устанавливает имя пользователя
        /// </summary>
        void SetUsername(string username);

        /// <summary>
        /// Устанавливает текущую эмоцию пользователя
        /// </summary>
        void SetCurrentEmotion(Sprite emotionSprite);

        /// <summary>
        /// Устанавливает количество эмоций в банке
        /// </summary>
        void SetEmotionJar(EmotionTypes type, float amount);

        /// <summary>
        /// Устанавливает количество очков
        /// </summary>
        void SetPoints(int points);

        /// <summary>
        /// Устанавливает количество записей
        /// </summary>
        void SetEntries(int entries);
        #endregion

        #region Методы управления UI
        /// <summary>
        /// Инициализирует view
        /// </summary>
        void Initialize();

        /// <summary>
        /// Показывает диалог подтверждения выхода из приложения
        /// </summary>
        Task ShowQuitConfirmationAsync();

        /// <summary>
        /// Очищает все данные в view
        /// </summary>
        void Clear();
        #endregion
    }
}