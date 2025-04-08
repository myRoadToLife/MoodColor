using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public abstract class DataProvider <TData> where TData : ISaveData
    {
        private readonly ISaveLoadService _saveLoadService;

        private List<IDataWriter<TData>> _writers = new List<IDataWriter<TData>>();
        private List<IDataReader<TData>> _readers = new List<IDataReader<TData>>();

        public DataProvider(ISaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
        }

        private TData Data { get; set; }

        public void RegisterWriter(IDataWriter<TData> writer)
        {
            if (_writers.Contains(writer))
                throw new ArgumentException(nameof(writer));

            _writers.Add(writer);
        }

        public void RegisterReader(IDataReader<TData> reader)
        {
            if (_readers.Contains(reader))
                throw new ArgumentException(nameof(reader));

            _readers.Add(reader);
        }

        public void Load()
        {
            if (_saveLoadService.TryLoad(out TData data))
                Data = data;
            else
                Reset();

            foreach (IDataReader<TData> reader in _readers)
                reader.ReadFrom(Data);
        }

        protected abstract TData GetOrigenData();

        private void Reset()
        {
            Data = GetOrigenData();
            Save();
        }

        public void Save()
        {
            foreach (IDataWriter<TData> writer in _writers)
                writer.WriteTo(Data);

            _saveLoadService.Save(Data);
        }
    }
}
