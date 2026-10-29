using System.Reflection;

namespace Penghou.Andirit.LatticeDb.Tests;

public sealed class HistoricalStorageIdentityTests
{
    [Fact]
    public void Schema2_LabelsRemainHistoricalHetuProtocolIdentities()
    {
        // These are persisted labels, not unfinished product branding.
        var names = new[] { "Metadata", "Repository", "Run", "IndexState", "Unit", "Stage", "Baseline" };
        foreach (var name in names)
        {
            var field = typeof(LatticeCodeGraphStore).GetField(name + "Label", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Equal("Hetu" + name, field.GetRawConstantValue());
        }
        Assert.Equal(2, LatticeCodeGraphStore.CurrentSchemaVersion);
        Assert.Equal(2, CodePublicationSnapshot.CurrentSchemaVersion);
        Assert.Equal("HetuGraphNode", NativeGraphMirror.NodeLabel);
        Assert.Equal("HetuMirror", NativeGraphMirror.MirrorLabel);
        Assert.Equal("hetu_json", typeof(NativeGraphMirror).GetField("JsonProperty", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue());
    }
}
