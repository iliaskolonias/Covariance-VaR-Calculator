using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using CovarianceVaRCalculator_Base;

namespace CovarianceVaRCalculator_Engine
{
	public partial class Parametric1DayVaR
	{
		//Test mode?
		bool testMode = false;
		
		//Lambda value for amortisation
		public double lambda = 0.94;

		//Debug output control
		private int allowDebugOutput;
	
		//For weights calculation
		double lastTotalSize;
		double lastTotalSum;
		private Matrix sumsSize;
		private Matrix sumsMoney;
		private Matrix weights;

		//For volatility calculations
		private Matrix logReturns;
		private Matrix amortisationRates;
		private Matrix volatilityMatrix;

		//Final VaR calculation
		double lastCalculatedVaR;
		BackgroundWorker covarianceMainWorker;
		BackgroundWorker[] covarianceThreads;
		int threadCount;
		int threadsRunning;
		int numThreadsCompleted;
		int percentComplete;
		int highestPercentageReached = 0;
		object lockObj;

		public Parametric1DayVaR(int allowDebug = 0)
		{
			allowDebugOutput = allowDebug;
		}
		public Tuple<double, SortedDictionary<string, Tuple<double, double> > > run1DayVar
		(
			 SortedDictionary<string, Security> secData, double percentile
			,DateTime startDate, DateTime endDate
			,BackgroundWorker worker, DoWorkEventArgs e
		)
		{
			try
			{
				threadCount = secData.Count;
				threadsRunning = 0;
				numThreadsCompleted = 0;
				percentComplete = 0;
				highestPercentageReached = 0;
				calculateAssetWeights(secData, startDate, endDate, worker, e);
				calculateLogReturns(secData, startDate, endDate, worker, e);
				calculateAssetVolatility(secData, startDate, endDate, worker, e, e == null);
				lastCalculatedVaR = doOverallPnLCalculation(percentile);
				if (0!=allowDebugOutput) Console.WriteLine("Calculated VaR: {0}", lastCalculatedVaR);
				SortedDictionary<string, Tuple<double, double>> data = diaggregatePnL(secData, percentile);
				return new Tuple<double,SortedDictionary<string,Tuple<double,double>>>(lastCalculatedVaR, data);
			}
			catch (Exception ex)
			{
				throw new CovarianceVaRCalculator_Exception(String.Format("Something went wrong with the calculation. Error details: {0}", ex.Message), ex);
			}
		}

