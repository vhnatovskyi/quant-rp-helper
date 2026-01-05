using Quant.Helper.Scripts.Abstractions;
using SharpHook.Data;
using WindowsInput;

namespace Quant.Helper.Scripts;

internal class SnowBallScript(InputSimulator simulator) : LoopingScriptBase(KeyCode.VcO, "Сніжки", simulator)
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await PressEKey(2000, token);
            await Task.Delay(500, token);
        }
        await PressEKey(500, default);
    }
}
