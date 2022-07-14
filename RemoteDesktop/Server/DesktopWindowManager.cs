using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yggdrasil.Api.Events.System;

namespace RemoteDesktop.Server
{
    internal sealed class DesktopWindowManager : IEventReceiver
    {
        private readonly ILogger<DesktopWindowManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DesktopWindowManager(
            ILogger<DesktopWindowManager> logger, 
            IServiceProvider serviceProvider,
            IEventManager eventManager)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            eventManager.AddReceiver(this);
        }

        
    }
}
