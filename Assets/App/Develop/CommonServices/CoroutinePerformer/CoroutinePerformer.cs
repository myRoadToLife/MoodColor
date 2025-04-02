using System;
using System.Collections;
using UnityEngine;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    public class CoroutinePerformer : MonoBehaviour, ICoroutinePerformer
    {
        private void Awake() =>
            DontDestroyOnLoad(this);

        public Coroutine StartPerformCoroutine(IEnumerator routine)
            => StartCoroutine(routine);

        public void StopPerformCoroutine(Coroutine routine)
            => StopCoroutine(routine);
    }
}
