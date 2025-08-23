// File: Helpers/SizeObserver.cs
using System.Windows;

namespace OLED_Sleeper.Helpers
{
    // Refactored: New helper class to remove code-behind from MonitorLayoutView.
    // This attached property allows binding the ActualWidth and ActualHeight of a FrameworkElement
    // to a command in the ViewModel, passing the dimensions as a parameter.
    public static class SizeObserver
    {
        public static readonly DependencyProperty ObserveProperty =
            DependencyProperty.RegisterAttached(
                "Observe",
                typeof(bool),
                typeof(SizeObserver),
                new PropertyMetadata(false, OnObserveChanged));

        public static readonly DependencyProperty ObservedWidthProperty =
            DependencyProperty.RegisterAttached(
                "ObservedWidth",
                typeof(double),
                typeof(SizeObserver),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ObservedHeightProperty =
            DependencyProperty.RegisterAttached(
                "ObservedHeight",
                typeof(double),
                typeof(SizeObserver),
                new PropertyMetadata(0.0));

        public static bool GetObserve(FrameworkElement frameworkElement)
        {
            return (bool)frameworkElement.GetValue(ObserveProperty);
        }

        public static void SetObserve(FrameworkElement frameworkElement, bool value)
        {
            frameworkElement.SetValue(ObserveProperty, value);
        }

        public static double GetObservedWidth(FrameworkElement frameworkElement)
        {
            return (double)frameworkElement.GetValue(ObservedWidthProperty);
        }

        public static void SetObservedWidth(FrameworkElement frameworkElement, double value)
        {
            frameworkElement.SetValue(ObservedWidthProperty, value);
        }

        public static double GetObservedHeight(FrameworkElement frameworkElement)
        {
            return (double)frameworkElement.GetValue(ObservedHeightProperty);
        }

        public static void SetObservedHeight(FrameworkElement frameworkElement, double value)
        {
            frameworkElement.SetValue(ObservedHeightProperty, value);
        }

        private static void OnObserveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement frameworkElement)
            {
                if ((bool)e.NewValue)
                {
                    frameworkElement.SizeChanged += OnSizeChanged;
                }
                else
                {
                    frameworkElement.SizeChanged -= OnSizeChanged;
                }
            }
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                SetObservedWidth(frameworkElement, e.NewSize.Width);
                SetObservedHeight(frameworkElement, e.NewSize.Height);
            }
        }
    }
}