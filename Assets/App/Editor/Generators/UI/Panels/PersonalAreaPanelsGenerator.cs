using UnityEngine;
using UnityEditor;
using System.IO;

namespace App.Editor.Generators.UI.Panels
{
    public static class PersonalAreaPanelsGenerator
    {
        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/FriendsPanel")]
        public static void GenerateFriendsPanels()
        {
            // Генерируем панель друзей
            Debug.Log("🔄 Генерация панели друзей...");
            FriendsPanelGenerator.CreateFriendsPanelPrefab();
            
            // Генерируем панель поиска друзей
            Debug.Log("🔄 Генерация панели поиска друзей...");
            FriendSearchPanelGenerator.GenerateFriendSearchPanel();
            
            Debug.Log("✅ Все панели для функциональности друзей успешно сгенерированы!");
        }
    }
}