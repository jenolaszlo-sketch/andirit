using System.Diagnostics;

// Let the real worker set up D-07's accepted publication. For the hold role,
// exit before staging or signalling READY so D-07 must reject the process.
var mode = Array.IndexOf(args, "--mode");
if (mode >= 0 && mode + 1 < args.Length && args[mode + 1] == "hold")
{
    Console.Error.WriteLine("R2 early-worker control exited before READY.");
    return 19;
}

const string name = "Andirit.LatticeDb.Tests.Worker";
var config = new ProcessStartInfo("dotnet") { UseShellExecute = false };
foreach (var option in new[] { "exec", "--depsfile", name + ".deps.json", "--runtimeconfig",
    name + ".runtimeconfig.json", name + ".real.dll" })
    config.ArgumentList.Add(option);
foreach (var arg in args)
    config.ArgumentList.Add(arg);
config.WorkingDirectory = AppContext.BaseDirectory;
using var process = Process.Start(config)!;
await process.WaitForExitAsync();
return process.ExitCode;
