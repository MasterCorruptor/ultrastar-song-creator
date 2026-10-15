// Controlled local terminal fixture only; never a codec, worker or product backend.
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

Console.OutputEncoding = new UTF8Encoding(false);
var mode = args[0];
await using var pipe = new NamedPipeClientStream(".", args[1], PipeDirection.InOut, PipeOptions.Asynchronous);
await pipe.ConnectAsync();
using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
await writer.WriteLineAsync(Environment.ProcessId.ToString());
if (await reader.ReadLineAsync() != "go") return 3;
switch (mode)
{
    case "echo":
        Console.Write(string.Join('\u001f', args.Skip(2)) + "|" + Environment.CurrentDirectory + "|" + Console.In.ReadToEnd().Length);
        break;
    case "bytes":
        var stdout = Console.OpenStandardOutput(); var stderr = Console.OpenStandardError();
        await Task.WhenAll(stdout.WriteAsync(Enumerable.Repeat((byte)0xFF, int.Parse(args[2])).ToArray()).AsTask(),
            stderr.WriteAsync(Enumerable.Repeat((byte)0xFE, int.Parse(args[3])).ToArray()).AsTask());
        break;
    case "nonzero": return 7;
    case "hold": await reader.ReadLineAsync(); break;
    case "spawn":
    case "detached":
        // Windows CreateProcess inherits extra inheritable pipe handles even when stdio is redirected.
        // ShellExecute of this exact owned executable (no command interpreter) starts without that inheritance.
        // This deliberately non-leaf fixture demonstrates a case the production runner cannot detect.
        var detachedWindows = mode == "detached" && OperatingSystem.IsWindows();
        var info = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = detachedWindows,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = mode == "detached" && !detachedWindows,
            RedirectStandardError = mode == "detached" && !detachedWindows
        };
        info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        info.ArgumentList.Add("child"); info.ArgumentList.Add(args[2]);
        using (var child = Process.Start(info)!)
        {
            await writer.WriteLineAsync("spawned");
            await reader.ReadLineAsync();
        }
        break;
    case "child": await reader.ReadLineAsync(); break;
    default: return 2;
}
return 0;
