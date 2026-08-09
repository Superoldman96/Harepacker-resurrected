using HaCreator.MapSimulator.UI;
using MapleLib.Img;

namespace UnitTest_MapSimulator;

public sealed class MapSimulatorUiFamilyTests
{
    [Theory]
    [InlineData(true, true, true, MapSimulatorUiFamily.VUpdate)]
    [InlineData(false, true, true, MapSimulatorUiFamily.VUpdate)]
    [InlineData(true, true, false, MapSimulatorUiFamily.BigBang)]
    [InlineData(false, true, false, MapSimulatorUiFamily.BigBang)]
    [InlineData(true, false, false, MapSimulatorUiFamily.LegacyPreBigBang)]
    [InlineData(false, false, false, MapSimulatorUiFamily.LegacyPreBigBang)]
    public void StatusBarOwnerImagesUseNewestAvailableFamily(
        bool hasStatusBar,
        bool hasStatusBar2,
        bool hasStatusBar3,
        MapSimulatorUiFamily expected)
    {
        Assert.Equal(
            expected,
            MapSimulatorUiFamilyResolver.ResolveFromStatusBarImages(
                hasStatusBar,
                hasStatusBar2,
                hasStatusBar3));
    }

    [Fact]
    public void SixtyFourBitVersionAlwaysUsesModernFamily()
    {
        Assert.Equal(
            MapSimulatorUiFamily.VUpdate,
            MapSimulatorUiFamilyResolver.Resolve(
                is64Bit: true,
                isPreBigBang: true,
                hasBigBangMarker: false));
    }

    [Fact]
    public void ExplicitPreBigBangMetadataUsesLegacyFamily()
    {
        Assert.Equal(
            MapSimulatorUiFamily.LegacyPreBigBang,
            MapSimulatorUiFamilyResolver.Resolve(
                is64Bit: false,
                isPreBigBang: true,
                hasBigBangMarker: true));
    }

    [Fact]
    public void MissingBigBangMarkerUsesLegacyFamily()
    {
        Assert.Equal(
            MapSimulatorUiFamily.LegacyPreBigBang,
            MapSimulatorUiFamilyResolver.Resolve(
                is64Bit: false,
                isPreBigBang: false,
                hasBigBangMarker: false));
    }

    [Fact]
    public void BigBangMarkerUsesBigBangFamily()
    {
        Assert.Equal(
            MapSimulatorUiFamily.BigBang,
            MapSimulatorUiFamilyResolver.Resolve(
                is64Bit: false,
                isPreBigBang: false,
                hasBigBangMarker: true));
    }

    [Fact]
    public void VersionInfoOverloadUsesTheSamePrecedence()
    {
        var versionInfo = new VersionInfo
        {
            Is64Bit = false,
            IsPreBB = false
        };

        Assert.Equal(
            MapSimulatorUiFamily.BigBang,
            MapSimulatorUiFamilyResolver.Resolve(versionInfo, hasBigBangMarker: true));

        versionInfo.IsPreBB = true;
        Assert.Equal(
            MapSimulatorUiFamily.LegacyPreBigBang,
            MapSimulatorUiFamilyResolver.Resolve(versionInfo, hasBigBangMarker: true));
    }

    [Fact]
    public void NullVersionInfoAndMissingMarkerRemainLegacy()
    {
        Assert.Equal(
            MapSimulatorUiFamily.LegacyPreBigBang,
            MapSimulatorUiFamilyResolver.Resolve(versionInfo: null, hasBigBangMarker: false));
    }
}
