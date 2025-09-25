using System.Numerics;
namespace VoltageMAUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        decimal effect, pulseFrequency;
        int meterPrefix = 1_000; // Default to Kilo
        class UnitOption //Unit options 
        {
            public string Name { get; set; }
            public int Value { get; set; } //Värdet för enheten 10000 10000000 etc

            public string Symbol { get; set; } //För enheter som kWh mWh etc
            public override string ToString() => Name;
        }
        public MainPage()
        {
            InitializeComponent();

            var units = new List<UnitOption>() //Creating different options
            {
                new UnitOption { Name = "Kilo", Symbol = "kWh", Value = 1_000 },
                new UnitOption { Name = "Mega",Symbol = "MWh", Value = 1_000_000 },
                new UnitOption { Name = "Giga", Symbol = "GWh", Value = 1_000_000_000 },
            };

            UnitPicker.ItemsSource = units;
            UnitPicker.SelectedIndex = 0;
        }
        private void OnCalculateClicked(object sender, EventArgs e)
        {
            GetCurrent();
        }
        void OnCalculatePulsFreq(object sender, EventArgs e)
        {
            GetFrequency();
        }

        void OnCalculateCounter(object sender, EventArgs e)
        {
            GetCounter();
        }
        void GetCurrent()
        {
            //V Current
            decimal primCurrent = decimal.TryParse(PrimaryCurrent.Text, out primCurrent) ? primCurrent : 0;

            //A Voltage
            decimal primVoltage = decimal.TryParse(PrimaryVoltage.Text, out primVoltage) ? primVoltage : 0;

            //W to kW
            effect = ((primVoltage * primCurrent) * 1.73m)/1000;

            //Print with 2 decimals
            Effect.Text = effect.ToString("0.##");
        }

        void GetFrequency()
        {

            //Puls constant
            decimal pulseConstant = decimal.TryParse(PulsConstant.Text, out pulseConstant) ? pulseConstant : 0;

            //Puls frequency imp/kWh
            pulseFrequency = (effect * pulseConstant) / 3600;

            CalcPuls.Text = pulseFrequency.ToString("0.####");
        }

        private void UnitPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            meterPrefix = ((UnitOption)UnitPicker.SelectedItem).Value;


        }

        void GetCounter()
        {

            if(UnitPicker.SelectedItem is UnitOption unit)
            {
                //Big maximal reading
                BigInteger meterReading = Convert.ToInt32(MeterMax.Text);

                //Yearly consumption
                decimal yearlyCounter = effect * 24 * 365; //kWh/år

                //Years until meter is full
                decimal counter = ((decimal)meterReading / yearlyCounter);
                decimal displayCounter  = counter * (unit.Value/1000);

                //Display years with 7 decimals
                CalcCounter.Text = $"{displayCounter:0.#######} år";
            }
            
        }
    }

}
