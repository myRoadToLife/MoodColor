namespace App.Develop.CommonServices.DataManagement
{
    public interface IDataRepository
    {
        //Сделать методы асинхронные
        string Read(string key);
        void Write(string key, string serializedData);
        void Remove(string key);
        bool Exists(string key);
    }
}
