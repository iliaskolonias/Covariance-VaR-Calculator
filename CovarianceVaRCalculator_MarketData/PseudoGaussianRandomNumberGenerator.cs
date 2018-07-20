using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_MarketData
{
	public static partial class PseudoRandomNumberGenerator
	{
		public static int Next(this Random rand)
		{
			return rand.Next();
		}
		public static int Next(this Random rand, int i)
		{
			return rand.Next(i);
		}
		public static int Next(this Random rand, int i, int j)
		{
			return rand.Next(i, j);
		}
		public static double NextDouble(this Random rand)
		{
			return rand.NextDouble();
		}

		public static double NextGaussianDouble(this Random rand)
		{
			// Small trick (Box-Muller transform)
			//Take 2 uniform (0,1) random doubles, transform and we're good to go
			//Producing a random normal N(0,1) distribution
			double u1 = rand.NextDouble();
			double u2 = rand.NextDouble();
			return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		}
		public static double NextGaussianDouble(this Random rand, double mean, double stdDev)
		{
			//Convert to random normal N(mean,stdDev^2)
			return mean + stdDev * rand.NextGaussianDouble();
		}
	}
}
