using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wsdot.Dor.Tax.DataContracts
{
	public class LocationCodeBoundary
	{
		/// <summary>
		/// The coordinates that define the boundary polygon.
		/// </summary>
		public byte[] Shape { get; set; }

		/// <summary>
		/// The unique identifier for a tax boundary.
		/// </summary>
		public string LocationCode { get; set; }
	}
}
