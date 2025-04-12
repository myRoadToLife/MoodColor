namespace App.Develop.CommonServices.SceneManagement
{
    public interface IInputSceneArgs
    {
    }

    public class MainSceneInputArgs : IInputSceneArgs
    {
        public int LevelNumber { get; }

        public MainSceneInputArgs(int levelNumber)
        {
            LevelNumber = levelNumber;
        }
    }

    public class PersonalAreaInputArgs : IInputSceneArgs
    {
    }

    public class AuthSceneInputArgs : IInputSceneArgs
    {
    }
}
