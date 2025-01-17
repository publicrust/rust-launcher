using Loader;
using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Loader
{
    public partial class MainTab : Window
    {
        public MainTab()
        {
            InitializeComponent();
            StartLoadingAnimation();
            
        }
        
        private void StartLoadingAnimation()
        {
            
            var progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(3) 
            };

            progressAnimation.Completed += (s, e) => OpenMainWindow();
            LoadingBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressAnimation);

            
            var storyboard = new Storyboard();
            storyboard.Children.Add(progressAnimation);
            Storyboard.SetTarget(progressAnimation, LoadingBar);
            Storyboard.SetTargetProperty(progressAnimation, new PropertyPath("Value"));

            storyboard.CurrentTimeInvalidated += (s, e) =>
            {
                var clock = (Clock)s;
                if (clock.CurrentProgress.HasValue)
                {
                    int progress = (int)(clock.CurrentProgress.Value * 100);
                    LoadingPercent.Text = $"{progress}%";
                }
            };

            storyboard.Begin(this);
        }

        private void OpenMainWindow()
        {
            
            if (Application.Current.Windows.Count == 1) 
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show(); 
            }

            this.Close(); 
        }
    }
}
