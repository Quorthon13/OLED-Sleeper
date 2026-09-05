namespace OLED_Sleeper.Features.MonitorInformation.Models
{
    /// <summary>
    /// What a single DDC/CI probe of a monitor reported.
    /// </summary>
    /// <param name="IsSupported">Whether the monitor answered a DDC/CI capabilities request.</param>
    /// <param name="MaxBrightness">
    /// The highest value the monitor accepts for the brightness VCP code. Zero when the monitor did not report a range.
    /// </param>
    public readonly record struct DdcCiCapabilities(bool IsSupported, uint MaxBrightness);
}
