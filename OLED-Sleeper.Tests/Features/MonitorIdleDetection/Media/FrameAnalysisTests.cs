using OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers;

namespace OLED_Sleeper.Tests.Features.MonitorIdleDetection.Media
{
    public class FrameAnalysisTests
    {
        private const int Cells = 64 * 36;

        [Fact]
        public void ChangedFraction_ForIdenticalFrames_IsZero()
        {
            Assert.Equal(0, FrameDifference.ChangedFraction(Frame(100), Frame(100)));
        }

        [Fact]
        public void ChangedFraction_WhenEveryCellChangesALot_IsOne()
        {
            Assert.Equal(1, FrameDifference.ChangedFraction(Frame(0), Frame(200)));
        }

        [Fact]
        public void ChangedFraction_WhenOneCellChanges_IsOneOverTheCellCount()
        {
            // Arrange
            var current = Frame(0);
            current[0] = current[1] = current[2] = 255;

            // Act
            var fraction = FrameDifference.ChangedFraction(Frame(0), current);

            // Assert
            Assert.Equal(1.0 / Cells, fraction, 6);
        }

        [Fact]
        public void ChangedFraction_ForChangesBelowTheThreshold_IsZero()
        {
            Assert.Equal(0, FrameDifference.ChangedFraction(Frame(100), Frame(105)));
        }

        [Fact]
        public void ChangedFraction_ForFramesOfDifferentSizesOrEmptyFrames_IsZero()
        {
            Assert.Equal(0, FrameDifference.ChangedFraction(new byte[16], new byte[32]));
            Assert.Equal(0, FrameDifference.ChangedFraction(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void MotionHistory_WithFewerThanTheRequiredMovingSamples_IsNotMoving()
        {
            // Arrange
            var history = new MotionHistory();

            // Act
            history.Add(0.5);
            history.Add(0.5);

            // Assert
            Assert.False(history.IsMoving);
        }

        [Fact]
        public void MotionHistory_WithThreeMovingSamplesOutOfFive_IsMoving()
        {
            // Arrange
            var history = new MotionHistory();

            // Act
            foreach (var fraction in new[] { 0.5, 0, 0.5, 0, 0.5 }) history.Add(fraction);

            // Assert
            Assert.True(history.IsMoving);
        }

        [Fact]
        public void MotionHistory_ForSamplesBelowTheThreshold_IsNotMoving()
        {
            // Arrange
            var history = new MotionHistory();

            // Act
            for (int i = 0; i < 5; i++) history.Add(MotionHistory.MovingFractionThreshold / 2);

            // Assert
            Assert.False(history.IsMoving);
        }

        [Fact]
        public void MotionHistory_ForgetsSamplesOlderThanTheWindow()
        {
            // Arrange
            var history = new MotionHistory();
            for (int i = 0; i < 3; i++) history.Add(0.5);

            // Act
            for (int i = 0; i < MotionHistory.WindowSize; i++) history.Add(0);

            // Assert
            Assert.False(history.IsMoving);
        }

        /// <summary>
        /// Builds a 64x36 BGRA frame with every channel of every pixel set to the same value.
        /// </summary>
        private static byte[] Frame(byte value)
        {
            var frame = new byte[Cells * 4];
            Array.Fill(frame, value);
            return frame;
        }
    }
}
