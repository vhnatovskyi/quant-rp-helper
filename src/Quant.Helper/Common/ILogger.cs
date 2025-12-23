using System.Collections.ObjectModel;

namespace Quant.Helper.Common;

public interface ILogger
{
    ObservableCollection<string> Messages { get; }

    void Log(string message);
}
