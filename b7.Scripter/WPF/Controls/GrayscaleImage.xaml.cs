using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace b7.Scripter.WPF.Controls
{
    public partial class GrayscaleImage : UserControl
    {
        #region - Dependency properties -
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(BitmapSource), typeof(GrayscaleImage),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged))
        );

        public static readonly DependencyProperty IsGrayscaleProperty = DependencyProperty.Register(
            "IsGrayscale", typeof(bool), typeof(GrayscaleImage),
            new PropertyMetadata(true)
        );
        

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((GrayscaleImage)d).OnSourceChanged((BitmapSource)e.NewValue);
        #endregion

        public BitmapSource Source
        {
            get => (BitmapSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public bool IsGrayscale
        {
            get => (bool)GetValue(IsGrayscaleProperty);
            set => SetValue(IsGrayscaleProperty, value);
        }

        public GrayscaleImage()
        {
            InitializeComponent();
        }

        protected void OnSourceChanged(BitmapSource source)
        {
            grayscaleImage.Source = new FormatConvertedBitmap(Source, PixelFormats.Gray8, null, 0.0);
        }
    }
}
