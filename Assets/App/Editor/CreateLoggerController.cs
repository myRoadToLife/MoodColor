using UnityEngine;
using UnityEditor;
using App.Develop.Utils.Logging;

namespace App.Editor
{
    public static class CreateLoggerController
    {
        [MenuItem("MoodColor/Utils/Create Logger Controller")]
        public static void CreateLoggerControllerOnScene()
        {
            // Проверяем, нет ли уже LoggerController на сцене
            LoggerController existingController = Object.FindObjectOfType<LoggerController>();
            if (existingController != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "LoggerController уже существует",
                    $"На сцене уже есть LoggerController на объекте '{existingController.gameObject.name}'.\n\nСоздать новый или выбрать существующий?",
                    "Создать новый",
                    "Выбрать существующий"
                );

                if (!overwrite)
                {
                    // Выбираем существующий
                    Selection.activeGameObject = existingController.gameObject;
                    EditorGUIUtility.PingObject(existingController.gameObject);
                    return;
                }
            }

            // Создаем новый GameObject
            GameObject loggerControllerGO = new GameObject("LoggerController");

            // Добавляем компонент LoggerController
            LoggerController controller = loggerControllerGO.AddComponent<LoggerController>();

            // Позиционируем объект
            loggerControllerGO.transform.position = Vector3.zero;

            // Добавляем иконку для лучшей видимости в Hierarchy
            var icon = EditorGUIUtility.IconContent("d_console.infoicon").image as Texture2D;
            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(loggerControllerGO, icon);
            }

            // Выбираем созданный объект
            Selection.activeGameObject = loggerControllerGO;
            EditorGUIUtility.PingObject(loggerControllerGO);

            // Уведомляем об успешном создании
            Debug.Log($"[CreateLoggerController] LoggerController создан на объекте '{loggerControllerGO.name}'");

            // Применяем текущие настройки
            controller.ApplySettingsToMyLogger();

            // Помечаем сцену как измененную
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }

        [MenuItem("MoodColor/Utils/Find Logger Controller")]
        public static void FindLoggerController()
        {
            LoggerController controller = Object.FindObjectOfType<LoggerController>();
            if (controller != null)
            {
                Selection.activeGameObject = controller.gameObject;
                EditorGUIUtility.PingObject(controller.gameObject);
                Debug.Log($"[CreateLoggerController] Найден LoggerController на объекте '{controller.gameObject.name}'");
            }
            else
            {
                bool create = EditorUtility.DisplayDialog(
                    "LoggerController не найден",
                    "На текущей сцене нет LoggerController.\n\nСоздать новый?",
                    "Создать",
                    "Отмена"
                );

                if (create)
                {
                    CreateLoggerControllerOnScene();
                }
            }
        }

        [MenuItem("MoodColor/Utils/Test MyLogger Categories")]
        public static void TestMyLoggerCategories()
        {
            LoggerController controller = Object.FindObjectOfType<LoggerController>();
            if (controller != null)
            {
                controller.TestAllCategories();
                Debug.Log("[CreateLoggerController] Тест всех категорий выполнен через LoggerController");
            }
            else
            {
                // Тестируем напрямую через MyLogger
                MyLogger.Log("Тест Default категории", MyLogger.LogCategory.Default);
                MyLogger.Log("Тест Sync категории", MyLogger.LogCategory.Sync);
                MyLogger.Log("Тест UI категории", MyLogger.LogCategory.UI);
                MyLogger.Log("Тест Network категории", MyLogger.LogCategory.Network);
                MyLogger.Log("Тест Firebase категории", MyLogger.LogCategory.Firebase);
                MyLogger.Log("Тест Editor категории", MyLogger.LogCategory.Editor);
                MyLogger.Log("Тест Gameplay категории", MyLogger.LogCategory.Gameplay);
                MyLogger.Log("Тест Bootstrap категории", MyLogger.LogCategory.Bootstrap);
                MyLogger.Log("Тест Emotion категории", MyLogger.LogCategory.Emotion);
                MyLogger.Log("Тест ClearHistory категории", MyLogger.LogCategory.ClearHistory);
                MyLogger.Log("Тест Regional категории", MyLogger.LogCategory.Regional);

                Debug.Log("[CreateLoggerController] Тест всех категорий выполнен напрямую через MyLogger");
            }
        }
    }
}