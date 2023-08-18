using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XFColorPickerControl
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {        
        public MainPage()
        {
            InitializeComponent();
            ColorPicker.PickedColor = Color.FromHex("#0c0b36");
        }

        private void ColorPicker_PickedColorChanged(object sender, Color colorPicked)
        {
            //// Use the selected color
            SelectedColorDisplayFrame.BackgroundColor = colorPicked;
            SelectedColorValueLabel.Text = colorPicked.ToHex();
            ColorPickerHolderFrame.BackgroundColor = colorPicked;

            if (colorPicked.Luminosity < 0.5)
                SelectedColorValueLabel.TextColor = Xamarin.Forms.Color.White;
            else
                SelectedColorValueLabel.TextColor = Xamarin.Forms.Color.SlateGray;            
        }

    }
}
