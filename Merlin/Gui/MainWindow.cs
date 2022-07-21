using System.Numerics;
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

internal sealed class MainWindow : IServerWindow, IHostedService
{
    private readonly ILogger<MainWindow> _logger;
    private readonly EventManager _eventManager;
    private readonly ClientManager _clientManager;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private Sdl2Window? _window;
    private GraphicsDevice? _graphicsDevice;
    private Task? _renderTask;
    private ImGuiController? _controller;

    public GraphicsDevice GraphicsDevice
    {
        get
        {
            if (_graphicsDevice == null)
            {
                throw new NullReferenceException("Graphics device not initialized!");
            }

            return _graphicsDevice;
        }
    }

    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if (_controller == null)
        {
            throw new Exception("Controller not initialized!");
        }

        return _controller.GetOrCreateImGuiBinding(factory, texture);
    }

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
        _controller = controller;
        
        _logger.LogInformation("Configuring ImGui.");
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _window.Resized += () =>
        {
            controller.WindowResized(_window.Width, _window.Height);
        };

        ImGui.SetNextWindowSize(new Vector2(100, 100));
        ImGui.SetNextWindowPos(Vector2.Zero);

        _logger.LogInformation("Starting render loop...");
            
        var showLoggingWindow = true;

        // used to calculate delta time
        var deltaStart = DateTime.Now;

        var logText = "";

        var currentClient = 0;

        while (_window.Exists)
        {
            var snapshot = _window.PumpEvents();
            await _eventManager.SendAsync(new InputPumpEvent(snapshot));

            if (!_window.Exists)
            {
                break;
            }

            controller.Update((float)(DateTime.Now - deltaStart).TotalSeconds, snapshot);
            deltaStart = DateTime.Now;

            #region Submit UI
            
            ImGui.DockSpaceOverViewport();

            var clients = _clientManager.Clients.ToArray();
            var labels = clients.Select(x => x.AuthenticationInformation.UserAccount).ToArray();

            ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);
          
            if (ImGui.Begin("Client List"))
            {
                ImGui.ListBox("Clients", ref currentClient, labels, labels.Length);

                await _eventManager.SendAsync(new ClientListWindowRenderEvent(clients.Select(x => (IRemoteClient)x).ToArray(), clients.Length > currentClient ? clients[currentClient] : null));
            }

            ImGui.End();

            await _eventManager.SendAsync(new ViewportRenderEvent());

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
        _window!.Close();
        await _renderTask!;

        _logger.LogInformation("Window closed.");
    }
}