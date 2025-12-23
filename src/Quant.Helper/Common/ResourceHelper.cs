using System.IO;
using System.Reflection;

namespace Quant.Helper.Common;

internal static class ResourceHelper
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public static byte[] GetEmbeddedResource(string resourceName)
    {
        string fullResourceName = $"Quant.Helper.Assets.{resourceName}";

        using var stream = _assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static Stream GetEmbeddedResourceStream(string resourceName)
    {
        string fullResourceName = $"Quant.Helper.Assets.{resourceName}";

        var stream = _assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");
        }

        return stream;
    }

    public static bool ResourceExists(string resourceName)
    {
        string fullResourceName = $"Quant.Helper.Assets.{resourceName}";
        return _assembly.GetManifestResourceStream(fullResourceName) != null;
    }
}