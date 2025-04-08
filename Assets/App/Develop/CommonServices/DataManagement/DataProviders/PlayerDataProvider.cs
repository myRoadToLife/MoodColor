namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        //Тут будем передавать сервис конфигов
        public PlayerDataProvider(ISaveLoadService saveLoadService) : base(saveLoadService)
        {
        }

        protected override PlayerData GetOrigenData()
        {
            return new PlayerData()
            {
                CurrentEmotion = 0,
                LastEmotion = 0,
            };
        }
    }
}
