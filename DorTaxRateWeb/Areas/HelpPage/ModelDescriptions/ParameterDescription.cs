#pragma warning disable 1591
using System.Collections.ObjectModel;

namespace Wsdot.Dor.Tax.Web.Areas.HelpPage.ModelDescriptions
{
	public class ParameterDescription
	{
		public ParameterDescription()
		{
			Annotations = new Collection<ParameterAnnotation>();
		}

		public Collection<ParameterAnnotation> Annotations { get; private set; }

		public string Documentation { get; set; }

		public string Name { get; set; }

		public ModelDescription TypeDescription { get; set; }
	}
}
#pragma warning restore 1591
