using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Yggdrasil.Api;
using Yggdrasil.Api.Events.Server.Gui;
using Yggdrasil.Api.Events.Server.Gui.Render;
using Yggdrasil.Api.Server;
using Yggdrasil.Events;

namespace Merlin.Gui;

internal sealed class MainWindow : IHostedService
{
    private readonly ILogger<MainWindow> _logger;
    private readonly EventManager _eventManager;
    private readonly ClientManager _clientManager;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private Sdl2Window _window;
    private GraphicsDevice _graphicsDevice;
    private Task? _renderTask;

    public MainWindow(ILogger<MainWindow> logger, EventManager eventManager, ClientManager clientManager, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _eventManager = eventManager;
        _clientManager = clientManager;
        _applicationLifetime = applicationLifetime;
    }

    private Task CreateRenderTask()
    {
        _logger.LogInformation("Creating render task.");
        return Task.Factory.StartNew(Render, TaskCreationOptions.LongRunning);
    }

    private async Task Render()
    {
        _logger.LogInformation("Creating window and graphics device.");

        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(
                50,
                50,
                1280,
                720,
                WindowState.Normal,
                "Merlin"),

            new GraphicsDeviceOptions(
                true,
                null,
                true,
                ResourceBindingModel.Improved,
                true,
                true),
            out _window,
            out _graphicsDevice);

        _logger.LogInformation("Using {backend}, API version {version}, device name: {name}, vendor {vendor}!",
            _graphicsDevice.BackendType,
            _graphicsDevice.ApiVersion,
            _graphicsDevice.DeviceName,
            _graphicsDevice.VendorName);

        _window.Resized += () =>
        {
            _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
        };

        _logger.LogInformation("Creating command list.");
          
        var commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        _logger.LogInformation("Creating ImGui renderer.");

