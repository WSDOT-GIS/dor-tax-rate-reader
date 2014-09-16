using System;

namespace Wsdot.Dor.Tax.DataContracts
{
	/// <summary>
	/// Represents a record in the Tax Rate table.
	/// </summary>
	public class TaxRateItem
	{
		/// <summary>
		/// Location code name
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Location code number
		/// </summary>
		public string Code { get; set; }
		/// <summary>
		/// State rate
		/// </summary>
		public float State { get; set; }
		/// <summary>
		/// Local rate (For location codes in RTA areas, includes RTA rate)
		/// </summary>
		public float Local { get; set; }
		/// <summary>
		/// RTA rate (currently defaulted to zero)
		/// </summary>
		public float Rta { get; set; }
		/// <summary>
		/// Combined state and local rates
		/// </summary>
		public float Rate { get; set; }
		/// <summary>
		/// Rate effective start date
		/// </summary>
		public DateTime EffectiveDate { get; set; }
		/// <summary>
		/// Rate expiration date
		/// </summary>
		public DateTime ExpirationDate { get; set; }
	}
}
