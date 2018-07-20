using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CovarianceVaRCalculator_Base;

namespace CovarianceVaRCalculator_Engine
{
	public static class GaussianInverseCDF
	{
		private static double[] c = new double[] { 2.515517, 0.802853, 0.010328 };
		private static double[] d = new double[] { 1.432788, 0.189269, 0.001308 };

		private static double RationalApproximation(double t)
		{
			// Abramowitz and Stegun formula 26.2.23.
			// The absolute value of the error should be less than 4.5 e-4.
			return t - ( ( c[2] * t + c[1] ) * t + c[0] ) /
						( ( ( d[2] * t + d[1] ) * t + d[0] ) * t + 1.0 );
		}
 
		public static double doInverse(double p)
		{
			if (p <= 0.0 || p >= 1.0)
			{
				throw new CovarianceVaRCalculator_Exception("Bad Gaussian inversion!");
			}
 
			if (p < 0.5)
			{
				// F^-1(p) = - G^-1(p)
				return -RationalApproximation( Math.Sqrt(-2.0 * Math.Log(p)) );
			}
			else
			{
				// F^-1(p) = G^-1(1-p)
				return RationalApproximation( Math.Sqrt(-2.0 * Math.Log(1-p)) );
			}
		}
	}
}
