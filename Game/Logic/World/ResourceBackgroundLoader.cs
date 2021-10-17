using Godot;

namespace FPS.Game.Logic.World
{
    public partial class ResourceBackgroundLoader
    {
        private string resourceName = null;
        private bool inProgress = false;

        public delegate void LoaderComplete(PackedScene scene);
        public event LoaderComplete OnLoaderComplete;

        public void Load(string resourceName)
        {
            if (this.inProgress)
            {
                GD.PrintErr("In progress");
                return;
            }

            this.inProgress = false;
            this.resourceName = resourceName;

            var result = ResourceLoader.LoadThreadedRequest(this.resourceName, "", true);
            if (result == Error.Ok)
            {
                this.inProgress = true;
            }
        }

        public void Tick()
        {
            if (this.inProgress)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(this.resourceName);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    GD.Print("Loading game level successfull.");

                    var scene = ResourceLoader.LoadThreadedGet(this.resourceName) as PackedScene;
                    GD.Print("[ResourceManager] Load resource completed " + resourceName);
                    OnLoaderComplete(scene);
                    this.inProgress = false;
                }
                else if (status == ResourceLoader.ThreadLoadStatus.InvalidResource || status == ResourceLoader.ThreadLoadStatus.Failed)
                {
                    GD.PrintErr("[ResourceManager]  Cant load resource " + resourceName);
                    this.inProgress = false;
                }
            }
        }
    }
}
