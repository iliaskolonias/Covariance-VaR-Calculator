using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using CovarianceVaRCalculator_Base;
using CovarianceVaRCalculator_Engine;
using CovarianceVaRCalculator_MarketData;

namespace CovarianceVaRCalculator_CmdLineTest
{
	class Program
	{
		static void Main(string[] args)
		{
			int verbosityLevel = 2;
			bool allowDebugOutput = true;
			try
			{
				//Basic portfolio parameters - for simplicity in testing, hard-coded
				int numSecurities = 2;		//Number of securities in the VaR model
				int numYearsToGenerate = 2;	//Number of years to generate history for (again, simplicity)

				SecurityDataGenerator sdg = new SecurityDataGenerator();
				SortedDictionary<string, Security> secData = sdg.doGenerateSecurities(numSecurities, DateTime.Today.AddYears(-numYearsToGenerate), DateTime.Today);

				if ((allowDebugOutput) && (2 < verbosityLevel))
				{
					foreach (KeyValuePair<string, Security> sd in secData)
					{
						foreach (var h in sd.Value.secHist.secHistory)
						{
							Console.WriteLine("	{0}, {1:dd/MM/yyyy} -> {2}, {3}, {4}",
								sd.Key, h.Key, h.Value.dataPoint.Item1, h.Value.dataPoint.Item2, h.Value.dataPointSize());
						}
						Console.WriteLine();
					}
				}

				Parametric1DayVaR VaR = new Parametric1DayVaR(allowDebugOutput ? verbosityLevel : 0);
				DateTime start = DateTime.Now;
				BackgroundWorker worker = new BackgroundWorker();
				DoWorkEventArgs e = new DoWorkEventArgs(start);
				VaR.run1DayVar(secData, 0.99, worker, null);
				DateTime end = DateTime.Now;
				if (allowDebugOutput) Console.WriteLine("Time taken: {0}", end - start);
			}
			catch (CovarianceVaRCalculator_Exception e)
			{
				Console.WriteLine("Operation failed! Full errror text: {0}", e.ToString());
			}
			finally
			{
				Console.ReadKey(true);
			}
		}
	}
}
