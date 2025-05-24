using UnityEngine;
using UnityEditor;
using System.IO;
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Panels
{
    public static class PersonalAreaPanelsGenerator
    {
        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/FriendsPanel")]
        public static void GenerateFriendsPanels()
        {
            // Генерируем панель друзей
            MyLogger.EditorLog("🔄 Генерация панели друзей...");
            FriendsPanelGenerator.CreateFriendsPanelPrefab();
            
            // Генерируем панель поиска друзей
            MyLogger.EditorLog("🔄 Генерация панели поиска друзей...");
            FriendSearchPanelGenerator.GenerateFriendSearchPanel();
            
            MyLogger.EditorLog("✅ Все панели для функциональности друзей успешно сгенерированы!");
        }
    }
}