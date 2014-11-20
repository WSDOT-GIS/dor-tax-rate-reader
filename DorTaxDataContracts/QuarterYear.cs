using System;

namespace Wsdot.Dor.Tax.DataContracts
{
	/// <summary>
	/// Represents a quarter of a specific year.
	/// </summary>
	public struct QuarterYear
	{
		private int _year;
		private int _quarter;

		/// <summary>
		/// Year
		/// </summary>
		public int Year
		{
			get { return _year; }
		}


		/// <summary>
		/// The quarter of the year.
		/// </summary>
		public int Quarter
		{
			get { return _quarter; }
		}

		/// <summary>
		/// Creates a new <see cref="QuarterYear"/>
		/// </summary>
		/// <param name="year">The year.</param>
		/// <param name="quarter">The quarter.</param>
		public QuarterYear(int year, int quarter)
		{
			_year = year;
			_quarter = quarter;
		}

		/// <summary>
		/// Creates a <see cref="QuarterYear"/> from a specified <see cref="DateTime"/>.
		/// </summary>
		/// <param name="date"></param>
		public QuarterYear(DateTime date): this(date.Year, GetQuarter(date))
		{
		}

		/// <summary>
		/// Gets the date range that this quarter year falls into.
		/// </summary>
		/// <returns></returns>
		public DateTime[] GetDateRange()
		{
			switch (this.Quarter)
			{
				case 1:
					return new[] { 
						new DateTime(this.Year, 1, 1),
						new DateTime(this.Year, 4, 1).Subtract(new TimeSpan(1))
					};
				case 2:
					return new[] { 
						new DateTime(this.Year, 4, 1),
						new DateTime(this.Year, 7, 1).Subtract(new TimeSpan(1))
					};
				case 3:
					return new[] { 
						new DateTime(this.Year, 7, 1),
						new DateTime(this.Year, 10, 1).Subtract(new TimeSpan(1))
					};
				case 4:
					return new[] { 
						new DateTime(this.Year, 10, 1),
						new DateTime(this.Year + 1, 1, 1).Subtract(new TimeSpan(1))
					};
				default:
					throw new IndexOutOfRangeException("Invalid quarter");
			}
		}

		/// <summary>
		/// Converts the <see cref="QuarterYear"/> into a string representation.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0}Q{1}", this.Year, this.Quarter);
		}

		/// <summary>
		/// Determines if this <see cref="QuarterYear"/> is equal to another <see cref="Object"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>Returns <see langword="true"/> if the two values are equal, <see langword="false"/> otherwise.</returns>
		public override bool Equals(object obj)
		{
			if (obj != null && obj.GetType() == typeof(QuarterYear))
			{
				var other = (QuarterYear)obj;
				return this.Year == other.Year && this.Quarter == other.Quarter;
			}
			else
			{
				return base.Equals(obj);
			}
		}

		/// <summary>
		/// Gets a hash code for the given quarter year. Hash code will be Year × 100 + Quarter.
		/// </summary>
		/// <returns>Year × 100 + Quarter</returns>
		public override int GetHashCode()
		{
			return this.Year * 100 + this.Quarter;
		}

		
		/// <summary>
		/// Determines if two <see cref="QuarterYear"/> values are equal.
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns>Returns <see langword="true"/> if the two values are equal, <see langword="false"/> otherwise.</returns>
		public static bool operator ==(QuarterYear q1, QuarterYear q2)
		{
			return q1.Equals(q2);
		}

		/// <summary>
		/// Determines if two <see cref="QuarterYear"/> values are not equal.
		/// </summary>
		/// <param name="q1"></param>
		/// <param name="q2"></param>
		/// <returns>Returns <see langword="true"/> if the two values are not equal, <see langword="false"/> otherwise.</returns>
		public static bool operator !=(QuarterYear q1, QuarterYear q2)
		{
			return !q1.Equals(q2);
		}


		/// <summary>
		/// Gets the quarter for the given date.
		/// </summary>
		/// <param name="date"></param>
		/// <returns>Returns the quarter that the given month falls into (1-4).</returns>
		public static int GetQuarter(DateTime date)
		{
			double mDiv3 = date.Month / 3;
			return Convert.ToInt32(Math.Ceiling(mDiv3) + 1);
		}

		/// <summary>
		/// Determines if the specified values for the <see cref="QuarterYear"/> are valid.
		/// </summary>
		/// <returns>Returns true if valid, false if not.</returns>
		public bool IsValid
		{
			get {
				return Quarter <= 4 && ((Year > 2011 && Quarter >= 1) 
					|| (Year > 2008 && Quarter >= 3) 
					|| (Year == 2008 && Quarter >= 2));
			}
		}

		/// <summary>
		/// Returns the current <see cref="QuarterYear"/>.
		/// </summary>
		public static QuarterYear Current {
			get
			{
				return new QuarterYear(DateTime.Today);
			}
		}
	}
}
