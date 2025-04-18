﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace KingfoodIO.Common
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<ClockHub, IClock> _clockHub;

        public Worker(ILogger<Worker> logger, IHubContext<ClockHub, IClock> clockHub)
        {
            _logger = logger;
            _clockHub = clockHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {Time}", DateTime.UtcNow);
                await _clockHub.Clients.All.ShowTime(DateTime.UtcNow);
                await Task.Delay(1000);
            }
        }
    }
}
