using System.Text.Json;
using System.Text.Json.Nodes;
using P2Qual;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 reviewer harness.
// Verbs: run <outJson> | integrate <db> <outJson> | reopen <db> <outJson> | recover <db> <outJson>
var action = args[0];
JsonObject result;
switch (action)
{
    case "run":
        result = await Review.RunAsync();
        break;
    case "integrate":
        result = await ReviewIntegration.IntegrateAsync(args[1]);
        break;
    case "reopen":
        result = await ReviewIntegration.ReopenAsync(args[1]);
        break;
    case "recover":
        result = await ReviewIntegration.RecoverAsync(args[1]);
        break;
    default:
        throw new ArgumentException("unknown verb: " + action);
}
File.WriteAllText(args[^1], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
var ok = action == "run" ? (bool)result["allPass"]! : true;
Console.WriteLine("RESULT " + (ok ? "OK" : "FAIL") + " " + action + " pid=" + Environment.ProcessId +
    (action == "run" ? " checks=" + result["checkSummary"] : ""));
return ok ? 0 : 1;
