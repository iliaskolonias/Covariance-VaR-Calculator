using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CovarianceVaRCalculator_Engine
{
	public class VaRResult : INotifyPropertyChanged
	{
		//KeyValuePair<string, Tuple<double, double>> Value { get; set; }
		private string secName;
		private double VaRDis;
		private double VaRStand;
		public string securityName
		{
			get { return secName; }
			set
			{
				if (value != secName)
				{
					secName = value;
					if (null != PropertyChanged)
					{
						PropertyChanged(this, new PropertyChangedEventArgs("securityName"));
					}
				}
			}
		}
		public double VaRDisaggregated
		{
			get { return VaRDis; }
			set
			{
				if (value != VaRDis)
				{
					VaRDis = value;
					if (null != PropertyChanged)
					{
						PropertyChanged(this, new PropertyChangedEventArgs("VaRDisaggregated"));
					}
				}
			}
		}

		public double VaRStandalone
		{
			get { return VaRStand; }
			set
			{
				if (value != VaRStand)
				{
					VaRStand = value;
					if (null != PropertyChanged)
					{
						PropertyChanged(this, new PropertyChangedEventArgs("VaRStandalone"));
					}
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
