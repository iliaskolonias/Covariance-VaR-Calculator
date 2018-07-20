using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IliasKoloniasTechnicalTestAberdeenAM_Base;

namespace IliasKoloniasTechnicalTestAberdeenAM_Engine
{
	public partial class Parametric1DayVaR
	{
		//Lambda value for amortisation
		public static double lambda = 0.94;
	
		//For means calculation
		private Matrix countsPrice;
		private Matrix sumsPrice;
		private Matrix meansPrice;
		
		//For weights calculation
		double lastTotalSize;
		double lastTotalSum;
		double lastCalculatedVaR;
		private Matrix sumsSize;
		private Matrix sumsMoney;
		private Matrix weights;

		//For covariance calculations
		private Matrix logReturns;
		private Matrix amortisationRates;
		private Matrix sumProductsPrice;
		private Matrix covarianceMatrix;
		private Matrix volatilityMatrix;

		public void run1DayVar(SortedDictionary<string, Security> secData, double percentile)
		{
			try
			{
				calculateMeans(secData);
				calculateAssetWeights(secData);
				calculateLogReturns(secData);
				calculateAssetVolatility(secData);
				lastCalculatedVaR = doOverallPnLCalculation(percentile);
				Console.WriteLine("Calculated VaR: {0}", lastCalculatedVaR);
				diaggregatePnL(secData);
			}
			catch (Exception e)
			{
				throw new IliasKoloniasTechnicalTestAberdeenAM_Exception(String.Format("Something went wrong with the calculation. Error details: {0}", e.Message), e);
			}
		}

		private void calculateMeans(SortedDictionary<string, Security> secData)
		{
			//Mean prices
			countsPrice = new Matrix(1, secData.Count);
			sumsPrice = new Matrix(1, secData.Count);
			meansPrice = new Matrix(1, secData.Count);
			int i = 0;
			foreach (KeyValuePair<string, Security> sd in secData)
			{
				countsPrice[0, i] = sd.Value.secHist.secHistory.Count;
				sumsPrice[0, i] = sd.Value.secHist.secHistory.Sum(x => x.Value.dataPoint.Item2);
				meansPrice[0, i] = sumsPrice[0, i] / (double)countsPrice[0, i];

				i++;
			}
			
			//Console.WriteLine("Means");
			//for (i = 0; i < secData.Count;i++) Console.WriteLine("{0}", meansPrice[0,i]);
		}

		private void calculateAssetWeights(SortedDictionary<string, Security> secData)
		{
			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = (DateTime.Today - horizonStart).Days;

			//Weights
			weights = new Matrix(1, secData.Count);
			sumsSize = new Matrix(horizon, secData.Count);
			sumsMoney = new Matrix(horizon, secData.Count);
			int i, j;
			i = 0;
			foreach (KeyValuePair<string, Security> sd in secData)
			{
				int dayCount = sd.Value.secHist.secHistory.Count;
				j = 0;
				foreach (KeyValuePair<DateTime, SecHistData> shd in sd.Value.secHist.secHistory)
				{
					if (horizonStart <= shd.Key)
					{
						sumsSize[j, i] += (double)shd.Value.dataPoint.Item1;
						sumsMoney[j, i] += (double)shd.Value.dataPoint.Item1 * shd.Value.dataPoint.Item2;
						j++;
					}
				}
				i++;
			}
			lastTotalSize = 0.0;
			lastTotalSum = 0.0;
			for (i = 0; i < sumsSize.NColumns; i++) lastTotalSize += sumsSize[sumsSize.NRows - 1, i];
			for (i = 0; i < sumsMoney.NColumns; i++) lastTotalSum += sumsMoney[sumsMoney.NRows - 1, i];
			for (i = 0; i < sumsMoney.NColumns; i++) weights[0, i] = sumsMoney[sumsMoney.NRows - 1, i] / lastTotalSum;

			//Console.WriteLine("Weights");
			//for (i = 0; i < secData.Count; i++) Console.WriteLine("{0}", weights[0, i]);
		}

		private void calculateLogReturns(SortedDictionary<string, Security> secData)
		{
			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = (DateTime.Today - horizonStart).Days;

			//Weights
			amortisationRates = new Matrix(horizon, 1);
			logReturns = new Matrix(horizon, secData.Count);
			double previousPrice = 0.0;
			int i, j;
			i = 0;
			foreach (KeyValuePair<string, Security> sd in secData)
			{
				int dayCount = sd.Value.secHist.secHistory.Count;
				j = 0;
				foreach (KeyValuePair<DateTime, SecHistData> shd in sd.Value.secHist.secHistory)
				{
					if (0 == i)
					{
						amortisationRates[j, 0] = ((j < horizon - 1) ? (1.0 - lambda) * Math.Pow(lambda, (double)(horizon - 2 - j)) : 0);
					}
					if (horizonStart <= shd.Key)
					{
						if (0 != j) 
						{
							logReturns[j-1, i] = Math.Log(shd.Value.dataPoint.Item2 / previousPrice);
						}
						previousPrice = shd.Value.dataPoint.Item2;
						j++;
					}
				}
				i++;
			}

			Console.WriteLine("Amortisations");
			for (j = 0; j < horizon; j++) Console.WriteLine("{0}", amortisationRates[j, 0]);
			Console.WriteLine("Log Returns");
			for (i = 0; i < secData.Count; i++) 
			{
				for (j = 0; j < horizon; j++) Console.Write("{0}	", logReturns[j, i]);
				Console.WriteLine();
			}
		}

