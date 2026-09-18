using SharpGen.Runtime;
using Vortice.DXGI;

namespace MainPCDoctor.Platform.Windows.Collectors;

/// <summary>
/// Reads dedicated VRAM capacity from the first physical DXGI adapter.
/// The helper methods (IsPhysicalGpu, BytesToGb) accept primitive inputs
/// so they can be unit-tested without a real GPU or COM factory.
/// </summary>
internal static class DxgiVramReader
{
    private const int MicrosoftBasicVendorId = 0x1414;
    private const int SoftwareAdapterFlag    = 0x2;   // AdapterFlags.Software

    /// <summary>
    /// Returns dedicated VRAM capacity in GB for the first physical GPU,
    /// or 0 if DXGI fails or no physical GPU is detected.
    /// </summary>
    public static float ReadDedicatedGb()
    {
        try
        {
            Result r = DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory);
            if (r.Failure || factory is null) return 0f;

            using (factory)
            {
                for (int i = 0; ; i++)
                {
                    Result er = factory.EnumAdapters1(i, out IDXGIAdapter1? adapter);
                    if (er.Failure || adapter is null) break;

                    using (adapter)
                    {
                        AdapterDescription1 desc = adapter.Description1;
                        // PointerSize → IntPtr → long → ulong (safe on 64-bit; VRAM values are positive)
                        ulong dedicatedBytes = (ulong)(long)(IntPtr)desc.DedicatedVideoMemory;
                        if (IsPhysicalGpu(desc.VendorId, (int)desc.Flags, dedicatedBytes))
                            return BytesToGb(dedicatedBytes);
                    }
                }
            }
        }
        catch { }

        return 0f;
    }

    /// <summary>Returns true when the adapter represents a real discrete or integrated GPU.</summary>
    internal static bool IsPhysicalGpu(int vendorId, int adapterFlags, ulong dedicatedVideoMemory)
    {
        if ((adapterFlags & SoftwareAdapterFlag) != 0) return false;  // software renderer
        if (vendorId == MicrosoftBasicVendorId)         return false;  // Microsoft Basic Render Driver
        if (dedicatedVideoMemory == 0)                   return false;  // no dedicated VRAM
        return true;
    }

    /// <summary>Converts a byte count to gigabytes.</summary>
    internal static float BytesToGb(ulong bytes)
        => (float)(bytes / (1024.0 * 1024.0 * 1024.0));
}
