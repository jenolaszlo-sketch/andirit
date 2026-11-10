using System.Text.Json;
using System.Text.Json.Nodes;
using P1Proof;

// ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01 harness.
// Verbs: oracle <outJson> | publish <db> <outJson> | reopen <db> <outJson> | recovery <db> <outJson>
var action = args[0];
var root = new JsonObject
{
    ["harness"] = "ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01",
    ["action"] = action,
    ["processId"] = Environment.ProcessId,
    ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    ["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
    ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
};
var ok = true;

switch (action)
{
    case "oracle":
    {
        var known = KnownAnswers.Run();
        var forms = await Cases.RunFormsAsync();
        var matrix = await Cases.RunMatrixAsync();
        root["knownAnswers"] = known;
        root["formCases"] = forms;
        root["matrix"] = matrix;
        foreach (var section in new[] { known, forms, matrix })
            foreach (var node in section)
                if (node is JsonObject entry && entry["pass"] is JsonValue value && !value.GetValue<bool>())
                    ok = false;
        root["knownAnswersPass"] = Count(known);
        root["formCasesPass"] = Count(forms);
        root["matrixPass"] = Count(matrix);
        File.WriteAllText(args[1], JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
        break;
    }
    case "publish":
    case "reopen":
    case "recovery":
    {
        var result = await Integration.RunAsync(action, args[1]);
        foreach (var pair in result) root[pair.Key] = JsonNode.Parse(pair.Value?.ToJsonString() ?? "null");
        File.WriteAllText(args[2], JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
        break;
    }
    default:
        throw new ArgumentException("unknown verb: " + action);
}

Console.WriteLine("RESULT " + (ok ? "OK" : "FAIL") + " " + action + " pid=" + Environment.ProcessId);
return ok ? 0 : 1;

static string Count(JsonArray array)
{
    var pass = 0;
    foreach (var node in array)
        if (node is JsonObject entry && entry["pass"] is JsonValue value && value.GetValue<bool>()) pass++;
    return pass + "/" + array.Count;
}
