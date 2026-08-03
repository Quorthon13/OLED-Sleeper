using OLED_Sleeper.Features.MonitorDimming.Helpers;

namespace OLED_Sleeper.Tests.Features.MonitorDimming.Helpers
{
    public class BrightnessScaleTests
    {
        [Theory]
        [InlineData(0, 0u)]
        [InlineData(15, 38u)]
        [InlineData(80, 204u)]
        [InlineData(100, 255u)]
        public void ToRawBrightness_WhenMonitorHasItsOwnRange_ScalesThePercentageOntoIt(int dimLevelPercentage, uint expected)
        {
            // Act
            var raw = BrightnessScale.ToRawBrightness(dimLevelPercentage, 255);

            // Assert
            Assert.Equal(expected, raw);
        }

        [Fact]
        public void ToRawBrightness_WhenMonitorReportedNoRange_ReturnsThePercentageUnchanged()
        {
            // Act
            var raw = BrightnessScale.ToRawBrightness(15, 0);

            // Assert
            Assert.Equal(15u, raw);
        }

        [Fact]
        public void ToRawBrightness_WhenMonitorRunsOnThePercentageScale_ReturnsThePercentageUnchanged()
        {
            // Act
            var raw = BrightnessScale.ToRawBrightness(15, 100);

            // Assert
            Assert.Equal(15u, raw);
        }

        [Fact]
        public void ToRawBrightness_WhenTheScaledValueLandsOnAHalf_RoundsAwayFromZero()
        {
            // Act
            var raw = BrightnessScale.ToRawBrightness(50, 255);

            // Assert
            Assert.Equal(128u, raw);
        }

        [Theory]
        [InlineData(-10, 0u)]
        [InlineData(150, 255u)]
        public void ToRawBrightness_WhenTheDimLevelIsOutsideThePercentageScale_ClampsIt(int dimLevelPercentage, uint expected)
        {
            // Act
            var raw = BrightnessScale.ToRawBrightness(dimLevelPercentage, 255);

            // Assert
            Assert.Equal(expected, raw);
        }
    }
}
