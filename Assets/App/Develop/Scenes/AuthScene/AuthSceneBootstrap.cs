using System.Collections;
using App.Develop.CommonServices.Firebase.Auth;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;
using System.Threading.Tasks;
using App.Develop.CommonServices.UI;

namespace App.Develop.Scenes.AuthScene
{
    public class AuthSceneBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public async Task Run(DIContainer container, AuthSceneInputArgs inputArgs)
        {
            _container = container;
            _container.Initialize();

            Debug.Log("✅ [AuthSceneBootstrap] сцена загружена");

            var panelManager = _container.Resolve<PanelManager>();
            if (panelManager == null)
            {
                Debug.LogError("❌ PanelManager не найден в контейнере!");
                return;
            }
            
            AuthUIController authUIController = await panelManager.ShowPanelAsync<AuthUIController>(AssetAddresses.AuthPanel);
            
            if (authUIController == null)
            {
                Debug.LogError($"❌ Не удалось загрузить или найти AuthUIController на панели {AssetAddresses.AuthPanel}");
                return;
            }
            
            GameObject authPanelInstance = authUIController.gameObject;
    
            var authManager = _container.Resolve<IAuthManager>();
            if (authManager == null)
            {
                Debug.LogError("❌ IAuthManager не найден в контейнере!");
                return;
            }
            
            authManager.Initialize(authUIController);
            
            var profileSetupUI = authPanelInstance.GetComponent<ProfileSetupUI>();
            if (profileSetupUI == null)
            {
                Debug.LogError("❌ Не найден компонент ProfileSetupUI на префабе AuthPanel");
            }
            else if (profileSetupUI is IInjectable injectableProfile)
            {
                injectableProfile.Inject(_container);
            }
        }
    }
}
