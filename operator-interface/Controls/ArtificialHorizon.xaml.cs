using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RovOperatorInterface.Controls
{
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ArtificialHorizon.Mode && targetType == typeof(Visibility) && Enum.TryParse(parameter.ToString(), out ArtificialHorizon.Mode VisibleMode))
            {
                return (ArtificialHorizon.Mode)value == VisibleMode ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class ArtificialHorizon : UserControl
    {
        public enum Mode
        {
            FirstPerson,
            Transverse
        }

        public static DependencyProperty ViewModeProperty = DependencyProperty.Register(nameof(ViewMode), typeof(Mode), typeof(ArtificialHorizon), new PropertyMetadata(Mode.FirstPerson, (sourceObject, e) => (sourceObject as ArtificialHorizon).UpdateAll()));
        public static DependencyProperty RollProperty = DependencyProperty.Register(nameof(Roll), typeof(double), typeof(ArtificialHorizon), new PropertyMetadata(0d, (sourceObject, e) => (sourceObject as ArtificialHorizon).UpdateOrientation()));
        public static DependencyProperty PitchProperty = DependencyProperty.Register(nameof(Pitch), typeof(double), typeof(ArtificialHorizon), new PropertyMetadata(0d, (sourceObject, e) => (sourceObject as ArtificialHorizon).UpdateOrientation()));
        public static DependencyProperty ViewingDistanceProperty = DependencyProperty.Register(nameof(ViewingDistance), typeof(double), typeof(ArtificialHorizon), new PropertyMetadata(1d, (sourceObject, e) => (sourceObject as ArtificialHorizon).UpdateAll()));
        public static DependencyProperty LevelLineMaxDeflectionProperty = DependencyProperty.Register(nameof(LevelLineMaxDeflection), typeof(double), typeof(ArtificialHorizon), new PropertyMetadata(30d, (sourceObject, e) => (sourceObject as ArtificialHorizon).RebuildLevelLines()));
        public static DependencyProperty LevelLineIntervalProperty = DependencyProperty.Register(nameof(LevelLineInterval), typeof(double), typeof(ArtificialHorizon), new PropertyMetadata(10d, (sourceObject, e) => (sourceObject as ArtificialHorizon).RebuildLevelLines()));

        public Mode ViewMode
        {
            get => ((Mode)(base.GetValue(ViewModeProperty)));
            set => base.SetValue(ViewModeProperty, value);
        }

        public double Roll
        {
            get => ((double)(base.GetValue(RollProperty)));
            set => base.SetValue(RollProperty, value);
        }


        public double Pitch
        {
            get => ((double)(base.GetValue(PitchProperty)));
            set => base.SetValue(PitchProperty, value);
        }

        public double ViewingDistance
        {
            get => ((double)(base.GetValue(ViewingDistanceProperty)));
            set => base.SetValue(ViewingDistanceProperty, value);
        }

        public double LevelLineMaxDeflection
        {
            get => ((double)(base.GetValue(LevelLineMaxDeflectionProperty)));
            set => base.SetValue(LevelLineMaxDeflectionProperty, value);
        }

        public double LevelLineInterval
        {
            get => ((double)(base.GetValue(LevelLineIntervalProperty)));
            set => base.SetValue(LevelLineIntervalProperty, value);
        }

        public ArtificialHorizon()
        {
            this.InitializeComponent();
            MainGrid.DataContext = this;

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAll();
        }

        private void UpdateAll()
        {
            RebuildLevelLines();
            UpdateOrientation();
        }

        private Line GenerateLevelLine(double angle)
        {
            double YPosition = (1 + Math.Tan(angle * Math.PI / 180) * ViewingDistance) * LevelLineContainer.ActualHeight / 2;
            double HalfWidth = Math.Tan(Math.Abs(angle) * Math.PI / 180) * ViewingDistance * LevelLineContainer.ActualWidth / 2;
            return new Line()
            {
                X1 = MainGrid.ActualWidth / 2 - HalfWidth,
                X2 = MainGrid.ActualWidth / 2 + HalfWidth,
                Y1 = YPosition,
                Y2 = YPosition,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Windows.UI.Colors.White)
            };
        }

        private void RebuildLevelLines()
        {
            LevelLineContainer.Children.Clear();

            if (ViewMode != Mode.FirstPerson)
                return;

            for (double Angle = 0; Angle <= LevelLineMaxDeflection; Angle += LevelLineInterval)
            {
                LevelLineContainer.Children.Add(GenerateLevelLine(Angle));
                LevelLineContainer.Children.Add(GenerateLevelLine(-Angle));
            }
        }

        private void UpdateOrientation()
        {
            double HorizontalRotation = ViewMode == Mode.FirstPerson ? Roll : Pitch;
            double VerticalRotation = ViewMode == Mode.FirstPerson ? Pitch : 0;

            (GroundRelativeContent.RenderTransform as CompositeTransform).Rotation = HorizontalRotation;
            double VisualizationRadius = MainGrid.ActualHeight / 2;
            double GroundYOffset = Math.Tan(VerticalRotation * Math.PI / 180) * ViewingDistance * VisualizationRadius;
            GroundYOffset = Math.Min(GroundYOffset, VisualizationRadius);
            double GroundXOffset = Math.Sqrt(Math.Pow(VisualizationRadius, 2) - GroundYOffset * GroundYOffset);

            GroundPathFigure.StartPoint = new Point(VisualizationRadius - GroundXOffset, VisualizationRadius + GroundYOffset);
            GroundArc.Point = new Point(VisualizationRadius + GroundXOffset, VisualizationRadius + GroundYOffset);
            GroundArc.IsLargeArc = GroundYOffset < 0;

            // If the X offset is close to zero, the arc becomes mathematically
            // unstable because arc would need to have the same start and end
            // points, which makes its shape undefined. This masks the whole
            // circle in that case.
            BlackoutEllipse.Visibility = GroundXOffset > 1e-5 || GroundYOffset > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
