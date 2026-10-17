namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers
{
    /// <summary>
    /// Keeps the most recent frame comparisons for a monitor and decides whether they add up to sustained motion.
    /// </summary>
    public class MotionHistory
    {
        /// <summary>
        /// How many of the latest samples are retained. At one sample a second this is about five seconds.
        /// </summary>
        public const int WindowSize = 5;

        /// <summary>
        /// How many retained samples must be moving for the monitor to count as moving.
        /// </summary>
        public const int RequiredMovingSamples = 3;

        /// <summary>
        /// The fraction of changed cells that makes one sample a moving one.
        /// </summary>
        public const double MovingFractionThreshold = 0.01;

        private readonly Queue<double> _samples = new();

        /// <summary>
        /// Gets whether enough of the retained samples were moving.
        /// </summary>
        public bool IsMoving => _samples.Count(fraction => fraction >= MovingFractionThreshold) >= RequiredMovingSamples;

        /// <summary>
        /// Adds a sample, dropping the oldest when the window is full.
        /// </summary>
        /// <param name="changedFraction">The fraction of changed cells from <see cref="FrameDifference"/>.</param>
        public void Add(double changedFraction)
        {
            _samples.Enqueue(changedFraction);
            if (_samples.Count > WindowSize)
            {
                _samples.Dequeue();
            }
        }
    }
}
