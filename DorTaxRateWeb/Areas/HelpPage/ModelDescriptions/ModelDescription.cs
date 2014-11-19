#pragma warning disable 1591
using System;

namespace Wsdot.Dor.Tax.Web.Areas.HelpPage.ModelDescriptions
{
	/// <summary>
	/// Describes a type model.
	/// </summary>
	public abstract class ModelDescription
	{
		public string Documentation { get; set; }

		public Type ModelType { get; set; }

		public string Name { get; set; }
	}
}
#pragma warning restore 1591
