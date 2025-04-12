using App.Develop.DI;
using UnityEngine;

namespace App.Develop.AuthScene
{
    public class DiContainerHolder : MonoBehaviour
    {
        public DIContainer Container { get; private set; }

        public void SetContainer(DIContainer container)
        {
            Container = container;
            DontDestroyOnLoad(gameObject);
        }
    }
}
