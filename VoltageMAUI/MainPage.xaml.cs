using System.Numerics;
using System;
using Microsoft.Maui.Dispatching;
namespace VoltageMAUI
{
    public partial class MainPage : ContentPage
    {

        decimal effect, effect_kw, pulseFrequency;
        public IDispatcherTimer ledTimer;
        bool ledState = false;

        int meterPrefix = 1_000; // Default to Kilo
        decimal primCurrent;
        decimal primVoltage;
        class UnitOption //Unit options 
        {
            public string Name { get; set; }
            public int Value { get; set; } //Värdet för enheten 10000 10000000 etc

            public string ActiveSymbol { get; set; } //För enheter som kW mW etc
            public string ApparentSymbol { get; set; } //För enheter som kVA MVA GVAetc
            public string ReactiveSymbol { get; set; } //För enheter som kVAr MVAr GVAr etc
            public string EnergySymbol { get; set; } //För enheter som kWh MWh GWh etc
            public override string ToString() => Name;
        }
        public MainPage()
        {
            InitializeComponent();

            var units = new List<UnitOption>() //Creating different options
            {
                new UnitOption { Name = "Kilo",Value = 1_000, ActiveSymbol = "kW", ApparentSymbol = "kVA", ReactiveSymbol = "kvar", EnergySymbol ="kWh" },
                new UnitOption { Name = "Mega",Value = 1_000_000, ActiveSymbol = "MW", ApparentSymbol = "MVA", ReactiveSymbol = "mvar", EnergySymbol = "MWh"},
                new UnitOption { Name = "Giga", Value = 1_000_000_000, ActiveSymbol = "GW", ApparentSymbol = "GVA", ReactiveSymbol ="gvar", EnergySymbol = "GWh" },
            };

            UnitPicker.ItemsSource = units;
            UnitPicker.SelectedIndex = 0;
        }

        decimal SquareRootOfThree()
        {
            return (decimal)Math.Sqrt(3);
        }

        void UpdateValuesReactive(object sender, EventArgs e)
        {
            GetCurrent();
            
            GetCounter();
           


            GetSecondaryValues(primVoltage, primCurrent);
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
            if (UnitPicker.SelectedItem is UnitOption unit)
            {
                //V Current
                primCurrent = decimal.TryParse(PrimaryCurrent.Text, out primCurrent) ? primCurrent : 0;

                //A Voltage
                primVoltage = decimal.TryParse(PrimaryVoltage.Text, out primVoltage) ? primVoltage : 0;

                //Cos phi 
                decimal cosPhi = decimal.TryParse(Cos.Text, out decimal cosVal) ? cosVal : 1;


                cosPhi = Math.Clamp(cosPhi, 0, 1); //Clamp between 0-1

                //W to kW
                //1.73 is for 3 phase
                //Effect P with Cos phi in kW
                effect_kw = ((primVoltage * primCurrent) * SquareRootOfThree() * cosPhi) / 1000m;

                //Print with 2 decimals
                Effect.Text = (effect_kw / (unit.Value / 1000m)).ToString("0.##") + unit.ActiveSymbol;
            }
        }

        void GetFrequency()
        {
            if (UnitPicker.SelectedItem is UnitOption unit)
            {
                //Puls constant
                decimal pulseConstant = decimal.TryParse(PulsConstant.Text, out pulseConstant) ? pulseConstant : 0;

                //Puls frequency imp/kWh
                pulseFrequency = (effect_kw * pulseConstant) / 3600m;
                if(pulseFrequency>10 || pulseFrequency < 2)
                {
                    HzWarning.TextColor = Colors.Yellow;
                    HzWarning.Text = "Rekommenderad pulfrekvens är runt 8-10";
                }
                else
                {
                    HzWarning.Text = "-";
                }

                CalcPuls.Text = pulseFrequency.ToString("0.####");
                StartLEDPulse();
            }
        }

