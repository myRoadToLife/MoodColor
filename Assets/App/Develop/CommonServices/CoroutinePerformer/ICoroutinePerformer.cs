using System.Collections;
using UnityEngine;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    public interface ICoroutinePerformer
    {
        Coroutine StartPerformCoroutine(IEnumerator routine);
        
        void StopPerformCoroutine(Coroutine routine);
    }
}
