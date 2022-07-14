using System.Diagnostics;
using System.Text;

namespace RemoteTerminal.Client;

internal class Terminal : IDisposable
{
    private readonly StreamWriter _standardInput;
    private readonly StreamReader _standardOutput;
    private readonly Process _cmdProcess;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    public Terminal()
    {
        _cmdProcess = new Process();
        _cmdProcess.StartInfo.FileName = "cmd.exe";
        _cmdProcess.StartInfo.CreateNoWindow = true;
        _cmdProcess.StartInfo.UseShellExecute = false;
        _cmdProcess.StartInfo.RedirectStandardOutput = true;
        _cmdProcess.StartInfo.RedirectStandardInput = true;
        _cmdProcess.StartInfo.RedirectStandardError = true;
        _cmdProcess.Start();

        _standardInput = _cmdProcess.StandardInput;
        _standardOutput = _cmdProcess.StandardOutput;

        _processingTask = Process();
    }

    private async Task Process()
    {
        while (!_cts.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await _standardOutput.ReadLineAsync().WaitAsync(_cts.Token);
                
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            NewLine?.Invoke(line);
        }
    }

    public async Task Send(string text)
    {
        await _standardInput.WriteLineAsync(text);
        await _standardInput.FlushAsync();
    }

    private bool _disposed;

    private void Cleanup()
    {
        _cts.Cancel();
        _standardInput.Dispose();
        _cmdProcess.Dispose();
    }

    ~Terminal()
    {
        if (!_disposed)
        {
            Cleanup();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
        Cleanup();
    }

    public event Action<string>? NewLine;
}