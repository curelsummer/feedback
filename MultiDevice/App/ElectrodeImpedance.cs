using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MultiDevice
{
    public class ElectrodeImpedance : INotifyPropertyChanged
    {
        public ElectrodeImpedance() { }
        public ElectrodeImpedance(string serNB,string status, string channel, string impedance)
        {
            this.serialNb = serNB;
            this.status = status;
            this.channel = channel;
            this.impedance = impedance;
        }

        string serialNb;
        public string SerialNb
        {
            get { return this.serialNb; }
            set
            {
                this.serialNb = value;
                OnPropertyChanged("SerialNb");
            }
        }


        string status;
        public string Status
        {
            get { return this.status; }
            set
            {
                this.status = value;
                OnPropertyChanged("Status");
            }
        }

        string channel;
        public string Channel
        {
            get { return this.channel; }
            set
            {
                this.channel = value;
                OnPropertyChanged("Channel");
            }
        }

        string impedance;
        public string Impedance
        {
            get { return this.impedance; }
            set
            {
                this.impedance = value;
                OnPropertyChanged("Impedance");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

    }
}
