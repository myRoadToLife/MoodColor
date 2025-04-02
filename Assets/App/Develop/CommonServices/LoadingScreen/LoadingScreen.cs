using System;
using UnityEngine;

namespace App.Develop.CommonServices.LoadingScreen
{
    public class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            Hide();
            DontDestroyOnLoad(this);
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);
       
    }
}
