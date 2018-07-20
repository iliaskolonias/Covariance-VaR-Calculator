using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_Base
{	public class CovarianceVaRCalculator_Exception : ApplicationException
	{
		public CovarianceVaRCalculator_Exception()
		{
		}
		public CovarianceVaRCalculator_Exception(string message) : base(message)
		{
		}
		public CovarianceVaRCalculator_Exception(string message, Exception inner) : base(message, inner)
		{
		}
	}
}