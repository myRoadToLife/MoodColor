namespace App.Develop.CommonServices.SceneManagement
{
    public interface IOutputSceneArgs
    {
        IInputSceneArgs NextSceneInputArgs { get; }
    }

    public abstract class OutputSceneArgs : IOutputSceneArgs
    {
        protected OutputSceneArgs(IInputSceneArgs nextSceneInputArgs)
        {
            NextSceneInputArgs = nextSceneInputArgs;
        }

        public IInputSceneArgs NextSceneInputArgs { get; }
    }

    public class OutputMainScreenArgs : OutputSceneArgs
    {
        public OutputMainScreenArgs(IInputSceneArgs nextSceneInputArgs) : base(nextSceneInputArgs)
        {
        }
    }

    public class OutputPersonalAreaScreenArgs : OutputSceneArgs
    {
        public OutputPersonalAreaScreenArgs(IInputSceneArgs nextSceneInputArgs) : base(nextSceneInputArgs)
        {
        }
    }

    public class OutputBootstrapArgs : OutputSceneArgs
    {
        public OutputBootstrapArgs(IInputSceneArgs nextSceneInputArgs) : base(nextSceneInputArgs)
        {
        }
    }
}
