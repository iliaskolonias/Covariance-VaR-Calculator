using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

using CovarianceVaRCalculator_Engine;

namespace CovarianceVaRCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private BackgroundWorker VaRCalculator;
		private DateTime startCalcTimeStamp;
	
		private void VaRCalculator_DoWork(object sender, DoWorkEventArgs e)
		{
			// Get the BackgroundWorker that raised this event.
			BackgroundWorker worker = sender as BackgroundWorker;
			//Go to work with it
			try
			{
				CovarianceVaRCalculatorEngine engine = new CovarianceVaRCalculatorEngine();
				e.Result = engine.RunVaR((Dictionary<String, Object>)e.Argument, worker, e);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void VaRCalculator_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//Check what we've got
			if (e.Error != null)
			{
				//Error
				MessageBox.Show(e.Error.Message);
			}
			else if (e.Cancelled)
			{
				//Cancellation
				MessageBox.Show("Operation Cancelled!");
			}
			else
			{
				MessageBox.Show(String.Format("Operation Completed! Time taken: {0}", (DateTime.Now - startCalcTimeStamp).ToString()));
				// Success, load up the data
				Tuple<double, List<VaRResult>> result = e.Result as Tuple<double, List<VaRResult>>;
				Total.Text = result.Item1.ToString();
				(DataContext as MainWindowViewModel).populate(result.Item2);
				VaRData.ItemsSource = (DataContext as MainWindowViewModel).Securities;
				CollectionViewSource.GetDefaultView(VaRData.ItemsSource).Refresh();
			}

			// Enable the buttons
			VaRData.Visibility = System.Windows.Visibility.Visible;
			CancelButton.IsEnabled = false;
			RunButton.IsEnabled = true;
		}

		private void VaRCalculator_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressShow.Value = e.ProgressPercentage;
		}

		private void RunButton_Click(object sender, EventArgs e)
		{
			//Validate the input data, and run the simulation
			bool dataIsGood = true;
			Dictionary<String, Object> parameters = new Dictionary<String, Object>();
			Int32 numSecurities;
			Double confidenceInterval;
			DateTime startDate;
			DateTime endDate;
			if (dataIsGood)
			{
				if (NumSecurities.Text == String.Empty) dataIsGood = false;
				dataIsGood &= Int32.TryParse(NumSecurities.Text, out numSecurities);				//Number of securities in the VaR model
				if (dataIsGood)
				{
					parameters.Add("numSecurities", numSecurities);
					if (ConfidencePercent.Text == String.Empty) dataIsGood = false;
					dataIsGood &= Double.TryParse(ConfidencePercent.Text, out confidenceInterval);	//Confidence interval
					confidenceInterval /= 100.0;
					if (dataIsGood)
					{
						parameters.Add("confidenceInterval", confidenceInterval);
						if (StartDate.Text == String.Empty) dataIsGood = false;
						dataIsGood &= DateTime.TryParse(StartDate.Text, out startDate);				//Start date
						if (dataIsGood)
						{
							parameters.Add("startDate", startDate);
							if (EndDate.Text == String.Empty) dataIsGood = false;
							dataIsGood &= DateTime.TryParse(EndDate.Text, out endDate);				//End date
							if (dataIsGood)
							{
								parameters.Add("endDate", endDate);
								if ((endDate - startDate).Days <= 0) dataIsGood = false;
								if (0 >= numSecurities) dataIsGood = false;
								if ((0 >= confidenceInterval) || (1 <= confidenceInterval)) dataIsGood = false;
								if (dataIsGood)
								{
									//Good to go
									CancelButton.Visibility = System.Windows.Visibility.Visible;
									CancelButton.IsEnabled = true;
									RunButton.IsEnabled = false;
									VaRData.Visibility = System.Windows.Visibility.Hidden;
									startCalcTimeStamp = DateTime.Now;
									VaRCalculator.RunWorkerAsync(parameters);
									return;
								}
							}
						}
					}
				}
			}
			MessageBox.Show("Wrong/incomplete data for simulation");
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			VaRCalculator.CancelAsync();
			VaRData.Visibility = System.Windows.Visibility.Visible;
			CancelButton.IsEnabled = false;
			CancelButton.Visibility = System.Windows.Visibility.Hidden;
			RunButton.IsEnabled = true;
		}

		private void checkInt(object sender, TextCompositionEventArgs e)
		{
			int val;
			e.Handled = !Int32.TryParse(e.Text, out val);
		}

		private void validateInt(object sender, DataObjectPastingEventArgs e)
		{
			int val;
			if (!Int32.TryParse(e.DataObject.GetData(typeof(string)).ToString(), out val))
			{
				e.CancelCommand();
			}
		}

		private void checkDouble(object sender, TextCompositionEventArgs e)
		{
			double val;
			e.Handled = !Double.TryParse(e.Text.Replace('.','0'), out val);		//Won't be able to get the '.' through otherwise
		}

		private void validateDouble(object sender, DataObjectPastingEventArgs e)
		{
			double val;
			if (!Double.TryParse(e.DataObject.GetData(typeof(string)).ToString(), out val))
			{
				e.CancelCommand();
			}
		}

		private void InitializeBackgroundWorker()
		{
			VaRCalculator = new BackgroundWorker();
			VaRCalculator.WorkerReportsProgress = true;
			VaRCalculator.WorkerSupportsCancellation = true;
			VaRCalculator.DoWork += new DoWorkEventHandler(VaRCalculator_DoWork);
			VaRCalculator.RunWorkerCompleted +=	new RunWorkerCompletedEventHandler(VaRCalculator_RunWorkerCompleted);
			VaRCalculator.ProgressChanged += new ProgressChangedEventHandler(VaRCalculator_ProgressChanged);
		}

		private void InitializeControls()
		{
			RunButton.Click += new RoutedEventHandler(RunButton_Click);
			CancelButton.Click += new RoutedEventHandler(CancelButton_Click);
			CancelButton.IsEnabled = false;
			CancelButton.Visibility = System.Windows.Visibility.Hidden;			//Haven't implemented cancelling yet...
			StartDate.Text = DateTime.Today.ToString();
			EndDate.Text = DateTime.Today.ToString();

		}

		public MainWindow()
        {
			InitializeComponent();
			InitializeBackgroundWorker();
			InitializeControls();
			DataContext = new MainWindowViewModel();
        }
    }
}
