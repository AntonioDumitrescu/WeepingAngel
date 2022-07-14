﻿using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RemoteDesktop.Messages;
using Yggdrasil.Api.Server;

namespace RemoteDesktop.Client;

internal sealed class VideoStream
{
    private const int CaptureOutputQueueSize = 5;
    private const int EncodeOutputQueueSize = 5;

    private readonly ILogger<VideoStream> _logger;
    private readonly IRemoteClient _client;
    private readonly IFrameSource _frameSource;
    private readonly IEncoder _encoder;
    private readonly CancellationTokenSource _cts = new();

    private readonly Channel<(BitmapPool.BitmapProvider image, DateTime time)> _captureOutput =
        Channel.CreateBounded<(BitmapPool.BitmapProvider image, DateTime time)>(
            new BoundedChannelOptions(CaptureOutputQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });

    private readonly Channel<(byte[][] nals, DateTime sourceTime)> _encodingOutput =
        Channel.CreateBounded<(byte[][] nals, DateTime sourceTime)>(
            new BoundedChannelOptions(EncodeOutputQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

    private Task? _task;

    public VideoStream(ILogger<VideoStream> logger, IRemoteClient client, IFrameSource frameSource, IEncoder encoder)
    {
        _logger = logger;
        _client = client;
        _frameSource = frameSource;
        _encoder = encoder;
    }

    public void BeginStreaming()
    {
        if (_task != null) throw new Exception("Already streaming!");

        _logger.LogInformation("Starting video stream.");
        _logger.LogInformation("Creating tasks.");

        var captureTask = Task.Factory.StartNew(CaptureAsync, TaskCreationOptions.LongRunning);
        var encodeTask = Task.Factory.StartNew(EncodeAsync, TaskCreationOptions.LongRunning);
        var streamTask = Task.Factory.StartNew(StreamAsync, TaskCreationOptions.LongRunning);

        _task = Task.WhenAll(captureTask, encodeTask, streamTask);
    }

    private async Task CaptureAsync()
    {
        _logger.LogInformation("Started capture thread.");

        while (!_cts.IsCancellationRequested)
        {
            var frameProvider = _frameSource.GetImage();

            try
            {
                await _captureOutput.Writer.WriteAsync((frameProvider, DateTime.Now), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cancelled capturing.");
                break;
            }
        }

        _logger.LogInformation("Exited capture thread.");
    }

    private async Task EncodeAsync()
    {
        _logger.LogInformation("Started encoder thread.");

        while (!_cts.IsCancellationRequested)
        {
            var (frame, time) = await _captureOutput.Reader.ReadAsync(_cts.Token);
            using (frame)
            {
                if (!_encoder.Encode(frame.Bitmap, out var results))
                {
                    _logger.LogInformation("Skipping frame.");
                    continue;
                }

                try
                {
                    await _encodingOutput.Writer.WriteAsync((results, time), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cancelled encoding.");
                    break;
                }
            }
        }

        _logger.LogInformation("Exited encode thread.");
    }

    private async Task StreamAsync()
    {
        _logger.LogInformation("Started streaming thread.");

        while (!_cts.IsCancellationRequested)
        {
            byte[][] nals;
            DateTime captureTime;
            try
            {
                (nals, captureTime) = await _encodingOutput.Reader.ReadAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cancelled streaming.");
                break;
            }

            _logger.LogInformation("Pipeline overhead: {ms} ms", Math.Round((DateTime.Now - captureTime).TotalMilliseconds, 2));
            
            await _client.Connection.Send(new NalStreamMessage(new List<byte[]>(nals)));
        }

        _logger.LogInformation("Exited streaming thread.");
    }

    public async Task Close()
    {
        if (_task == null) throw new Exception("Not streaming!");

        _logger.LogInformation("Closing stream.");
        _cts.Cancel();

        await _task!;

        _logger.LogInformation("Closed all streaming threads.");
    }
}