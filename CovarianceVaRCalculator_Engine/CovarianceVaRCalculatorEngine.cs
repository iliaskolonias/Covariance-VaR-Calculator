using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;

using CovarianceVaRCalculator_Base;
using CovarianceVaRCalculator_MarketData;

namespace CovarianceVaRCalculator_Engine
{
	public partial class CovarianceVaRCalculatorEngine
	{
		int numSecurities;			//Number of securities in portfolio
		double confidenceInterval;	//Confidence interval
		DateTime startDate;			//Start date for historical data
		DateTime endDate;			//End date for historical data
		double calculatedVaR;
		List<VaRResult> result;

		public CovarianceVaRCalculatorEngine()
		{
			numSecurities		= 0;
			confidenceInterval	= 0.99;
			startDate			= DateTime.Today.AddYears(-1);
			endDate				= DateTime.Today;

			calculatedVaR		= -1;
			result				= new List<VaRResult>();
		}

		public object RunVaR(Dictionary<String, Object> parameters, BackgroundWorker worker, DoWorkEventArgs e)
		{
			bool allowDebugOutput = false;
			try
			{
				//Basic portfolio parameters
				if (!parameters.ContainsKey("numSecurities")) throw new CovarianceVaRCalculator_Exception("Number of securities for calculation not specified");
				//numSecurities = parameters.Item1 ?? 1000;						//Number of securities in the VaR model
				numSecurities = ((Int32)parameters["numSecurities"]);			//Number of securities in the VaR model
				if (!parameters.ContainsKey("confidenceInterval")) throw new CovarianceVaRCalculator_Exception("Confidence Interval for calculation not specified");
				//confidenceInterval = parameters.Item2 ?? 0.99;				//Confidence interval
				confidenceInterval = (Double)parameters["confidenceInterval"];	//Confidence interval
				if (!parameters.ContainsKey("startDate")) throw new CovarianceVaRCalculator_Exception("Number of securities for calculation not specified");
				//startDate = parameters.Item3 ?? DateTime.Today.AddYears(-1);	//Start date for historical data
				startDate = (DateTime)parameters["startDate"];					//Start date for historical data
				if (!parameters.ContainsKey("endDate")) throw new CovarianceVaRCalculator_Exception("Number of securities for calculation not specified");
				//endDate = parameters.Item4 ?? DateTime.Today;					//End date for historical data
				endDate = (DateTime)parameters["endDate"];						//End date for historical data

				SecurityDataGenerator sdg = new SecurityDataGenerator();
				SortedDictionary<string, Security> secData = sdg.doGenerateSecurities(numSecurities, startDate, endDate);

				calculatedVaR = -1;
				result.Clear();
				Parametric1DayVaR VaR = new Parametric1DayVaR(0);
				Tuple<double, SortedDictionary<string, Tuple<double, double>>> data = VaR.run1DayVar(secData, confidenceInterval, startDate, endDate, worker, e);
				calculatedVaR = data.Item1;
				foreach (KeyValuePair<string, Tuple<double, double>> d in data.Item2)
				{
					VaRResult res = new VaRResult { securityName = d.Key, VaRDisaggregated = d.Value.Item1, VaRStandalone = d.Value.Item2 };
					result.Add(res);
				}
				return new Tuple<double, List<VaRResult>>(data.Item1, result);
			}
			catch (CovarianceVaRCalculator_Exception ex)
			{
				if (allowDebugOutput) Console.WriteLine("Operation failed! Full errror text: {0}", ex.ToString());
				throw;
			}
		}

	}
}
