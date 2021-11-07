using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

namespace FPS.Game.Logic.World
{
    public delegate void CallBackFunction(Godot.Node node);

    public class RenderElement
    {
        public string resName;
        public CallBackFunction function;
        public Godot.PackedScene res;
        public RenderElement(string _resName, CallBackFunction _function, Godot.PackedScene _res)
        {
            this.resName = _resName;
            this.function = _function;
            this.res = _res;
        }

    }
    public class PreRenderElement
    {
        public string resName;
        public CallBackFunction function;

        public PreRenderElement(string _resName, CallBackFunction _function)
        {
            this.resName = _resName;
            this.function = _function;
        }

    }
    public partial class ResourceBackgroundLoader : Godot.Node
    {

        public static Queue<RenderElement> renderQueue = new Queue<RenderElement>();
        public static Queue<PreRenderElement> loaderQueue = new Queue<PreRenderElement>();
        public static Dictionary<string, Godot.Node> nodes = new Dictionary<string, Godot.Node>();

        public static Godot.Semaphore waitHandle = new Godot.Semaphore();

        public static PreRenderElement currentLoaderElement = null;

        public void HandleElements()
        {
            if (currentLoaderElement == null)
            {
                if (loaderQueue.Count > 0)
                {
                    var element = loaderQueue.Dequeue();
                    var cached = Godot.ResourceLoader.HasCached(element.resName);


                    /**
                     * Fix a temp problem with godot
                     * https://github.com/godotengine/godot/issues/54151
                     * */

                    Console.WriteLine("[Loader]" + "Start with " + element.resName + " - cached: " + cached);

                    if (cached)
                    {
                        renderQueue.Enqueue(new RenderElement(element.resName, element.function,
                                            Godot.ResourceLoader.Load(element.resName) as Godot.PackedScene));
                    }
                    else
                    {
                        Godot.RenderingServer.RenderLoopEnabled = false;

                        var result = Godot.ResourceLoader.LoadThreadedRequest(element.resName, "", true);

                        if (result != Godot.Error.Ok)
                        {
                            Console.WriteLine("[Loader]" + "Error " + element.resName);
                        }
                        else
                        {
                            Console.WriteLine("[Loader]" + "Loaded " + element.resName);
                            currentLoaderElement = element;
                        }
                    }
                }
            }
            else
            {

                Console.WriteLine("[Loader]" + "Get Status " + currentLoaderElement.resName);

                var status = Godot.ResourceLoader.LoadThreadedGetStatus(currentLoaderElement.resName);
                if (status == Godot.ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    /**
                    * Fix a temp problem with godot
                    * https://github.com/godotengine/godot/issues/54151
                    * */

                    Godot.RenderingServer.RenderLoopEnabled = true;

                    Console.WriteLine("[Loader]" + "Finishing " + currentLoaderElement.resName);

                    var loadedScene = Godot.ResourceLoader.LoadThreadedGet(currentLoaderElement.resName) as Godot.PackedScene;

                    renderQueue.Enqueue(new RenderElement(currentLoaderElement.resName, currentLoaderElement.function, loadedScene));
                    currentLoaderElement = null;
                }
                else if (status != Godot.ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    /**
                    * Fix a temp problem with godot
                    * https://github.com/godotengine/godot/issues/54151
                    * */
                    Console.WriteLine("[Loader]" + "Fails with " + status);
                    Godot.RenderingServer.RenderLoopEnabled = true;
                    currentLoaderElement = null;
                }
                else
                {
                    Console.WriteLine("[Loader]" + "In Progress " + currentLoaderElement.resName + " => " + Godot.RenderingServer.RenderLoopEnabled);
                }
            }
            waitHandle.Post();
        }

        public Godot.Timer timer = new Godot.Timer();

        public override void _Ready()
        {
            Godot.RenderingServer.RenderLoopEnabled = true;
            base._Ready();

            this.AddChild(timer);
            timer.Autostart = true;
            timer.WaitTime = 0.1f;
            timer.Timeout += HandleElements;
            timer.Start();

            //  Godot.RenderingServer.FramePreDraw += HandleElements;
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            loaderIsRunning = false;
            timer.Autostart = false;
            if (loaderThread != null)
                loaderThread.Abort();
        }

        public static Thread loaderThread = null;

        public static void Start()
        {
            loaderThread = new Thread(new ThreadStart(ThreadProc));
            loaderThread.Start();
        }

        public static void Add(string name, CallBackFunction func)
        {
            loaderQueue.Enqueue(new PreRenderElement
            (
                name,
                func
           ));
        }

        public static void handleQueueProcess()
        {
            if (renderQueue.Count > 0)
            {
                var element = renderQueue.Dequeue();
                Console.WriteLine("Start with render new scene of " + element.resName);

                if (nodes.ContainsKey(element.resName))
                {
                    if (element.function != null)
                        element.function(nodes[element.resName]);

                    return;
                }

                Godot.PackedScene resource = element.res;

                if (!resource.CanInstantiate())
                {
                    return;
                }

                Godot.RenderingServer.ForceSync();
                Godot.OS.DelayUsec(16000);

                var obj = resource.Instantiate();
                nodes.Add(element.resName, obj);

                if (element.function != null)
                    element.function(obj);

            }
        }

        public static bool loaderIsRunning = true;

        public static void ThreadProc()
        {
            while (loaderIsRunning)
            {
                waitHandle.Wait();
                Godot.RenderingServer.ForceSync();
                handleQueueProcess();

                Thread.Sleep(100);
            }
        }
    }
}
