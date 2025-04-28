using UnityEngine;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Base
{
    public abstract class BaseUIElement : MonoBehaviour
    {
        protected virtual void OnValidate()
        {
            ValidateReferences();
        }

        protected virtual void ValidateReferences() { }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        protected virtual void UnsubscribeFromEvents() { }

        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }
    }
} 