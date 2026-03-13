using System;
using System.Diagnostics;
using System.Text;

namespace IpBoxKpirImporter.Services;

public static class ProcessRunner
{
    public static string Run(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Nie udało się uruchomić procesu: {fileName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} zakończył się kodem {process.ExitCode}. STDERR: {stderr}");

        return stdout;
    }
}
