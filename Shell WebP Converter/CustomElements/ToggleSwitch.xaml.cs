using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Shell_WebP_Converter.CustomElements
{
    /// <summary>
    /// Interaction logic for ToggleSwitch.xaml
    /// </summary>
    public partial class ToggleSwitch : UserControl
    {
        public static readonly DependencyProperty IsThreePositionProperty =
            DependencyProperty.Register("IsThreePosition", typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(true, OnConfigurationChanged));

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TogglePosition), typeof(ToggleSwitch), new PropertyMetadata(TogglePosition.Left, OnPositionChanged));

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.LightGray));

        public static readonly DependencyProperty ThumbColorProperty =
            DependencyProperty.Register("ThumbColor", typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty LeftColorProperty =
            DependencyProperty.Register("LeftColor", typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Blue));

        public static readonly DependencyProperty CenterColorProperty =
            DependencyProperty.Register("CenterColor", typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty RightColorProperty =
            DependencyProperty.Register("RightColor", typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Green));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ToggleSwitch), new PropertyMetadata(new CornerRadius(10)));

        public bool IsThreePosition
        {
            get { return (bool)GetValue(IsThreePositionProperty); }
            set { SetValue(IsThreePositionProperty, value); }
        }

        public enum TogglePosition
        {
            Left,
            Center,
            Right
        }
        /// <summary>
        /// Position of the toggle
        /// </summary>
        public TogglePosition Position
        {
            get { return (TogglePosition)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public Brush ThumbColor
        {
            get { return (Brush)GetValue(ThumbColorProperty); }
            set { SetValue(ThumbColorProperty, value); }
        }

        public Brush LeftColor
        {
            get { return (Brush)GetValue(LeftColorProperty); }
            set { SetValue(LeftColorProperty, value); }
        }

        public Brush CenterColor
        {
            get { return (Brush)GetValue(CenterColorProperty); }
            set { SetValue(CenterColorProperty, value); }
        }

        public Brush RightColor
        {
            get { return (Brush)GetValue(RightColorProperty); }
            set { SetValue(RightColorProperty, value); }
        }

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        private SolidColorBrush? backgroundBrush;

        public ToggleSwitch()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            backgroundBrush = ((this.FindName("BackgroundBrush") as SolidColorBrush));
            UpdateVisuals();

            if (BackgroundBorder != null)
            {
                BackgroundBorder.SizeChanged += BackgroundBorder_SizeChanged;
            }

            MainGrid.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            MainGrid.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.Dispatcher.InvokeAsync(() => SetThumbPositionImmediate(Position));
        }

        private void BackgroundBorder_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateVisuals();
            SetThumbPositionImmediate(Position);
        }

        private static void OnConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ToggleSwitch)d;
            control.UpdateVisuals();
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ToggleSwitch)d;
            control.AnimateToPosition((TogglePosition)e.NewValue);
        }

        private void UpdateVisuals()
        {
            if (MainGrid == null) return;

            MainGrid.ColumnDefinitions.Clear();

            int positions = IsThreePosition ? 3 : 2;

            for (int i = 0; i < positions; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            if (BackgroundBorder != null)
            {
                Grid.SetColumn(BackgroundBorder, 0);
                Grid.SetColumnSpan(BackgroundBorder, positions);
            }

            AnimateToPosition(Position);
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MainGrid == null || BackgroundBorder == null) return;

            if (!IsThreePosition)
            {
                Position = Position == TogglePosition.Left ? TogglePosition.Right : TogglePosition.Left;
                return;
            }

            var position = e.GetPosition(BackgroundBorder);
            var positionsCount = 3;
            var columnWidth = BackgroundBorder.ActualWidth / positionsCount;
            int clickedColumn = (int)(position.X / columnWidth);

            clickedColumn = Math.Max(0, Math.Min(2, clickedColumn));
            TogglePosition newPosition = (TogglePosition)clickedColumn;
            Position = newPosition;
        }

        private void AnimateToPosition(TogglePosition newPosition)
        {
            if (Thumb == null || BackgroundBorder == null || backgroundBrush == null) return;

            double targetLeft;
            Color targetColor = Colors.LightGray;
            int positions = IsThreePosition ? 3 : 2;
            double availableWidth = Math.Max(0, BackgroundBorder.ActualWidth - Thumb.ActualWidth);
            int index;
            switch (newPosition)
            {
                case TogglePosition.Left:
                    index = 0;
                    targetColor = (LeftColor as SolidColorBrush)?.Color ?? Colors.Blue;
                    break;
                case TogglePosition.Center:
                    if (!IsThreePosition) throw new InvalidOperationException("Center position not allowed in two-position mode.");
                    index = 1;
                    targetColor = (CenterColor as SolidColorBrush)?.Color ?? Colors.Gray;
                    break;
                case TogglePosition.Right:
                    index = positions - 1;
                    targetColor = (RightColor as SolidColorBrush)?.Color ?? Colors.Green;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            double segmentCount = positions - 1;
            double step = segmentCount > 0 ? (availableWidth / segmentCount) : 0;
            targetLeft = step * index;

            var positionAnimation = new DoubleAnimation
            {
                To = targetLeft,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Thumb.BeginAnimation(Canvas.LeftProperty, positionAnimation);

            var colorAnimation = new ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            backgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void SetThumbPositionImmediate(TogglePosition position)
        {
            if (Thumb == null || BackgroundBorder == null) return;
            int positions = IsThreePosition ? 3 : 2;
            double availableWidth = Math.Max(0, BackgroundBorder.ActualWidth - Thumb.ActualWidth);
            int index = position == TogglePosition.Left ? 0 : position == TogglePosition.Center ? 1 : positions - 1;
            double segmentCount = positions - 1;
            double step = segmentCount > 0 ? (availableWidth / segmentCount) : 0;
            double left = step * index;
            Canvas.SetLeft(Thumb, left);

            if (backgroundBrush != null)
            {
                var targetColor = (position == TogglePosition.Left) ? (LeftColor as SolidColorBrush)?.Color : (position == TogglePosition.Center ? (CenterColor as SolidColorBrush)?.Color : (RightColor as SolidColorBrush)?.Color);
                backgroundBrush.Color = targetColor ?? backgroundBrush.Color;
            }
        }
    }
}
