using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Base
{
    public abstract class BaseUIElement : MonoBehaviour
    {
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            ValidateReferences();
#endif
        }

        protected virtual void ValidateReferences() { }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        protected virtual void UnsubscribeFromEvents() { }

        protected void LogError(string message)
        {
            MyLogger.LogError($"[{GetType().Name}] {message}");
        }

        protected void LogWarning(string message)
        {
            MyLogger.LogWarning($"[{GetType().Name}] {message}");
        }
    }
} 