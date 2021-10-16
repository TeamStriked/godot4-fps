using System.Linq;
using Godot;
using System;
using FPS.Game.Logic.Camera;
using FPS.Game.Logic.Level;
using FPS.Game.Logic.Player;
using System.Collections.Generic;
namespace FPS.Game.Logic.World
{
    public partial class Loader : Timer
    {
        static Queue<string> resources = new Queue<string>();
        public static List<string> loadedResources = new List<string>();

        public delegate void ResourceLoaderInfo(int totalFiles, int finishedFiles, string nextFile);
        public static event ResourceLoaderInfo OnResourceLoaderInfo;

        public delegate void ResourceLoaderComplete(string[] resources);
        public static event ResourceLoaderComplete OnResourceLoaderComplete;

        public static int totalRecords = 0;
        public static int loadedRecords = 0;

        public static string currentFile = null;

        public override void _Ready()
        {
            this.Connect("timeout", new Callable(this, "TickResources"));
        }

        public static void LoadResources(string[] extractList)
        {
            resources.Clear();
            currentFile = null;
            totalRecords = extractList.Length;

            foreach (var item in extractList)
            {
                resources.Enqueue(item);
            }
        }

        public void TickResources()
        {
            if (currentFile != null)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(currentFile);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    loadedResources.Add(currentFile);
                    loadedRecords++;
                    currentFile = null;

                    if (resources.Count == 0)
                    {
                        OnResourceLoaderComplete(loadedResources.ToArray());
                    }
                }
                Console.WriteLine(status);
            }
            else if (resources.Count > 0)
            {
                currentFile = resources.Dequeue();
                if (ResourceLoader.HasCached(currentFile))
                {
                    loadedResources.Add(currentFile);
                    OnResourceLoaderInfo(totalRecords, loadedRecords + 1, currentFile);

                    loadedRecords++;
                    currentFile = null;

                    if (resources.Count == 0)
                    {
                        OnResourceLoaderComplete(loadedResources.ToArray());
                    }
                }
                else
                {
                    var result = ResourceLoader.LoadThreadedRequest(currentFile);
                    OnResourceLoaderInfo(totalRecords, loadedRecords + 1, currentFile);
                }
            }
        }
    }

}
