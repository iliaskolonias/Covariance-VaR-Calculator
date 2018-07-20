using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading.Tasks;

using CovarianceVaRCalculator_Base;
using CovarianceVaRCalculator_Engine;
using CovarianceVaRCalculator_MarketData;

namespace CovarianceVaRCalculator
{
	class MainWindowViewModel
	{
		private List<VaRResult> data;
		public ICollectionView Securities { get; private set; }

		public MainWindowViewModel()
		{
		}

		public void populate(List<VaRResult> results)
		{
			data = results;
			Securities = CollectionViewSource.GetDefaultView(data);
			Securities.Refresh();
		}
	}
}
