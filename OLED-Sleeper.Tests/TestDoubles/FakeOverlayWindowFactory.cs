using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IOverlayWindowFactory"/> that hands out <see cref="FakeOverlayWindow"/> instances
    /// and keeps every one it created.
    /// </summary>
    public class FakeOverlayWindowFactory : IOverlayWindowFactory
    {
        /// <summary>
        /// The handle the first overlay reports when the caller does not pick one. Any non-zero value
        /// works; zero is the "no handle" sentinel and would leave every overlay untracked.
        /// </summary>
        private const int DefaultFirstHandle = 1000;

        private nint _nextHandle;

        public FakeOverlayWindowFactory() : this(DefaultFirstHandle)
        {
        }

        /// <param name="firstHandle">The handle the first overlay reports once shown; later overlays
        /// count up from it. Zero makes every overlay report no handle at all.</param>
        public FakeOverlayWindowFactory(nint firstHandle)
        {
            _nextHandle = firstHandle;
        }

        /// <summary>
        /// Every overlay created so far, in creation order.
        /// </summary>
        public List<FakeOverlayWindow> Created { get; } = new();

        /// <inheritdoc />
        public IOverlayWindow Create()
        {
            var overlay = new FakeOverlayWindow(_nextHandle);

            // A factory built with zero keeps handing out zero instead of moving off the sentinel.
            if (_nextHandle != nint.Zero)
            {
                _nextHandle++;
            }

            Created.Add(overlay);
            return overlay;
        }
    }
}
