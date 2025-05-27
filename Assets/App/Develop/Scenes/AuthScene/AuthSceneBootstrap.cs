using System;
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

            PanelManager panelManager = _container.Resolve<PanelManager>();
            if (panelManager == null)
            {
                throw new InvalidOperationException("PanelManager not found in container!");
            }

            AuthUIController authUIController = await panelManager.ShowPanelAsync<AuthUIController>(AssetAddresses.AuthPanel);
            if (authUIController == null)
            {
                throw new InvalidOperationException($"Failed to load or find AuthUIController on panel {AssetAddresses.AuthPanel}");
            }

            GameObject authPanelInstance = authUIController.gameObject;

            IAuthManager authManager = _container.Resolve<IAuthManager>();
            if (authManager == null)
            {
                throw new InvalidOperationException("IAuthManager not found in container!");
            }
            authManager.Initialize(authUIController);

            ProfileSetupUI profileSetupUI = authPanelInstance.GetComponent<ProfileSetupUI>();
            if (profileSetupUI == null)
            {
                throw new InvalidOperationException("ProfileSetupUI component not found on AuthPanel prefab");
            }
            else if (profileSetupUI is IInjectable injectableProfile)
            {
                injectableProfile.Inject(_container);
            }
        }
    }
}
