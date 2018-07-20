using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading.Tasks;

using CovarianceVaRCalculator_Base;
using CovarianceVaRCalculator_Engine;
using CovarianceVaRCalculator_MarketData;

namespace CovarianceVaRCalculator
{
	class MainWindowViewModel
	{
		private List<VaRResult> data;
		public ICollectionView Securities { get; private set; }

		public MainWindowViewModel()
		{
		}

		public void populate(List<VaRResult> results)
		{
			data = results;
			Securities = CollectionViewSource.GetDefaultView(data);
			Securities.Refresh();
		}

		/*
		public object RunVaR(Tuple<int, double, DateTime, DateTime> parameters, BackgroundWorker worker)
		{
			bool allowDebugOutput = false;
			try
			{
				//Basic portfolio parameters
				int numSecurities = parameters.Item1;			//Number of securities in the VaR model
				double confidenceInterval = parameters.Item2;	//Confidence interval
				DateTime startDate = parameters.Item3;			//Start date for historical data
				DateTime endDate = parameters.Item4;			//End date for historical data
				SecurityDataGenerator sdg = new SecurityDataGenerator();
				SortedDictionary<string, Security> secData = sdg.doGenerateSecurities(numSecurities, startDate, endDate);

				Parametric1DayVaR VaR = new Parametric1DayVaR(0);
				Tuple<double, SortedDictionary<string, Tuple<double, double> > > data = VaR.run1DayVar(secData, confidenceInterval);
				List<VaRResult> result = new List<VaRResult>();
				foreach (KeyValuePair<string, Tuple<double, double> > d in data.Item2)
				{
					VaRResult res = new VaRResult { securityName = d.Key, VaRDisaggregated = d.Value.Item1, VaRStandalone = d.Value.Item2 };
					result.Add(res);
				}
				return new Tuple<double, List<VaRResult> >(data.Item1, result);
			}
			catch (CovarianceVaRCalculator_Exception e)
			{
				if (allowDebugOutput) Console.WriteLine("Operation failed! Full errror text: {0}", e.ToString());
			}
			return new List<VaRResult>();
		}
		*/
	}
}
