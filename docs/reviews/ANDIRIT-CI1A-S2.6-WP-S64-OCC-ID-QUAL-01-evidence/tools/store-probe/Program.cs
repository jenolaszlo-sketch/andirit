using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;
var dbPath = Path.GetFullPath(args[0]);
var mode = args[1];
const string metaLabel = "AndiritOccurrenceMeta";
const string unitLabel = "AndiritOccurrenceUnit";
const string pageLabel = "AndiritOccurrencePage";

static List<(string Key, string Payload)> Rows(string path, string label)
{
    using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
    using var txn = db.BeginReadTransaction();
    using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
    using var r = q.Execute(txn);
    var list = r.ReadAll().Select(row => (row.GetValue(0).AsString(), row.GetValue(1).AsString())).ToList();
    txn.Commit();
    return list;
}
static string Decode(string b64) => Encoding.UTF8.GetString(Convert.FromBase64String(b64));
static string Sha(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

if (mode == "inspect")
{
    var rows = new List<object>();
    foreach (var label in new[] { metaLabel, unitLabel, pageLabel })
        foreach (var (key, payload) in Rows(dbPath, label).OrderBy(x => x.Key, StringComparer.Ordinal))
            rows.Add(new { label, key, payload = JsonNode.Parse(Decode(payload)) });
    var all = string.Join("\n", rows.Select(r => System.Text.Json.JsonSerializer.Serialize(r)));
    var result = new
    {
        logicalSha = Sha(all),
        fileSha = Sha(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(dbPath)))),
        rows,
    };
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
if (mode == "open")
{
    try
    {
        using var store = new LatticeCodeGraphStore(dbPath);
        Console.WriteLine("OPENED");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine("REFUSED " + ex.GetType().FullName);
        Console.WriteLine(ex.Message);
        return 3;
    }
}
if (mode == "tamper")
{
    var label = args[2];
    var key = args[3];
    var set = args[4];
    var value = args[5];
    using var db = LatticeDatabase.Open(dbPath, new LatticeDatabaseOptions { Create = false });
    using var txn = db.BeginWriteTransaction();
    JsonObject row;
    using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k RETURN s.payload"))
    {
        q.Bind("k", LatticeValue.From(key));
        using var r = q.Execute(txn);
        row = JsonNode.Parse(Decode(r.ReadAll()[0].GetValue(0).AsString()))!.AsObject();
    }
    if (set == "Remove") row.Remove(args[5]); else row[set] = value;
    using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
    {
        q.Bind("k", LatticeValue.From(key));
        q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
        using var r = q.Execute(txn);
        r.ReadAll();
    }
    txn.Commit();
    Console.WriteLine("TAMPERED");
    return 0;
}
if (mode == "tamper-meta")
{
    var field = args[2];
    var value = args[3];
    using var db = LatticeDatabase.Open(dbPath, new LatticeDatabaseOptions { Create = false });
    using var txn = db.BeginWriteTransaction();
    var rows = new List<(string Key, JsonObject Row)>();
    using (var q = db.Prepare($"MATCH (s:{metaLabel}) RETURN s.key, s.payload"))
    {
        using var r = q.Execute(txn);
        foreach (var row in r.ReadAll())
            rows.Add((row.GetValue(0).AsString(), JsonNode.Parse(Decode(row.GetValue(1).AsString()))!.AsObject()));
    }
    foreach (var (key, row) in rows)
    {
        if (value == "Remove") row.Remove(field);
        else if (field is "SchemaVersion" or "IdentityGeneration") row[field] = long.Parse(value);
        else row[field] = value;
        using var q = db.Prepare($"MATCH (s:{metaLabel}) WHERE s.key = $k SET s.payload = $p");
        q.Bind("k", LatticeValue.From(key));
        q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
        using var r = q.Execute(txn); r.ReadAll();
    }
    txn.Commit();
    Console.WriteLine("TAMPERED");
    return 0;
}
if (mode == "tamper-all-authority")
{
    var field = args[2];
    var value = args[3];
    using var db = LatticeDatabase.Open(dbPath, new LatticeDatabaseOptions { Create = false });
    using var txn = db.BeginWriteTransaction();
    var rows = new List<(string Key, JsonObject Row)>();
    using (var q = db.Prepare($"MATCH (s:{unitLabel}) RETURN s.key, s.payload"))
    {
        using var r = q.Execute(txn);
        foreach (var row in r.ReadAll())
            rows.Add((row.GetValue(0).AsString(), JsonNode.Parse(Decode(row.GetValue(1).AsString()))!.AsObject()));
    }
    foreach (var (key, row) in rows)
    {
        var authority = row["Authority"]!.AsObject();
        if (value == "Remove") authority.Remove(field);
        else if (field is "Version" or "IdentityGeneration") authority[field] = long.Parse(value);
        else authority[field] = value;
        using var q = db.Prepare($"MATCH (s:{unitLabel}) WHERE s.key = $k SET s.payload = $p");
        q.Bind("k", LatticeValue.From(key));
        q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
        using var r = q.Execute(txn); r.ReadAll();
    }
    txn.Commit();
    Console.WriteLine("TAMPERED");
    return 0;
}
if (mode == "tamper-first-target")
{
    var value = args[2];
    using var db = LatticeDatabase.Open(dbPath, new LatticeDatabaseOptions { Create = false });
    using var txn = db.BeginWriteTransaction();
    var rows = new List<(string Key, JsonObject Row)>();
    using (var q = db.Prepare($"MATCH (s:{unitLabel}) RETURN s.key, s.payload"))
    {
        using var r = q.Execute(txn);
        foreach (var row in r.ReadAll())
            rows.Add((row.GetValue(0).AsString(), JsonNode.Parse(Decode(row.GetValue(1).AsString()))!.AsObject()));
    }
    var mutated = false;
    foreach (var (key, row) in rows)
    {
        if (mutated) break;
        var facts = row["Facts"]!.AsArray();
        if (facts.Count == 0) continue;
        facts[0]!["TargetId"]!["Value"] = value;
        mutated = true;
        using var q = db.Prepare($"MATCH (s:{unitLabel}) WHERE s.key = $k SET s.payload = $p");
        q.Bind("k", LatticeValue.From(key));
        q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
        using var r = q.Execute(txn); r.ReadAll();
    }
    txn.Commit();
    Console.WriteLine(mutated ? "TAMPERED" : "NO_FACTS");
    return 0;
}

Console.Error.WriteLine("usage: inspect|open|tamper <db> ...");
return 2;

