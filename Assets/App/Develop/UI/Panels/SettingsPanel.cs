using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.UI.Base;
using UnityEngine;

namespace App.Develop.UI.Panels
{
    public class SettingsPanel : BasePanel
    {
        [SerializeField] private SettingsPanelController _controller;
        
        public override void Show()
        {
            base.Show();
            
            // Дополнительная логика при показе панели настроек
            Debug.Log("Панель настроек отображена");
        }
        
        public override void Hide()
        {
            base.Hide();
            
            // Дополнительная логика при скрытии панели настроек
            Debug.Log("Панель настроек скрыта");
        }
    }
} 