		private void calculateAssetVolatility(SortedDictionary<string, Security> secData)
		{
			sumProductsPrice = new Matrix(secData.Count, secData.Count);
			covarianceMatrix = new Matrix(secData.Count, secData.Count);
			volatilityMatrix = new Matrix(secData.Count, secData.Count);

			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = (DateTime.Today - horizonStart).Days;

			//Assemble the sums
			int i = 0;
			int j = 0;

			/*
			foreach (KeyValuePair<string, Security> sd_i in secData)
			{
				j = 0;
				SortedDictionary<DateTime,SecHistData> sd_i_hist = sd_i.Value.secHist.secHistory;
				foreach (KeyValuePair<string, Security> sd_j in secData)
				{
					if (i >= j)	//No need for double calcs, we'll take care of the symmetry at the end
					{
						int matchCount = 0;
						SortedDictionary<DateTime, SecHistData> sd_j_hist = sd_j.Value.secHist.secHistory;
						foreach (KeyValuePair<DateTime, SecHistData> sd_i_point in sd_i_hist)
						{
							KeyValuePair<DateTime, SecHistData> dateMatch = sd_j_hist.First(x=>x.Key==sd_i_point.Key);
							sumProductsPrice[i, j] += sd_i_point.Value.dataPointPrice() * dateMatch.Value.dataPointPrice();
							matchCount++;
						}
						covarianceMatrix[i, j] = sumProductsPrice[i, j] / matchCount - meansPrice[0, i] * meansPrice[0, j];
					}
					j++;
				}
				i++;
			}

			for (i = 0; i < secData.Count; i++)
			{
				for (j = 0; j < secData.Count; j++)
				{
					if (i < j) covarianceMatrix[i, j] = covarianceMatrix[j, i];
				}
			}
			*/

			for (int k = 0; k < horizon; k++)
			{
				for (i = 0; i < secData.Count; i++)
				{
					for (j = 0; j < secData.Count; j++)
					{
						if (i >= j)
						{
							volatilityMatrix[i, j] += logReturns[k, i] * logReturns[k, j] * amortisationRates[k,0] / (double)horizon;
						}
					}
				}
			}

			for (i = 0; i < secData.Count; i++)
			{
				for (j = 0; j < secData.Count; j++)
				{
					if (i < j) volatilityMatrix[i, j] = volatilityMatrix[j, i];
				}
			}

			//for (i = 0; i < secData.Count; i++)
			//{
			//	for (j = 0; j < secData.Count; j++) Console.Write("{0}	", covarianceMatrix[i, j]);
			//	Console.WriteLine();
			//}
			for (i = 0; i < secData.Count; i++)
			{
				for (j = 0; j < secData.Count; j++) Console.Write("{0}	", volatilityMatrix[i, j]);
				Console.WriteLine();
			}
		}

		private double doOverallPnLCalculation(double percentile)
		{
			//Matrix portfolioVolatility = weights.Multiply(covarianceMatrix.Multiply(weights.Transpose()));
			Matrix portfolioVolatility = weights.Multiply(volatilityMatrix.Multiply(weights.Transpose()));
			if ((1 != portfolioVolatility.NRows) || (1 != portfolioVolatility.NColumns))
			{
				throw new IliasKoloniasTechnicalTestAberdeenAM_Exception("Bad configuration!");
			}

			//Console.WriteLine("{0}, {1}, {2}", portfolioVolatility[0, 0], GaussianInverseCDF.doInverse(percentile), lastTotalSum);

			return Math.Sqrt(portfolioVolatility[0,0]) * GaussianInverseCDF.doInverse(percentile) * lastTotalSum;
		}

		private SortedDictionary<string, double> diaggregatePnL(SortedDictionary<string, Security> secData)
		{
			SortedDictionary<string, double> result = new SortedDictionary<string, double>();
			int i = 0;
			foreach (KeyValuePair<string, Security> sd in secData)
			{
				result.Add(sd.Key, lastCalculatedVaR * weights[0, i]);
				i++;
			}
			return result;
		}
	}
}