        private void StartLEDPulse()
        {
            //Min and max frequency
            if (pulseFrequency < 0.5m) pulseFrequency = 0.5m;
            if (pulseFrequency > 100m) pulseFrequency = 100m;

            // Halfperiod in ms for on and off (LED ON or OFF)
            // Lower frequency = longer interval
            int intervalMs = (int)(1000m / (2m * pulseFrequency));
            Millis.Text = "Ms: " + intervalMs.ToString();

            // Start timer, Lambda function into the timer
            // Lambda here is an inline function with no name, it returns true to keep the timer going
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(intervalMs), () =>
            {
                ledState = !ledState;
                PulsIndicator.Color = ledState ? Colors.Red : Colors.Gray;

                return true; // true = fortsätt ticka
            });
        }





        private void UnitPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            meterPrefix = ((UnitOption)UnitPicker.SelectedItem).Value;
            UpdateValuesReactive(sender, e);


        }

        void GetCounter()
        {

            if (UnitPicker.SelectedItem is UnitOption unit)
            {
                //Big maximal reading
                BigInteger meterReading;

                if (!BigInteger.TryParse(MeterMax.Text, out meterReading))
                {
                    CalcCounter.Text = "-";
                    return;
                }


                //Yearly consumption
                decimal yearlyCounter_kWh = effect_kw * 24 * 365; //kWh/år

                //Years until meter is full
                if(meterReading>0 && yearlyCounter_kWh > 0)
                {
                    decimal counter = ((decimal)meterReading / yearlyCounter_kWh);
                    decimal yearsUntilFull = counter;
                    decimal hoursUntilFull = yearsUntilFull * 365 * 24;
                    if (hoursUntilFull < 4000)
                    {
                        WarningCounter.Text = "Omslagstid för mätare under 4000 timmar";
                        WarningCounter.TextColor = Colors.Yellow;
                    }
                    else
                    {
                        WarningCounter.Text = "-";
                    }
                        //Display years with 7 decimals
                        CalcCounter.Text = $"{yearsUntilFull:0.##} år ({hoursUntilFull.ToString("0.##")} timmar)";
                }
                else
                {
                    //Empty
                    CalcCounter.Text = "-";
                }
               

              
            }

        }

        private void PrimaryCurrent_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        //U for Voltage, I for Current, cosPhi for Power factor, hours for time in hours
        void GetSecondaryValues(decimal U, decimal I)
        {

            if(!(UnitPicker.SelectedItem is UnitOption unit))
            {
                return;
            }

            //Try and get cos phi, if it dosen't work set to 1
            decimal cosPhi = decimal.TryParse(Cos.Text, out decimal cosVal) ? cosVal : 1;

            cosPhi = Math.Clamp(cosPhi, 0, 1); //Clamp between 0-1

            //Standerdise to 1h if not set
            decimal hours = decimal.TryParse(InputH.Text, out decimal hVal) ? hVal : 1;

            //Active effect in kW
            decimal P = effect_kw;

            //S is cosPhi if its not 0, then its P/S which gives us CosPhi, if its null somehow set it to 0
            decimal S = cosPhi != 0 ? P / cosPhi : 0;

            //Reactive effect in kVAr based on kW
            decimal Q = (decimal)Math.Sqrt(Math.Max(0.0, (double)(S * S - P * P))); //kVAr or reactive effect

            decimal energy = P * hours; //kWh as a base


            EffectH.Text = (energy / (unit.Value/1000m)).ToString("0.##") + unit.EnergySymbol;
            ApparentPower.Text = (S / (unit.Value / 1000m)).ToString("0.##") + unit.ApparentSymbol;
            ReactivePower.Text = (Q / (unit.Value / 1000m)).ToString("0.##") + unit.ReactiveSymbol;
            Effect.Text = (P / (unit.Value / 1000m)).ToString("0.##") + unit.ActiveSymbol;



            /*
            SecoundValues.Text =
                $"Aktiv effekt {P:0.##} kw\n" +
                $"Skeneffekt {S:0.##} kVA\n" +
                $"Reaktiv effekt {Q:0.##} kVAr\n" +
                $"Energi {energy:0.##} kWh";*/




        }
    }

}
