// Assets/App/Develop/AppServices/Firebase/Auth/ProfileSetupUI.cs
using System;
using System.Collections.Generic;
using App.Develop.AppServices.Firebase.Auth.Services;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using TMPro;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
{
    public class ProfileSetupUI : MonoBehaviour, IInjectable
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _nicknameInput;
        [SerializeField] private TMP_Dropdown _genderDropdown;
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;

        private UserProfileService _profileService;
        private SceneSwitcher _sceneSwitcher;
        private bool _isProcessing;

        public void Inject(DIContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            try
            {
                _sceneSwitcher = container.Resolve<SceneSwitcher>();
                _profileService = container.Resolve<UserProfileService>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации ProfileSetupUI: {ex.Message}");
            }
        }

        public async void OnContinueProfile()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                string nickname = _nicknameInput.text.Trim();
                string gender = _genderDropdown.options[_genderDropdown.value].text.ToLower();

                if (!IsValidNickname(nickname))
                {
                    ShowPopup("Никнейм должен содержать только латинские буквы без пробелов");
                    return;
                }

                bool success = await _profileService.SetupProfile(nickname, gender);
                if (success)
                {
                    ShowPopup("Профиль успешно обновлен!");
                    GoToPersonalArea();
                }
                else
                {
                    ShowPopup("Ошибка при обновлении профиля");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при установке профиля: {ex.Message}");
                ShowPopup("Произошла ошибка. Попробуйте позже.");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void OnSkipProfile()
        {
            GoToPersonalArea();
        }

        private bool IsValidNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return false;

            foreach (char c in nickname)
            {
                if (!char.IsLetter(c) || c > 127) // латиница, без юникода/кириллицы
                    return false;
            }

            return true;
        }

        private void GoToPersonalArea()
        {
            _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
        }

        private void ShowPopup(string message)
        {
            _popupPanel.SetActive(true);
            _popupText.text = message;

            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup()
        {
            _popupPanel.SetActive(false);
        }
    }
}