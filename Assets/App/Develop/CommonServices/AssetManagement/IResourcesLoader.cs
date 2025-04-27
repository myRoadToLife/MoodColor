using UnityEngine;

namespace App.Develop.CommonServices.AssetManagement
{
    /// <summary>
    /// Интерфейс для загрузки ресурсов из папки Resources
    /// </summary>
    public interface IResourcesLoader
    {
        /// <summary>
        /// Загружает ассет по указанному пути
        /// </summary>
        /// <typeparam name="T">Тип загружаемого ассета</typeparam>
        /// <param name="path">Путь к ассету в папке Resources</param>
        /// <returns>Загруженный ассет или null, если ассет не найден</returns>
        T LoadAsset<T>(string path) where T : Object;

        /// <summary>
        /// Выгружает ассет из памяти
        /// </summary>
        /// <param name="asset">Ассет для выгрузки</param>
        void UnloadAsset(Object asset);
    }
} 