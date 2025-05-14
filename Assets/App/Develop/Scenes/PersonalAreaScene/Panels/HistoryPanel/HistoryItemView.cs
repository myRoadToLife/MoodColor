using App.Develop.CommonServices.Emotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Убедись, что это пространство имен доступно и содержит EmotionHistoryEntry и EmotionData

    namespace App.Develop.Scenes.PersonalAreaScene.Panels.HistoryPanel
    {
        public class HistoryItemView : MonoBehaviour
        {
            [SerializeField] private Image _emotionIndicator;
            [SerializeField] private TextMeshProUGUI _dateText;
            [SerializeField] private TextMeshProUGUI _timeText;
            [SerializeField] private TextMeshProUGUI _emotionNameText; // Для отображения типа эмоции

            public void Setup(EmotionHistoryEntry entry)
            {
                if (entry == null)
                {
                    Debug.LogError("[HistoryItemView] Received entry is NULL!");
                    gameObject.SetActive(false);
                    return;
                }
                if (entry.EmotionData == null)
                {
                    Debug.LogError($"[HistoryItemView] Received entry.EmotionData is NULL! Timestamp from entry: {entry.Timestamp}");
                    if (_dateText != null) _dateText.text = entry.Timestamp.ToString("dd.MM.yyyy");
                    if (_timeText != null) _timeText.text = entry.Timestamp.ToString("HH:mm");
                    if (_emotionNameText != null) _emotionNameText.text = "Ошибка данных";
                    if (_emotionIndicator != null) _emotionIndicator.color = Color.magenta; // Явный цвет ошибки
                    gameObject.SetActive(true); // Показать, чтобы увидеть ошибку
                    return;
                }

                Debug.Log($"[HistoryItemView Setup] Displaying: Timestamp='{entry.Timestamp}', EmotionType='{entry.EmotionData.Type}', Color='{entry.EmotionData.Color}', Value='{entry.EmotionData.Value}'");

                gameObject.SetActive(true);

                if (_dateText != null)
                    _dateText.text = entry.Timestamp.ToLocalTime().ToString("dd.MM.yyyy");
            
                if (_timeText != null)
                    _timeText.text = entry.Timestamp.ToLocalTime().ToString("HH:mm");

                if (_emotionIndicator != null)
                {
                    // Предполагается, что в EmotionData есть поле Color типа UnityEngine.Color
                    _emotionIndicator.color = entry.EmotionData.Color; 
                    // Если у тебя для каждой эмоции своя иконка, то здесь будет логика вроде:
                    // _emotionIndicator.sprite = GetSpriteForEmotion(entry.EmotionData.Type);
                }

                if (_emotionNameText != null)
                {
                    // В EmotionData.Type (string) хранится системное имя типа эмоции (например, "Joy", "Sadness")
                    // Здесь можно его напрямую отобразить или получить более "красивое" или локализованное название
                    _emotionNameText.text = entry.EmotionData.Type; 
                }
            }

            // Пример вспомогательного метода, если бы у тебя были спрайты для эмоций:
            // private Sprite GetSpriteForEmotion(string emotionType)
            // {
            //     // Здесь логика загрузки или выбора спрайта на основе emotionType
            //     // Например, из Resources или через ScriptableObject с конфигурацией эмоций
            //     return null; 
            // }
        }
    }