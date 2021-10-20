using System.Threading.Tasks;
using Godot;
using System;
using System.Threading;

namespace FPS.Game.Logic.World
{
    public class ResourceBackgroundLoader
    {
        private string resourceName = null;
        private bool inProgress = false;

        readonly static AutoResetEvent _signal = new AutoResetEvent(true);

        public delegate void LoaderComplete(Node scene);
        public event LoaderComplete OnLoaderComplete;

        public void LoadInstancedScene(string res)
        {
            //Starts a new Task that will NOT block the UI thread.
            Task.Run(() =>
            {
                _signal.WaitOne();

                //This simulates the heavy task.
                var scene = ResourceLoader.Load(res) as PackedScene;
                scene.ResourceLocalToScene = true;
                Console.WriteLine("Start instancing.");
                var obj = scene.Instantiate();
                OnLoaderComplete(obj);
                Console.WriteLine("Instanced thread loaded complete.");
                _signal.Set();
            });
        }

    }
}
