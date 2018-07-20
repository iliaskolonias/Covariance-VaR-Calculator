using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_Base
{
	public partial class SecurityHistory
	{
		public SecurityHistory()
		{
			secHistory = new SortedDictionary<DateTime, SecHistData>();
		}
		public SortedDictionary<DateTime, SecHistData> secHistory { get; set; }
	}
}
