using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovarianceVaRCalculator_Base
{
	public static partial class DateUtilities
	{
		public static bool isWorkingDay(DateTime date)
		{
			return (!((DayOfWeek.Saturday == date.DayOfWeek) || (DayOfWeek.Sunday == date.DayOfWeek)));
		}

		public static int numWorkingDaysInRange(DateTime fromDate, DateTime toDate)
		{
			DateTime startDate = ((fromDate > toDate) ? toDate : fromDate);
			DateTime endDate = ((fromDate > toDate) ? fromDate : toDate);
			int days = 0;
			for (DateTime curDate = startDate; curDate <= endDate; curDate = curDate.AddDays(1))
			{
				if (isWorkingDay(curDate)) days++;
			}
			return days;
		}
	}
}