        var controller = new ImGuiController(_graphicsDevice, _window, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
        _logger.LogInformation("Configuring ImGui.");
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _window.Resized += () =>
        {
            controller.WindowResized(_window.Width, _window.Height);
        };

        ImGui.SetNextWindowSize(new Vector2(100, 100));
        ImGui.SetNextWindowPos(Vector2.Zero);

        _logger.LogInformation("Starting render loop...");
            
        // ui state:
        var showGuiStatistics = false;
        var showLoggingWindow = true;
        var showImGuiMetrics = false;

        var totalFrames = 0L;

        // used to calculate avg FPS
        var startTime = DateTime.Now;

        // used to calculate delta time
        var deltaStart = DateTime.Now;

        var profiler = new Profiler();

        var pluginClientRenderPoints = new DataPointList<float>(1000);
        var frameTimePoints = new DataPointList<float>(1000);
        var pluginViewportRenderPoints = new DataPointList<float>(1000);

        var logText = "";

        var currentClient = 0;
        while (_window.Exists)
        {
            totalFrames++;

            profiler.PushSection("Pump Events");
            var snapshot = _window.PumpEvents();
            var pumpEventsTicks = profiler.PopSectionRemove().ElapsedTicks;

            profiler.PushSection("Pump Events (plugins)");
            await _eventManager.SendAsync(new InputPumpEvent(snapshot));
            var pumpEventsPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;

            if (!_window.Exists)
            {
                break;
            }

            controller.Update((float)(DateTime.Now - deltaStart).TotalSeconds, snapshot);
            deltaStart = DateTime.Now;

            #region Submit UI

            ImGui.DockSpaceOverViewport();
            var mainMenuRenderPluginsTicks = 0L;
            var profilingMenuRenderPluginsTicks = 0L;
            var loggingMenuRenderPluginsTicks = 0L;

            if (ImGui.IsKeyDown((int)Key.AltLeft))
            {
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.SmallButton("Exit"))
                    {
                        _window.Close();
                    }

                    if (ImGui.BeginMenu("Profiling"))
                    {
                        ImGui.Checkbox("GUI Time", ref showGuiStatistics);
                        ImGui.Checkbox("ImGui Metrics", ref showImGuiMetrics);
                        profiler.PushSection("Profiling Menu");
                        await _eventManager.SendAsync(new ProfilingMenuRenderEvent());
                        profilingMenuRenderPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Logging"))
                    {
                        ImGui.Checkbox("Show Logs", ref showLoggingWindow);

                        profiler.PushSection("Logging Menu");
                        await _eventManager.SendAsync(new LoggingMenuRenderEvent());
                        loggingMenuRenderPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;

                        ImGui.EndMenu();
                    }

                    profiler.PushSection("Main Menu");
                    await _eventManager.SendAsync(new MainMenuRenderEvent());
                    mainMenuRenderPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;

                    ImGui.EndMainMenuBar();
                }

            }

            var clients = _clientManager.Clients.ToArray();
            var labels = clients.Select(x => x.AuthenticationInformation.UserAccount).ToArray();

            ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Client List"))
            {
                ImGui.ListBox("Clients", ref currentClient, labels, labels.Length);

                profiler.PushSection("Client List (plugins)");
                await _eventManager.SendAsync(new ClientListWindowRenderEvent(clients.Select(x => (IRemoteClient)x).ToArray(), clients.Length > currentClient ? clients[currentClient] : null));
            }

            var clientListWindowRenderPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;
            pluginClientRenderPoints.AddPoint((float)Math.Round(clientListWindowRenderPluginsTicks / 10000f, 2));

            ImGui.End();

            profiler.PushSection("Plugin Render Viewport");
            await _eventManager.SendAsync(new ViewportRenderEvent());
            var viewportRenderPluginsTicks = profiler.PopSectionRemove().ElapsedTicks;
            pluginViewportRenderPoints.AddPoint((float)Math.Round(viewportRenderPluginsTicks / 10000f, 2));

            if (showGuiStatistics)
            {
                if (ImGui.Begin("FPS / FT"))
                {
                    ImGui.Text($"FPS: {ImGui.GetIO().Framerate:0.##}");
                    ImGui.Text($"Frame Time : {1000f / ImGui.GetIO().Framerate:0.##}");
                    frameTimePoints.AddPoint((float)Math.Round(1000f / ImGui.GetIO().Framerate, 2));

                    ImGui.TreePush();
                    {
                        ImGui.PlotLines("Graph", ref frameTimePoints.AsArray()[0], frameTimePoints.Count - 10);
                    }
                    ImGui.TreePop();

                    ImGui.Text($"Rendered frames: {totalFrames}");
                    ImGui.Text($"Average FPS: {totalFrames / (DateTime.Now - startTime).TotalSeconds:0.##}");
                    ImGui.Text("UI Operations");
                    ImGui.TreePush("UI Operations");
                    {
                        ImGui.Text($"Client render (plugins): {clientListWindowRenderPluginsTicks / 10000f:0.##} ms");
                        ImGui.TreePush();
                        {
                            ImGui.PlotLines("Graph", ref pluginClientRenderPoints.AsArray()[0], pluginClientRenderPoints.Count - 10);
                        }
                        ImGui.TreePop();

                        ImGui.Text($"Viewport render (plugins): {viewportRenderPluginsTicks / 10000f:0.##}");
                        ImGui.TreePush();
                        {
                            ImGui.PlotLines("Graph", ref pluginViewportRenderPoints.AsArray()[0], pluginViewportRenderPoints.Count - 10);
                        }
                        ImGui.TreePop();

                        ImGui.Text($"Pump events: {pumpEventsTicks / 10000f:0.##} ms");
                        ImGui.Text($"Pump events (plugins): {pumpEventsPluginsTicks / 10000f:0.##} ms");
                        ImGui.Text($"Main menu (plugins): {mainMenuRenderPluginsTicks / 10000f:0.##} ms");
                        ImGui.Text($"Profiling menu (plugins): {profilingMenuRenderPluginsTicks / 10000f:0.##} ms");
                        ImGui.Text($"Logging menu (plugins): {loggingMenuRenderPluginsTicks / 10000f:0.##} ms");
                    }
                    ImGui.TreePop();
                }
                ImGui.End();
            }

            if (showLoggingWindow)
            {
                if (ImGui.Begin("Logging"))
                {
                    var needUpdate = LoggingAggregator.Instance.NeedUpdate;
                    var logs = LoggingAggregator.Instance.Gather();

                    if (needUpdate)
                    {
                        logText = string.Join("\r\n", logs);
                    }

                    ImGui.TextUnformatted(logText);

                    if (needUpdate)
                    {
                        ImGui.SetScrollHereY(1);
                    } 
                }
                ImGui.End();
            }

            if (showImGuiMetrics)
            {
                ImGui.ShowMetricsWindow();
            }

            #endregion

            commandList.Begin();
            commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            commandList.ClearColorTarget(0, new RgbaFloat(0, 0, 0, 1f));
            controller.Render(_graphicsDevice, commandList);
            commandList.End();
            _graphicsDevice.SubmitCommands(commandList);
            _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
            controller.SwapExtraWindows(_graphicsDevice);
        }

        _logger.LogInformation("Exited render loop.");

        _applicationLifetime.StopApplication();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting window service.");
        _renderTask = CreateRenderTask();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Waiting to close window.");
        _window.Close();
        await _renderTask!;

        _logger.LogInformation("Window closed.");
    }
}