namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// A <see cref="TimeProvider"/> whose clock moves only when a test says so.
    /// </summary>
    public class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _now;

        /// <summary>
        /// Moves the clock forward.
        /// </summary>
        /// <param name="amount">How far to move it.</param>
        public void Advance(TimeSpan amount) => _now += amount;
    }
}
