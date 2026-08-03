using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IOverlayWindow"/> that records what it was asked to do instead of creating a window.
    /// Its handle drops back to zero on <see cref="Close"/>, the same as a real overlay.
    /// </summary>
    public class FakeOverlayWindow : IOverlayWindow
    {
        private readonly nint _handleOnceShown;

        /// <param name="handleOnceShown">The handle to report after <see cref="Show"/> runs. Zero
        /// reproduces an overlay that never got a window handle.</param>
        public FakeOverlayWindow(nint handleOnceShown)
        {
            _handleOnceShown = handleOnceShown;
        }

        /// <summary>
        /// The bounds passed to <see cref="Show"/>, or null while the overlay has not been shown.
        /// </summary>
        public Rect? ShownBounds { get; private set; }

        /// <summary>
        /// True once <see cref="Close"/> has run.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <inheritdoc />
        public nint Handle { get; private set; }

        /// <inheritdoc />
        public void Show(Rect bounds)
        {
            ShownBounds = bounds;
            Handle = _handleOnceShown;
        }

        /// <inheritdoc />
        public void Close()
        {
            IsClosed = true;
            Handle = nint.Zero;
        }
    }
}
