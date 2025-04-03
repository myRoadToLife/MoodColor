using System;
using System.Collections.Generic;
using App.Develop.CommonServices.DataManagement.DataProviders;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement
{
    public static class SaveDataKeys
    {
        private static Dictionary<Type, string> Keys = new Dictionary<Type, string>()
        {
            { typeof(PlayerData), "PlayerData" },
        };

        public static string GetSaveDataKey <TDada>() where TDada: ISaveData
            => Keys[typeof(TDada)];
    }
}
