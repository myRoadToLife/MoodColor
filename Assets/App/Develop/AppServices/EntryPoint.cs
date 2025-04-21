using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase;
using UnityEngine;

namespace App.Develop.AppServices
{
    public class EntryPoint : MonoBehaviour
    {
        private ServiceCollection _services;
        private FirebaseManager _firebaseManager;
        private AuthService _authService;
        private UserProfileService _userProfileService;

        private void Awake()
        {
            _services = new ServiceCollection();
            _firebaseManager = new FirebaseManager();
            
            _authService = new AuthService(_firebaseManager);
            _userProfileService = new UserProfileService(_firebaseManager);

            _services.AddSingleton(_firebaseManager);
            _services.AddSingleton(_authService);
            _services.AddSingleton(_userProfileService);
        }

        private void OnDestroy()
        {
            _userProfileService?.Cleanup();
        }
    }
} 