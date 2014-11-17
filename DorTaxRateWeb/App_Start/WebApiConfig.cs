using Wsdot.Dor.Tax.Web.Formatters;
using System.Web.Http;

namespace Wsdot.Dor.Tax.Web
{
	/// <summary>
	/// Configures WebAPI.
	/// </summary>
	public static class WebApiConfig
	{
		/// <summary>
		/// Register components.
		/// </summary>
		/// <param name="config"></param>
		public static void Register(HttpConfiguration config)
		{
			// Insert the GeoJsonFormatter at the top of the list so it is the default JSON formatter for supported types.
			config.Formatters.Insert(0, new GeoJsonFormatter());
			config.Formatters.Remove(config.Formatters.XmlFormatter);
			config.MapHttpAttributeRoutes();
		}
	}
}