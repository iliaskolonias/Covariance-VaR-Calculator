using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_Base
{
	public partial class SecHistData
	{
		public Tuple<int, double> dataPoint {get; set;}
		public int dataPointQuantity()
		{
			return dataPoint.Item1;
		}
		public double dataPointPrice()
		{
			return dataPoint.Item2;
		}
		public double dataPointSize()
		{
			return (double)dataPoint.Item1 * dataPoint.Item2;
		}
	}
}
