using System.Windows;
using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Core
{
    /// <summary>
    /// Enforces a single running instance of the application.
    /// If a second instance is launched, it signals the first instance to show its main window and exits silently.
    /// </summary>
    public class ApplicationInstanceManager : IApplicationInstanceManager
    {
        #region Constants

        private const string MutexName = "OLED-Sleeper-Mutex";
        private const string EventName = "OLED-Sleeper-ShowWindow";

        #endregion Constants

        #region Fields

        private Mutex? _mutex;
        private EventWaitHandle? _showWindowEvent;
        private Action? _showMainWindowAction;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Indicates whether this is the first instance of the application.
        /// </summary>
        public bool IsFirstInstance { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInstanceManager"/> class.
        /// </summary>
        public ApplicationInstanceManager()
        { }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Initializes the single-instance check.
        /// If another instance is running, signals it to show its main window and exits the current process.
        /// </summary>
        public void Initialize()
        {
            _mutex = new Mutex(true, MutexName, out bool isNewInstance);
            IsFirstInstance = isNewInstance;

            if (!IsFirstInstance)
            {
                SignalFirstInstanceAndExit();
                return;
            }

            CreateEventAndListen();
        }

        /// <summary>
        /// Sets the delegate to show the main window when signaled by a second instance.
        /// Should be called after DI and services are initialized.
        /// </summary>
        /// <param name="showMainWindowAction">Action to show the main window.</param>
        public void SetShowMainWindowAction(Action showMainWindowAction)
        {
            _showMainWindowAction = showMainWindowAction ?? throw new ArgumentNullException(nameof(showMainWindowAction));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Signals the first instance to show its main window and exits the current process.
        /// </summary>
        private void SignalFirstInstanceAndExit()
        {
            try
            {
                _showWindowEvent = EventWaitHandle.OpenExisting(EventName);
                _showWindowEvent.Set();
            }
            catch
            {
                // If first instance hasn't created the event yet, just exit silently
            }
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Creates the event wait handle and starts listening for signals from secondary instances.
        /// </summary>
        private void CreateEventAndListen()
        {
            _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            Task.Run(ListenForShowWindowSignal);
        }

        /// <summary>
        /// Listens for signals from secondary instances to show the main window.
        /// </summary>
        private void ListenForShowWindowSignal()
        {
            if (_showWindowEvent == null) return;

            while (true)
            {
                try
                {
                    _showWindowEvent.WaitOne();
                    if (_showMainWindowAction != null)
                    {
                        Application.Current.Dispatcher.Invoke(_showMainWindowAction);
                    }
                }
                catch
                {
                    // Ignore exceptions, typically happens during shutdown
                    break;
                }
            }
        }

        #endregion Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Releases resources and mutex when the application exits.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _showWindowEvent?.Dispose();
                if (_mutex != null)
                {
                    try
                    {
                        _mutex.ReleaseMutex();
                    }
                    catch
                    {
                        // Ignore if already released
                    }
                    _mutex.Dispose();
                }
            }
        }

        #endregion IDisposable Implementation
    }
}