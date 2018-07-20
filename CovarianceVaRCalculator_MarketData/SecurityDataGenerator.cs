using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CovarianceVaRCalculator_Base;

namespace CovarianceVaRCalculator_MarketData
{
	public partial class SecurityDataGenerator
	{
		public static Random rng = new Random(1000);
		public SortedDictionary<string, Security> doGenerateSecurities(int numSecurities, DateTime startDate, DateTime endDate)
		{
			SortedDictionary<string, Security> result;
			try
			{
				result = new SortedDictionary<string, Security>();
			}
			catch (OutOfMemoryException e)
			{
				//Not really going to happen in this case
				throw new CovarianceVaRCalculator_Exception("Security dictionary memory allocation failed!", e);
			}
			for (int i=0;i<numSecurities;i++)
			{
				string secKey = String.Format("Security_{0:0000}",i);
				Security sec;
				try
				{
					sec = new Security(secKey);
				}
				catch (OutOfMemoryException e)
				{
					//Not really going to happen in this case
					throw new CovarianceVaRCalculator_Exception("Security memory allocation failed!", e);
				}
				int quantity = 0;
				int prevQuantity = 0;
				double unitPrice = 0;
				double prevUnitPrice = 0;
				for (int j = DateUtilities.numWorkingDaysInRange(startDate,endDate); j >= 0; j--)
				{
					DateTime businessDay = DateTime.Today.AddDays(-j);
					if (DateUtilities.isWorkingDay(businessDay))
					{
						SecHistData dataPoint;
						try
						{
							dataPoint = new SecHistData();
						}
						catch (OutOfMemoryException e)
						{
							//Not really going to happen in this case
							throw new CovarianceVaRCalculator_Exception("Security history memory allocation failed!", e);
						}
						if (j == (endDate - startDate).Days)
							quantity = rng.Next(100, 1000);
						else
							quantity = rng.Next(((20 > prevQuantity) ? 20 : 19 * prevQuantity / 20), (20 > prevQuantity) ? 200 : 21 * prevQuantity / 20);
						prevQuantity = quantity;

						if (j == (endDate - startDate).Days)
							unitPrice = rng.NextGaussianDouble(100.0, 5.0);
						else
							unitPrice = rng.NextGaussianDouble((1 > prevUnitPrice ? 1 : prevUnitPrice), prevUnitPrice / 20.0);
						prevUnitPrice = unitPrice;

						try
						{
							dataPoint.dataPoint = new Tuple<int, double>(quantity, unitPrice);
						}
						catch (OutOfMemoryException e)
						{
							//Not really going to happen in this case
							throw new CovarianceVaRCalculator_Exception("Security history data memory allocation failed!", e);
						}
						sec.secHist.secHistory.Add(businessDay, dataPoint);
					}
				}
				try
				{
					result.Add(secKey, sec);
				}
				catch (ArgumentException e)
				{
					//Not really going to happen in this case
					throw new CovarianceVaRCalculator_Exception("Security already in dictionary!", e);
				}
			}
			return result;
		}
	}
}
