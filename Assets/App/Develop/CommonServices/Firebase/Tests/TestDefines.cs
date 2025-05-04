#if UNITY_EDITOR
using UnityEditor;

// Этот файл содержит настройки условной компиляции,
// которые гарантируют, что тестовые классы не будут включены в релизные сборки

namespace App.Develop.AppServices.Firebase.Tests
{
    public class TestDefines : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            // Добавляем символ DEVELOPMENT_BUILD при разработке,
            // чтобы тестовые скрипты были доступны в debug-сборках
            if (EditorUserBuildSettings.development)
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup);
                
                if (!symbols.Contains("DEVELOPMENT_BUILD"))
                {
                    symbols += ";DEVELOPMENT_BUILD";
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
                }
            }
        }
    }
}
#endif 