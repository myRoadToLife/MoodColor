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

        public bool TryLoad <TDada>(out TDada data) where TDada : ISaveData
        {
            string key = SaveDataKeys.GetSaveDataKey<TDada>();

            if (_dataRepository.Exists(key) == false)
            {
                data = default(TDada);
                return false;
            }

            string serializeData = _dataRepository.Read(key);
            data = _dataSerializer.Deserialize<TDada>(serializeData);

            return true;
        }
    }
}
