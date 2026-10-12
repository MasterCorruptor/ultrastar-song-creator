using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace UltraStar.SongCreator.Acquisition;

internal sealed record ProbeOutput(int ExitCode, string StandardOutput, string StandardError);

internal static class ProbeProcess
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static async Task<ProbeOutput> RunAsync(string executable, IEnumerable<string> arguments,
        TimeSpan timeout, int maximumOutputBytes, CancellationToken cancellationToken, string? workingDirectory = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = new CancellationTokenSource(timeout);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = executable,
                WorkingDirectory = workingDirectory is null ? "" : LocalMediaSource.LocalPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        try
        {
            if (!process.Start()) throw new Win32Exception("Could not start media probe.");
        }
        catch (Win32Exception e)
        {
            throw new MediaAcquisitionException(AcquisitionError.ProbeUnavailable, "Configured probe could not be started.", e);
        }

        var exceeded = 0;
        async Task<byte[]> ReadAsync(Stream stream, int limit)
        {
            using var result = new MemoryStream();
            var buffer = new byte[8192];
            try
            {
                int count;
                while ((count = await stream.ReadAsync(buffer, stop.Token)) != 0)
                {
                    if (result.Length + count > limit)
                    {
                        Interlocked.Exchange(ref exceeded, 1);
                        stop.Cancel();
                        throw new MediaAcquisitionException(AcquisitionError.ProbeOutputTooLarge, "Probe output exceeded the configured limit.");
                    }
                    result.Write(buffer, 0, count);
                }
                return result.ToArray();
            }
            catch
            {
                stop.Cancel(); // A failed pipe must not leave other tasks waiting for a hanging child.
                throw;
            }
        }

        process.StandardInput.Close();
        var output = ReadAsync(process.StandardOutput.BaseStream, maximumOutputBytes);
        var errors = ReadAsync(process.StandardError.BaseStream, Math.Min(maximumOutputBytes, 65536));
        var exited = process.WaitForExitAsync(stop.Token);
        try
        {
            await Task.WhenAll(output, errors, exited);
            return new(process.ExitCode, Utf8.GetString(await output), Utf8.GetString(await errors));
        }
        catch (Exception e) when (e is OperationCanceledException or IOException or DecoderFallbackException or MediaAcquisitionException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref exceeded) != 0)
                throw new MediaAcquisitionException(AcquisitionError.ProbeOutputTooLarge, "Probe output exceeded the configured limit.", e);
            if (deadline.IsCancellationRequested)
                throw new MediaAcquisitionException(AcquisitionError.ProbeTimedOut, "Media probe exceeded the configured deadline.", e);
            if (e is DecoderFallbackException)
                throw new MediaAcquisitionException(AcquisitionError.InvalidProbeData, "Probe output is not valid UTF-8.", e);
            throw new MediaAcquisitionException(AcquisitionError.ProbeFailed, "Media probe output could not be read.", e);
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { /* Child exited between observation and kill. */ }
            }
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }
}
