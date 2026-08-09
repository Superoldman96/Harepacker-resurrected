using HaCreator.WorldMap;
using MapleLib.WzLib.WzProperties;

namespace UnitTest_WorldMapEditor;

public sealed class WorldMapValidationTests
{
    [Fact]
    public void Validate_AcceptsCanonicalFixtureAndKnownMarkerType()
    {
        WorldMapDocument root = WorldMapCodec.Read(WorldMapFixtureFactory.CreateSurface());
        WorldMapDocument child = WorldMapCodec.Read(WorldMapFixtureFactory.CreateChildSurface());
        var hierarchy = new WorldMapHierarchyIndex(new[] { root, child });
        var markerRoot = new WzSubProperty("mapImage");
        markerRoot.AddProperty(WorldMapFixtureFactory.CreateCanvas("29", 16, 16));
        WorldMapMarkerRegistry registry = WorldMapMarkerRegistry.FromProperty(markerRoot);

        WorldMapValidationResult result = WorldMapValidator.Validate(root, hierarchy, registry);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReportsMissingRequiredDataAndBadMapIdsWithoutRepairingDocument()
    {
        WorldMapDocument document = WorldMapDocument.CreateNew("Malformed", logicalName: "");
        WorldMapMapEntry entry = document.Surface.AddEntry("7");
        entry.Type = 999;
        entry.MapIds.Add(0);
        entry.MapIds.Add(0);
        WorldMapLink link = document.Surface.AddLink("3");
        WorldMapFogLayer fog = document.Surface.AddFogLayer("9");

        WorldMapValidationResult result = WorldMapValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Path == "info/WorldMap");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message.Contains("outside the native 9-digit range"));
        Assert.Contains(result.Warnings, diagnostic => diagnostic.Message.Contains("Duplicate map ID"));
        Assert.Contains(result.Warnings, diagnostic => diagnostic.Path == "MapLink/3/link/linkMap");
        Assert.Contains(result.Warnings, diagnostic => diagnostic.Path == "Fog/9/0");
        Assert.NotNull(document.Surface.BaseImage);
        Assert.Equal(640, document.Surface.BaseImage!.Width);
        Assert.Equal(2, entry.MapIds.Count);
        Assert.Null(link.LinkMap);
        Assert.Null(fog.Image);
    }

    [Fact]
    public void ValidateAll_ReportsMissingParentAndNavigationTargets()
    {
        WorldMapDocument document = WorldMapCodec.Read(WorldMapFixtureFactory.CreateSurface());
        document.Surface.ParentName = "MissingParent";
        document.Surface.Links[0].LinkMap = "MissingTarget";

        WorldMapValidationResult result = WorldMapValidator.ValidateAll(new[] { document });

        Assert.Contains(result.Warnings, diagnostic => diagnostic.Path == "info/parentMap");
        Assert.Contains(result.Warnings, diagnostic => diagnostic.Path == "MapLink/3/link/linkMap");
    }
}
