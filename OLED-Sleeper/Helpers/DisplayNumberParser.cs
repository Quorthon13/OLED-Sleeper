using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OLED_Sleeper.Helpers
{
    public class DisplayNumberParser
    {
        public static int ParseDisplayNumber(string deviceName)
        {
            var match = Regex.Match(deviceName, @"\d+$");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number;
            }
            return -1;
        }
    }
}