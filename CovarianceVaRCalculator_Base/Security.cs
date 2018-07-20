using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_Base
{
	public partial class Security
    {
        public Security(string name)
        {
            Name = name;
			secHist = new SecurityHistory();
        }
        public string Name				{ get; set; }
		public SecurityHistory secHist	{ get; set; }
    }
}
