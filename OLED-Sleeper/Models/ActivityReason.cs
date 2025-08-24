using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLED_Sleeper.Models
{
    /// <summary>
    /// Defines the specific reason why a monitor is considered active.
    /// </summary>
    public enum ActivityReason
    { None, MousePosition, ActiveWindow, SystemInput }
}