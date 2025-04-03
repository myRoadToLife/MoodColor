namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public interface ISaveLoadService
    {
        bool TryLoad<TDada>(out TDada data) where TDada : ISaveData;
        void Save<TDada>(TDada data) where TDada : ISaveData;
    }
}
