using System.Text.Json;
using P1Delta;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01 harness.
// Verbs: run <outJson>
var action = args[0];
if (action != "run") throw new ArgumentException("unknown verb: " + action);

var result = await Delta.RunAsync();
File.WriteAllText(args[1], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
var ok = (bool)result["allPass"]!;
Console.WriteLine("RESULT " + (ok ? "OK" : "FAIL") + " delta pid=" + Environment.ProcessId + " checks=" + result["checkSummary"]);
return ok ? 0 : 1;
