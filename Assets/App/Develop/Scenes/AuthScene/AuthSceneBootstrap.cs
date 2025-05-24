using System.Collections;
using App.Develop.CommonServices.Firebase.Auth;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;
using System.Threading.Tasks;
using App.Develop.CommonServices.UI;
using App.Develop.Utils.Logging;

namespace App.Develop.Scenes.AuthScene
{
    public class AuthSceneBootstrap : MonoBehaviour
    {
        private DIContainer _container;

        public async Task Run(DIContainer container, AuthSceneInputArgs inputArgs)
        {
            _container = container;
            _container.Initialize();

            MyLogger.Log("✅ [AuthSceneBootstrap] сцена загружена", MyLogger.LogCategory.Bootstrap);

            var panelManager = _container.Resolve<PanelManager>();
            if (panelManager == null)
            {
                MyLogger.LogError("❌ PanelManager не найден в контейнере!", MyLogger.LogCategory.Bootstrap);
                return;
            }
            
            AuthUIController authUIController = await panelManager.ShowPanelAsync<AuthUIController>(AssetAddresses.AuthPanel);
            
            if (authUIController == null)
            {
                MyLogger.LogError($"❌ Не удалось загрузить или найти AuthUIController на панели {AssetAddresses.AuthPanel}", MyLogger.LogCategory.Bootstrap);
                return;
            }
            
            GameObject authPanelInstance = authUIController.gameObject;
    
            var authManager = _container.Resolve<IAuthManager>();
            if (authManager == null)
            {
                MyLogger.LogError("❌ IAuthManager не найден в контейнере!", MyLogger.LogCategory.Bootstrap);
                return;
            }
            
            authManager.Initialize(authUIController);
            
            var profileSetupUI = authPanelInstance.GetComponent<ProfileSetupUI>();
            if (profileSetupUI == null)
            {
                MyLogger.LogError("❌ Не найден компонент ProfileSetupUI на префабе AuthPanel", MyLogger.LogCategory.Bootstrap);
            }
            else if (profileSetupUI is IInjectable injectableProfile)
            {
                injectableProfile.Inject(_container);
            }
        }
    }
}
