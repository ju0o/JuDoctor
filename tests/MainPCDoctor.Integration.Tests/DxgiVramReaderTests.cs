using MainPCDoctor.Platform.Windows.Collectors;
using Xunit;

namespace MainPCDoctor.Integration.Tests;

/// <summary>
/// Unit tests for the DXGI adapter-selection and byte-conversion helpers inside DxgiVramReader.
/// These methods accept primitive inputs so no real GPU or COM factory is required.
/// </summary>
public class DxgiVramReaderTests
{
    private const ulong Gb6  = 6UL  * 1024 * 1024 * 1024;
    private const ulong Gb8  = 8UL  * 1024 * 1024 * 1024;
    private const ulong Gb16 = 16UL * 1024 * 1024 * 1024;

    private const int NvidiaVendorId = 0x10DE;
    private const int AmdVendorId    = 0x1002;
    private const int MsBasicVendor  = 0x1414;  // Microsoft Basic Render Driver
    private const int SoftwareFlag   = 0x2;      // AdapterFlags.Software

    // ─── IsPhysicalGpu ────────────────────────────────────────────────────────

    [Fact]
    public void IsPhysicalGpu_ReturnsFalse_ForSoftwareAdapter()
    {
        // AdapterFlags.Software set — this is a software renderer, not real GPU
        Assert.False(DxgiVramReader.IsPhysicalGpu(NvidiaVendorId, SoftwareFlag, Gb6));
    }

    [Fact]
    public void IsPhysicalGpu_ReturnsFalse_ForMicrosoftBasicRenderDriver()
    {
        // VendorId 0x1414 = Microsoft — Basic Render Driver fallback, never real GPU
        Assert.False(DxgiVramReader.IsPhysicalGpu(MsBasicVendor, adapterFlags: 0, dedicatedVideoMemory: 0));
    }

    [Fact]
    public void IsPhysicalGpu_ReturnsFalse_WhenZeroDedicatedVideoMemory()
    {
        // iGPU using shared system memory only — no dedicated VRAM reported
        Assert.False(DxgiVramReader.IsPhysicalGpu(NvidiaVendorId, adapterFlags: 0, dedicatedVideoMemory: 0));
    }

    [Fact]
    public void IsPhysicalGpu_ReturnsTrue_ForNvidiaGpuWith6Gb()
    {
        // RTX 3050 class — 6 GB dedicated, real vendor id, no software flag
        Assert.True(DxgiVramReader.IsPhysicalGpu(NvidiaVendorId, adapterFlags: 0, dedicatedVideoMemory: Gb6));
    }

    [Fact]
    public void IsPhysicalGpu_ReturnsTrue_ForAmdGpuWith8Gb()
    {
        Assert.True(DxgiVramReader.IsPhysicalGpu(AmdVendorId, adapterFlags: 0, dedicatedVideoMemory: Gb8));
    }

    [Fact]
    public void IsPhysicalGpu_ReturnsFalse_WhenSoftwareFlagAndZeroVram()
    {
        // Both software flag and zero vram — should reject
        Assert.False(DxgiVramReader.IsPhysicalGpu(NvidiaVendorId, SoftwareFlag, dedicatedVideoMemory: 0));
    }

    // ─── BytesToGb ────────────────────────────────────────────────────────────

    [Fact]
    public void BytesToGb_ConvertsCorrectly_For6Gb()
    {
        Assert.Equal(6f, DxgiVramReader.BytesToGb(Gb6), precision: 2);
    }

    [Fact]
    public void BytesToGb_ConvertsCorrectly_For8Gb()
    {
        Assert.Equal(8f, DxgiVramReader.BytesToGb(Gb8), precision: 2);
    }

    [Fact]
    public void BytesToGb_ConvertsCorrectly_For16Gb()
    {
        Assert.Equal(16f, DxgiVramReader.BytesToGb(Gb16), precision: 2);
    }

    [Fact]
    public void BytesToGb_ReturnsZero_ForZeroBytes()
    {
        Assert.Equal(0f, DxgiVramReader.BytesToGb(0), precision: 2);
    }
}
