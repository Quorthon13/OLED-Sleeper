namespace OLED_Sleeper.Services
{
    public interface IDimmerService
    {
        void DimMonitor(string hardwareId, int dimLevel);

        void UndimMonitor(string hardwareId);

        void RestoreBrightness(string hardwareId, uint originalBrightness);

        Dictionary<string, uint> GetDimmedMonitors();
    }
}