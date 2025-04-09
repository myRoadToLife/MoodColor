using App.Develop.CommonServices.DataManagement.DataProviders;

namespace App.Develop.CommonServices.DataManagement
{
    public class SaveLoadService : ISaveLoadService
    {
        private readonly IDataSerializer _dataSerializer;
        private readonly IDataRepository _dataRepository;

        public SaveLoadService(IDataSerializer dataSerializer, IDataRepository dataRepository)
        {
            _dataSerializer = dataSerializer;
            _dataRepository = dataRepository;
        }

        public void Save <TDada>(TDada data) where TDada : ISaveData
        {
            string serializeData = _dataSerializer.Serialize(data);
            _dataRepository.Write(SaveDataKeys.GetSaveDataKey<TDada>(), serializeData);
        }

        public bool TryLoad <TData>(out TData data) where TData : ISaveData
        {
            string key = SaveDataKeys.GetSaveDataKey<TData>();

            if (_dataRepository.Exists(key) == false)
            {
                data = default(TData);
                return false;
            }

            string serializeData = _dataRepository.Read(key);
            data = _dataSerializer.Deserialize<TData>(serializeData);

            return true;
        }
    }
}
