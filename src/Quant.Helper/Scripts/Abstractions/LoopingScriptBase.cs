using SharpHook.Data;

namespace Quant.Helper.Scripts.Abstractions;

public abstract class LoopingScriptBase(KeyCode selectorKey, string name) : IScript
{
    private readonly object _lock = new();
    public event Action? RunningStateChanged;
    private CancellationTokenSource? _cts;
    private Task? _task;

    public KeyCode SelectorKey => selectorKey;

    public string Name => name;

    public bool IsRunning => _cts != null
                && !_cts.IsCancellationRequested
                && _task != null
                && !(_task.IsCompleted || _task.IsCanceled || _task.IsFaulted);

    public async Task ExecuteAsync()
    {
        lock (_lock)
        {
            if (IsRunning)
            {
                return;
            }
            _cts = new CancellationTokenSource();
            _task = Process(_cts.Token);
        }

        await Task.CompletedTask;
    }


    public async Task StopAsync()
    {
        lock (_lock)
        {
            if (!IsRunning)
                return;
            _cts?.Cancel();
        }

        if (_task != null)
        {
            try
            {
                await _task;
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                lock (_lock)
                {
                    _cts?.Dispose();
                    _cts = null;
                    _task = null;
                }
            }
        }


        RunningStateChanged?.Invoke();
    }

    private async Task Process(CancellationToken token)
    {
        try
        {
            await ExecuteAsync(token);
        }
        finally
        {
            await StopAsync();
        }
    }

    protected abstract Task ExecuteAsync(CancellationToken token);
}
