using SharpHook.Data;
using WindowsInput;

namespace Quant.Helper.Scripts.Abstractions;

public abstract class LoopingScriptBase(KeyCode selectorKey, string name, InputSimulator input) : IScript
{
    private readonly object _lock = new();

    public event Action<bool>? OnRunningEvent;
    private CancellationTokenSource? _cts;
    private Task? _task;

    public KeyCode SelectorKey => selectorKey;

    public string Name => name;

    private bool IsRunning => _cts != null
                && !_cts.IsCancellationRequested
                && _task != null
                && !(_task.IsCompleted || _task.IsCanceled || _task.IsFaulted);

    public async Task ExecuteAsync()
    {
        lock (_lock)
        {
            if (IsRunning)
            {
                _cts?.Cancel();
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
        OnRunningEvent?.Invoke(false);
    }

    private async Task Process(CancellationToken token)
    {
        try
        {
            OnRunningEvent?.Invoke(true);
            await ExecuteAsync(token);
        }
        finally
        {
            OnRunningEvent?.Invoke(false);
        }
    }

    protected abstract Task ExecuteAsync(CancellationToken token);

    protected async Task PressEKey(int durationMs, CancellationToken token)
    {
        input.Keyboard.KeyDown(VirtualKeyCode.VK_E);
        await Task.Delay(durationMs, token);
        input.Keyboard.KeyUp(VirtualKeyCode.VK_E);
    }
}
