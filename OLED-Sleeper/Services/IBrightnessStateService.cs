using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    public interface IBrightnessStateService
    {
        Dictionary<string, uint> LoadState();

        void SaveState(Dictionary<string, uint> state);
    }
}