		private void calculateAssetWeights(SortedDictionary<string, Security> secData, DateTime startDate, DateTime endDate, BackgroundWorker worker = null, DoWorkEventArgs e = null)
		{
			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = DateUtilities.numWorkingDaysInRange(horizonStart, endDate);

			//Weights
			if ((worker!=null) && (e!=null))
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
				}
			}
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
			for (i = 0; i < sumsSize.numColumns; i++) lastTotalSize += sumsSize[sumsSize.numRows - 1, i];
			for (i = 0; i < sumsMoney.numColumns; i++) lastTotalSum += sumsMoney[sumsMoney.numRows - 1, i];
			for (i = 0; i < sumsMoney.numColumns; i++) weights[0, i] = sumsMoney[sumsMoney.numRows - 1, i] / lastTotalSum;
		}

		private void calculateLogReturns(SortedDictionary<string, Security> secData, DateTime startDate, DateTime endDate, BackgroundWorker worker = null, DoWorkEventArgs e = null)
		{
			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = DateUtilities.numWorkingDaysInRange(horizonStart, endDate);
			int i, j;
			amortisationRates = new Matrix(horizon, 1);
			logReturns = new Matrix(horizon, secData.Count);
			double previousPrice;

			//Weights
			if ((worker != null) && (e != null))
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
				}
			}
			previousPrice = 0.0;
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
							logReturns[j - 1, i] = Math.Log(shd.Value.dataPoint.Item2 / previousPrice);
						}
						previousPrice = shd.Value.dataPoint.Item2;
						j++;
					}
				}
				i++;
			}
			//Normalise to 1-year volatility, and cut off any excess
			int workingDaysInYear = DateUtilities.numWorkingDaysInRange(endDate.AddYears(-1).AddDays(1), endDate);
			double amortisationScalingFactor = (1.0 - Math.Pow(lambda, (double)((horizon > workingDaysInYear) ? workingDaysInYear : horizon)));
			for (i = 0; i < horizon; i++)
			{
				amortisationRates[i, 0] /= amortisationScalingFactor;
				if (i > workingDaysInYear) amortisationRates[i, 0] = 0;
			}

			if (2 < allowDebugOutput)
			{
				Console.WriteLine("Amortisations");
				for (j = 0; j < horizon; j++) Console.WriteLine("{0}", amortisationRates[j, 0]);
				Console.WriteLine("Log Returns");
				for (i = 0; i < secData.Count; i++)
				{
					for (j = 0; j < horizon; j++) Console.Write("{0}	", logReturns[j, i]);
					Console.WriteLine();
				}
			}
		}

		private void InitializeBackgroundWorkers(int secCount)
		{
			covarianceThreads = new BackgroundWorker[secCount];
			for (int i = 0; i < secCount;i++ )
			{
				covarianceThreads[i] = new BackgroundWorker();
				covarianceThreads[i].WorkerSupportsCancellation = true;
				covarianceThreads[i].DoWork += new DoWorkEventHandler(covarianceThreads_DoWork);
				covarianceThreads[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(covarianceThreads_RunWorkerCompleted);
			}
		}

		private void covarianceThreads_DoWork(object sender, DoWorkEventArgs e)
		{
			// Get the BackgroundWorker that raised this event.
			int secCount = (e.Argument as Tuple<int, int, int, BackgroundWorker>).Item1;
			int horizon = (e.Argument as Tuple<int, int, int, BackgroundWorker>).Item2;
			int curSec = (e.Argument as Tuple<int, int, int, BackgroundWorker>).Item3;
			BackgroundWorker worker = sender as BackgroundWorker;
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			if ((worker != null) && (e != null))
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
				}
			}
			//Go to work with it
			int j;
			Matrix volatilityMatrixRow = new Matrix(1, secCount);
			for (j = 0; j < secCount; j++)
			{
				if (curSec >= j)
				{
					for (int k = 0; k < horizon; k++)
					{
						volatilityMatrixRow[0, j] += logReturns[k, curSec] * logReturns[k, j] * amortisationRates[k, 0];
					}
				}
				else volatilityMatrixRow[0, j] = volatilityMatrixRow[0, secCount - 1 - j];
			}
			e.Result = new Tuple<int, Matrix>(curSec, volatilityMatrixRow);
		}

		private void covarianceThreads_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			int curSec;
			Matrix covarianceRow;
			//BackgroundWorker parent = null;

			//Check what we've got
			if (e.Error != null)
			{
				//Error
				//MessageBox.Show(e.Error.Message);
			}
			else if (e.Cancelled)
			{
				//Cancellation
				//MessageBox.Show("Operation Cancelled!");
			}
			else
			{
				// Success, load up the data
				Tuple<int, Matrix> result = e.Result as Tuple<int, Matrix>;
				curSec = result.Item1;
				covarianceRow = result.Item2;
				for (int i = 0; i < covarianceRow.numColumns; i++) volatilityMatrix[curSec, i] = covarianceRow[0, i];
				try
				{
					Interlocked.Increment(ref numThreadsCompleted);
					if ((covarianceMainWorker != null) && (covarianceMainWorker.IsBusy))
					{
						if (Monitor.TryEnter(percentComplete, 20))
						{
							percentComplete = 100 * numThreadsCompleted / threadCount;
							if (percentComplete > highestPercentageReached)
							{
								try
								{
									if (Monitor.TryEnter(highestPercentageReached, 10))
									{
										highestPercentageReached = percentComplete;
										if (Monitor.IsEntered(highestPercentageReached)) Monitor.Exit(highestPercentageReached);
									}
								}
								catch (Exception exc)
								{
									throw new CovarianceVaRCalculator_Exception(String.Format("Calcluation failure! Exception message: {0}", exc.Message), exc);
								}
								if (!testMode) covarianceMainWorker.ReportProgress(highestPercentageReached);
							}
							if (Monitor.IsEntered(percentComplete)) Monitor.Exit(percentComplete);
						}
					}
				}
				finally
				{
					if (!testMode) covarianceMainWorker.ReportProgress(highestPercentageReached);
					Interlocked.Decrement(ref threadsRunning);
				}
			}
		}

		private void calculateAssetVolatility(SortedDictionary<string, Security> secData, DateTime startDate, DateTime endDate, BackgroundWorker worker = null, DoWorkEventArgs e = null, bool testing = false)
		{
			testMode = testing;
			volatilityMatrix = new Matrix(secData.Count, secData.Count);

			//Find time horizon (let's not do it with default here...)
			//Find latest start date for security data
			covarianceMainWorker = worker;
			InitializeBackgroundWorkers(secData.Count);
			DateTime horizonStart = secData.Values.Max(x => x.secHist.secHistory.Keys.Min());
			int horizon = DateUtilities.numWorkingDaysInRange(horizonStart, endDate);

			//Assemble the sums
			numThreadsCompleted = 0;
			percentComplete = 0;
			highestPercentageReached = 0;
			threadCount = secData.Count;
			int i = 0;
			int j = 0;

			if ((worker != null) && (e != null))
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
				}
			}
			for (i = 0; i < secData.Count; i++)
			{
				while (32 == threadsRunning) Thread.Sleep(1);
				covarianceThreads[i].RunWorkerAsync(new Tuple<int, int, int, BackgroundWorker>(secData.Count, horizon, i, worker));
				Interlocked.Increment(ref threadsRunning);
			}

			while (numThreadsCompleted < secData.Count)
			{
				//Sit tight ad wait nocely for everything to end...
				Thread.Sleep(1);
			}

			if (2 < allowDebugOutput)
			{
				Console.WriteLine("Volatility Matrix");
				for (i = 0; i < secData.Count; i++)
				{
					for (j = 0; j < secData.Count; j++) Console.Write("{0}	", volatilityMatrix[i, j]);
					Console.WriteLine();
				}
			}
		}

		private double doOverallPnLCalculation(double percentile)
		{
			Matrix portfolioVolatility = weights.Multiply(volatilityMatrix.Multiply(weights.Transpose()));
			if ((1 != portfolioVolatility.numRows) || (1 != portfolioVolatility.numColumns))
			{
				throw new CovarianceVaRCalculator_Exception("Bad configuration!");
			}

			return Math.Sqrt(portfolioVolatility[0,0]) * GaussianInverseCDF.doInverse(percentile) * lastTotalSum;
		}

		private SortedDictionary<string, Tuple<double,double> > diaggregatePnL(SortedDictionary<string, Security> secData, double percentile)
		{
			SortedDictionary<string, Tuple<double, double> > result = new SortedDictionary<string, Tuple<double, double>>();
			int i = 0;
			foreach (KeyValuePair<string, Security> sd in secData)
			{
				double individualVaR = weights[0,i] * Math.Sqrt(volatilityMatrix[i,i]) * GaussianInverseCDF.doInverse(percentile) * lastTotalSum;
				Tuple<double, double> VaRs = new Tuple<double,double>(lastCalculatedVaR * weights[0, i], individualVaR);
				result.Add(sd.Key, VaRs);
				i++;
			}

			if (1 < allowDebugOutput)
			{
				foreach (KeyValuePair<string, Tuple<double, double>> r in result)
				{
					Console.WriteLine("VaR for {0}: {1} (if it was on its own: {2})", r.Key, r.Value.Item1, r.Value.Item2);
				}
			}
			return result;
		}
	}
}