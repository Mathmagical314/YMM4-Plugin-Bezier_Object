using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Curver
{
    public class VelocityKeyframe : INotifyPropertyChanged
    {
        private double time;
        private double speed;
        private double handleOut;
        private double handleIn;

        public event PropertyChangedEventHandler? PropertyChanged;
        public double Time
        {
            get => time;
            set
            {
                if (Math.Abs(time - value) > 0.0001)
                {
                    time = Math.Clamp(value, 0.0, 1.0);
                    OnPropertyChanged();
                }
            }
        }

        public double Speed
        {
            get => speed;
            set
            {
                if (Math.Abs(speed - value) > 0.01)
                {
                    speed = Math.Clamp(value, 0.0, 500.0);
                    OnPropertyChanged();
                }
            }
        }


        public double HandleOut
        {
            get => handleOut;
            set
            {
                if (Math.Abs(handleOut - value) > 0.01)
                {
                    handleOut = value;
                    OnPropertyChanged();
                }
            }
        }


        public double HandleIn
        {
            get => handleIn;
            set
            {
                if (Math.Abs(handleIn - value) > 0.01)
                {
                    handleIn = value;
                    OnPropertyChanged();
                }
            }
        }

        public VelocityKeyframe()
        {
            time = 0.0;
            speed = 100.0;
            handleOut = 0.0;
            handleIn = 0.0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
