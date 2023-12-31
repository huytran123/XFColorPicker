using System;
using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace XFColorPickerControl.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ColorPicker : ContentView
    {
        bool _isUserPick = false;
        /// <summary>
        /// Occurs when the Picked Color changes
        /// </summary>
        public event EventHandler<Color> PickedColorChanged;

        public static BindableProperty PickedColorProperty
            = BindableProperty.Create(
                nameof(PickedColor),
                typeof(Color),
                typeof(ColorPicker),
                Color.Transparent,
                BindingMode.Default,
                propertyChanged: PickedColorPropertyChanged);

        /// <summary>
        /// Get the current Picked Color
        /// </summary>
        public Color PickedColor
        {
            get { return (Color)GetValue(PickedColorProperty); }
            set
            {

                SetValue(PickedColorProperty, value);
               
            }
        }

        private static void PickedColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ColorPicker)bindable).PickedColor = (Color)newValue;
        }

        public static readonly BindableProperty GradientColorStyleProperty
            = BindableProperty.Create(
                nameof(GradientColorStyle),
                typeof(GradientColorStyle),
                typeof(ColorPicker),
                GradientColorStyle.ColorsToDarkStyle,
                BindingMode.OneTime, null);

        /// <summary>
        /// Set the Color Spectrum Gradient Style
        /// </summary>
        public GradientColorStyle GradientColorStyle
        {
            get { return (GradientColorStyle)GetValue(GradientColorStyleProperty); }
            set { SetValue(GradientColorStyleProperty, value); }
        }


        public static readonly BindableProperty ColorListProperty
            = BindableProperty.Create(
                nameof(ColorList),
                typeof(string[]),
                typeof(ColorPicker),
                new string[]
                {
                    new Color(255, 0, 0).ToHex(), // Red
					new Color(255, 255, 0).ToHex(), // Yellow
					new Color(0, 255, 0).ToHex(), // Green (Lime)
					new Color(0, 255, 255).ToHex(), // Aqua
					new Color(0, 0, 255).ToHex(), // Blue
					new Color(255, 0, 255).ToHex(), // Fuchsia
					new Color(255, 255, 255).ToHex(), // Red
				},
                BindingMode.OneTime, null);

        /// <summary>
        /// Sets the Color List
        /// </summary>
        public string[] ColorList
        {
            get { return (string[])GetValue(ColorListProperty); }
            set { SetValue(ColorListProperty, value); }
        }


        public static readonly BindableProperty ColorListDirectionProperty
            = BindableProperty.Create(
                nameof(ColorListDirection),
                typeof(ColorListDirection),
                typeof(ColorPicker),
                ColorListDirection.Horizontal,
                BindingMode.OneTime);

        /// <summary>
        /// Sets the Color List flow Direction
        /// </summary>
        public ColorListDirection ColorListDirection
        {
            get { return (ColorListDirection)GetValue(ColorListDirectionProperty); }
            set { SetValue(ColorListDirectionProperty, value); }
        }


        public static readonly BindableProperty PointerCircleDiameterUnitsProperty
            = BindableProperty.Create(
                nameof(PointerCircleDiameterUnits),
                typeof(double),
                typeof(ColorPicker),
                0.6,
                BindingMode.OneTime);

        /// <summary>
        /// Sets the Picker Pointer Size
        /// Value must be between 0-1
        /// Calculated against the View Canvas size
        /// </summary>
        public double PointerCircleDiameterUnits
        {
            get { return (double)GetValue(PointerCircleDiameterUnitsProperty); }
            set { SetValue(PointerCircleDiameterUnitsProperty, value); }
        }

        public static readonly BindableProperty PointerCircleBorderUnitsProperty
            = BindableProperty.Create(
                nameof(PointerCircleBorderUnits),
                typeof(double),
                typeof(ColorPicker),
                0.3,
                BindingMode.OneTime);

        /// <summary>
        /// Sets the Picker Pointer Border Size
        /// Value must be between 0-1
        /// Calculated against pixel size of Picker Pointer
        /// </summary>
        public double PointerCircleBorderUnits
        {
            get { return (double)GetValue(PointerCircleBorderUnitsProperty); }
            set { SetValue(PointerCircleBorderUnitsProperty, value); }
        }


        private SKPoint _lastTouchPoint = SKPoint.Empty;

        public ColorPicker()
        {
            InitializeComponent();
        }

        private void SkCanvasView_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var skImageInfo = e.Info;
            var skSurface = e.Surface;
            var skCanvas = skSurface.Canvas;

            var skCanvasWidth = skImageInfo.Width;
            var skCanvasHeight = skImageInfo.Height;

            skCanvas.Clear(SKColors.White);

            // Draw gradient rainbow Color spectrum
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;

                System.Collections.Generic.List<SKColor> colors = new System.Collections.Generic.List<SKColor>();
                ColorList.ForEach((color) => { colors.Add(Color.FromHex(color).ToSKColor()); });

                // create the gradient shader between Colors
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    ColorListDirection == ColorListDirection.Horizontal ?
                        new SKPoint(skCanvasWidth, 0) : new SKPoint(0, skCanvasHeight),
                    colors.ToArray(),
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    skCanvas.DrawPaint(paint);
                }
            }

            // Draw darker gradient spectrum
            using (var paint = new SKPaint())
            {
                paint.IsAntialias = true;

                // Initiate the darkened primary color list
                var colors = GetGradientOrder();

                // create the gradient shader 
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    ColorListDirection == ColorListDirection.Horizontal ?
                        new SKPoint(0, skCanvasHeight) : new SKPoint(skCanvasWidth, 0),
                    colors,
                    null,
                    SKShaderTileMode.Clamp))
                {
                    paint.Shader = shader;
                    skCanvas.DrawPaint(paint);
                }
            }

            // Picking the Pixel Color values on the Touch Point

            // Represent the color of the current Touch point
            SKColor touchPointColor;

            // Efficient and fast
            // https://forums.xamarin.com/discussion/92899/read-a-pixel-info-from-a-canvas
            // create the 1x1 bitmap (auto allocates the pixel buffer)
            using (SKBitmap bitmap = new SKBitmap(skImageInfo))
            {
                // get the pixel buffer for the bitmap
                IntPtr dstpixels = bitmap.GetPixels();

                if (_lastTouchPoint == SKPoint.Empty)
                    _lastTouchPoint = FindColor(bitmap, skSurface, skImageInfo, dstpixels);

                // read the surface into the bitmap
                skSurface.ReadPixels(skImageInfo,
                    dstpixels,
                    skImageInfo.RowBytes,
                    (int)_lastTouchPoint.X, (int)_lastTouchPoint.Y);


                // access the color
                touchPointColor = bitmap.GetPixel(0, 0);
            }

            // Painting the Touch point
            using (SKPaint paintTouchPoint = new SKPaint())
            {
                paintTouchPoint.Style = SKPaintStyle.Fill;
                paintTouchPoint.Color = SKColors.White;
                paintTouchPoint.IsAntialias = true;

                var valueToCalcAgainst = (skCanvasWidth > skCanvasHeight) ? skCanvasWidth : skCanvasHeight;

                var pointerCircleDiameterUnits = PointerCircleDiameterUnits; // 0.6 (Default)
                pointerCircleDiameterUnits = (float)pointerCircleDiameterUnits / 10f; //  calculate 1/10th of that value
                var pointerCircleDiameter = (float)(valueToCalcAgainst * pointerCircleDiameterUnits);

                // Outer circle of the Pointer (Ring)
                skCanvas.DrawCircle(
                    _lastTouchPoint.X,
                    _lastTouchPoint.Y,
                    pointerCircleDiameter / 2, paintTouchPoint);

                // Draw another circle with picked color
                paintTouchPoint.Color = touchPointColor;

                var pointerCircleBorderWidthUnits = PointerCircleBorderUnits; // 0.3 (Default)
                var pointerCircleBorderWidth = (float)pointerCircleDiameter *
                                                        (float)pointerCircleBorderWidthUnits; // Calculate against Pointer Circle

                // Inner circle of the Pointer (Ring)
                skCanvas.DrawCircle(
                    _lastTouchPoint.X,
                    _lastTouchPoint.Y,
                    ((pointerCircleDiameter - pointerCircleBorderWidth) / 2), paintTouchPoint);
            }

            // Set selected color
            PickedColor = touchPointColor.ToFormsColor();
            PickedColorChanged?.Invoke(this, PickedColor);
        }

        private void SkCanvasView_OnTouch(object sender, SKTouchEventArgs e)
        {
            _lastTouchPoint = e.Location;

            var canvasSize = SkCanvasView.CanvasSize;

            // Check for each touch point XY position to be inside Canvas
            // Ignore any Touch event ocurred outside the Canvas region 
            if ((e.Location.X > 0 && e.Location.X < canvasSize.Width) &&
                (e.Location.Y > 0 && e.Location.Y < canvasSize.Height))
            {
                e.Handled = true;

                // update the Canvas as you wish
                _isUserPick = true;
                SkCanvasView.InvalidateSurface();
                _isUserPick = false;
            }
        }

        SKPoint FindColor(SKBitmap bitmap, SKSurface skSurface, SKImageInfo skImageInfo, IntPtr dstpixels)
        {
            double distance = 100.0;
            SKPoint sKPointMin = new SKPoint(1, 1);
            var columns = FindColumns(PickedColor);
            var rows = FindRows(PickedColor);
            var startW = (int)(bitmap.Width / ColorList.Length * columns.StartColumn);
            var endW = (int)(bitmap.Width / ColorList.Length * columns.StopColumn);
            var startH = (int)(bitmap.Height / 3 * rows.StartRow);
            var endH = (int)(bitmap.Height / 3 * rows.StopRow);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int y = startH; y < endH; y = y + 1)
            {
                for (int x = startW; x < endW; x = x + 1)
                {
                    // read the surface into the bitmap
                    skSurface.ReadPixels(skImageInfo,
                        bitmap.GetPixels(),
                        skImageInfo.RowBytes,
                        x, y);
                    //Get the color at each pixel
                    var now_color = bitmap.GetPixel(0, 0);
                    //System.Diagnostics.Debug.WriteLine(now_color.ToFormsColor().ToHex());
                    //Compare Pixel's Color ARGB property with the picked color's ARGB property
                    var xColor = now_color.ToFormsColor(); ;
                    var dbl_test_red = Math.Pow(xColor.R * 255 - PickedColor.R * 255, 2.0);
                    var dbl_test_green = Math.Pow(xColor.G * 255 - PickedColor.G * 255, 2.0);
                    var dbl_test_blue = Math.Pow(xColor.B * 255 - PickedColor.B * 255, 2.0);
                    var temp = Math.Sqrt(dbl_test_blue + dbl_test_green + dbl_test_red);

                    if (temp == 0.0)
                    {
                        // the lowest possible distance is - of course - zero
                        // so I can break the loop (thanks to Willie Deutschmann)
                        // here I could return the input_color itself
                        // but in this example I am using a list with named colors
                        // and I want to return the Name-property too
                        stopwatch.Stop();
                        System.Diagnostics.Debug.WriteLine($"Total time:{stopwatch.ElapsedMilliseconds / 1000} ");
                        return new SKPoint(x, y);
                    }
                    else if (temp < distance)
                    {
                        distance = temp;
                        sKPointMin = new SKPoint(x, y);
                    }
                }
            }
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"Total time:{(double)stopwatch.ElapsedMilliseconds / 1000} ");
            System.Diagnostics.Debug.WriteLine($"Color picker:{PickedColor.ToHex()} ");
            return sKPointMin;
        }

        //We divide image to 7 columns base on ColorList.Count
        //and find column contain picker color when initial 
        (double StartColumn, double StopColumn) FindColumns(Color color)
        {
            if (color.R == color.B && color.B == color.G && color.B > 0) return (5.5, 6);
            if (color.R == color.G && color.R > 0) return (0.5, 1.5);
            if (color.G == color.B && color.B > 0) return (2.5, 3.5);
            if (color.R == color.B && color.B > 0) return (4.5, 5.5);

            var max = Math.Max(color.R, Math.Max(color.G, color.B));
            if (max == color.R) return (0, 1);
            if (max == color.G) return (1.5, 2.5);
            if (max == color.B) return (3.5, 4.5);
            return (0, 6);
        }

        (double StartRow, double StopRow) FindRows(Color color)
        {
            if (color.Luminosity >= 0.0 && color.Luminosity <= (double)1 / 3) return (0, 1);
            if (color.Luminosity >= (double)1 / 3 && color.Luminosity <= (double)2 / 3) return (0.5, 1.5);
            return (2, 3);
        }

        private SKColor[] GetGradientOrder()
        {
            if (GradientColorStyle == GradientColorStyle.ColorsOnlyStyle)
            {
                return new SKColor[]
                {
                        SKColors.Transparent
                };
            }
            else if (GradientColorStyle == GradientColorStyle.ColorsToDarkStyle)
            {
                return new SKColor[]
                {
                        SKColors.Transparent,
                        SKColors.Black
                };
            }
            else if (GradientColorStyle == GradientColorStyle.DarkToColorsStyle)
            {
                return new SKColor[]
                {
                        SKColors.Black,
                        SKColors.Transparent
                };
            }
            else if (GradientColorStyle == GradientColorStyle.ColorsToLightStyle)
            {
                return new SKColor[]
                {
                        SKColors.Transparent,
                        SKColors.White
                };
            }
            else if (GradientColorStyle == GradientColorStyle.LightToColorsStyle)
            {
                return new SKColor[]
                {
                        SKColors.White,
                        SKColors.Transparent
                };
            }
            else if (GradientColorStyle == GradientColorStyle.LightToColorsToDarkStyle)
            {
                return new SKColor[]
                {
                        SKColors.White,
                        SKColors.Transparent,
                        SKColors.Black
                };
            }
            else if (GradientColorStyle == GradientColorStyle.DarkToColorsToLightStyle)
            {
                return new SKColor[]
                {
                        SKColors.Black,
                        SKColors.Transparent,
                        SKColors.White
                };
            }
            else
            {
                return new SKColor[]
                {
                    SKColors.Transparent,
                    SKColors.Black
                };
            }
        }
    }

    public enum GradientColorStyle
    {
        ColorsOnlyStyle,
        ColorsToDarkStyle,
        DarkToColorsStyle,
        ColorsToLightStyle,
        LightToColorsStyle,
        LightToColorsToDarkStyle,
        DarkToColorsToLightStyle
    }

    public enum ColorListDirection
    {
        Horizontal,
        Vertical
    }
}
