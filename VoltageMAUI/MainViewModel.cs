using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoltageMAUI
{
    internal class MainViewModel : INotifyPropertyChanged
    {

        private decimal primVoltage;
        public decimal PrimVoltage
        {
            get => primVoltage;
            set { primVoltage = value; OnPropertyChanged(); UpdateValues(); }
        }

        private decimal primCurrent;
        public decimal PrimCurrent
        {
            get => primCurrent;
            set { primCurrent = value; OnPropertyChanged(); UpdateValues(); }
        }

        private decimal effect;
        public decimal Effect
        {
            get => effect;
            set { effect = value; OnPropertyChanged(); }
        }

        private string secondaryValues;
        public string SecondaryValues
        {
            get => secondaryValues;
            set { secondaryValues = value; OnPropertyChanged(); }
        }

        //Handles when a property is changed
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void UpdateValues()
        {
            //W to kW
            //1.73 is for 3 phase
            Effect = ((PrimVoltage * PrimCurrent) * 1.73m) / 1_000;

            decimal P = Effect;
            decimal S = primVoltage * primCurrent * 1.73m / 1_000;

            decimal Q = (decimal)Math.Sqrt(Math.Max(0.0, (double)(S * S - P * P))); //kVAr or reactive effect

            decimal energyPerDay = Effect * 24; //kWh per day

            SecondaryValues =
                 $"Aktiv effekt {P:0.##} kw\n" +
                 $"Skeneffekt {S:0.##} kVA\n" +
                 $"Reaktiv effekt {Q:0.##} kVAr\n" +
                 $"Energi (24h) {energyPerDay:0.##} kWh";
        }



    }
}
