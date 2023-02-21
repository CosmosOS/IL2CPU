// ReSharper disable InconsistentNaming
namespace IL2CPU.API
{
    /// <summary>
    /// This enum contains the possible target platforms,
    /// to eventually allow for selective inclusion of plugs,
    /// depending on the target platform.
    /// </summary>
    public enum TargetPlatform
    {

        x86,
        x64,
        // IA64, i dont think we will support this
        ARM
    }
}
