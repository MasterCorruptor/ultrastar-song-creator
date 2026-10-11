// Only a test child process, never a product/media tool.
using System.Text.Json;

switch (args[0])
{
    case "arguments": Console.Write(JsonSerializer.Serialize(args.Skip(1).ToArray())); break;
    case "failure": Console.Error.Write("Synthetic failure"); return 7;
    case "hang": await Task.Delay(Timeout.Infinite); break;
    case "stdout-limit": Console.Write(new string('x', 2 * 1024 * 1024)); await Task.Delay(Timeout.Infinite); break;
    case "stderr-limit": Console.Error.Write(new string('x', 2 * 1024 * 1024)); await Task.Delay(Timeout.Infinite); break;
    case "invalid-utf8": await Console.OpenStandardOutput().WriteAsync(new byte[] { 0xFF }); break;
    default: return 2;
}
return 0;
