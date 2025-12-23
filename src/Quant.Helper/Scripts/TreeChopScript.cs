using Quant.Helper.Scripts.Abstractions;
using SharpHook.Data;
using WindowsInput;

namespace Quant.Helper.Scripts;

internal class TreeChopScript(InputSimulator simulator) : LoopingScriptBase(KeyCode.VcT, "Лісоруб", simulator)
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await PressEKey(2500, token);
        await Task.Delay(500, token);
        for (int i = 0; i < 20 && !token.IsCancellationRequested; ++i)
        {
            simulator.Mouse.LeftButtonDown();
            await Task.Delay(100, token);
            simulator.Mouse.LeftButtonUp();
            await Task.Delay(700, token);
        }
    }
}
