using NetTopologySuite.Features;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebApi.OutputCache.V2;
using Wsdot.Dor.Tax;
using Wsdot.Dor.Tax.DataContracts;

namespace DorTaxRateWeb.Controllers
{
	/// <summary>
	/// Provides access to tax rate data from 
	/// <see href="http://dor.wa.gov/content/FindTaxesAndRates/Downloads.aspx">Washington State Department of Revenue</see>.
	/// </summary>
	[RoutePrefix("tax")]
	public class TaxRateController : ApiController
	{
		const int _defaultCache = 365*24*60*60*60;

		/// <summary>
		/// Gets the tax rates for a specific quarterYear year.
		/// </summary>
		/// <param name="year">A year. Minimum allowed value is 2008.</param>
		/// <param name="quarterYear">An integer representing a quarterYear: a value of 1 through 4. For 2008, only quarters 3 and 4 are available.</param>
		/// <returns>Returns a list of <see cref="TaxRateItem"/> objects.</returns>
		[Route("rates/{year:min(2008)}/{quarter:range(1,4)}")]
		[CacheOutput(ServerTimeSpan=_defaultCache, ClientTimeSpan=_defaultCache)]
		public HttpResponseMessage GetTaxRates(int year, int quarter)
		{
			IEnumerable<TaxRateItem> taxRates = DorTaxRateReader.GetTaxRates(new QuarterYear(year, quarter)).Select(kvp => kvp.Value);
			var response = this.Request.CreateResponse<IEnumerable<TaxRateItem>>(taxRates);
			return response;
		}

		/// <summary>
		/// Gets the current tax rates by redirecting to <see cref="GetCurrentTaxRates(int, int)"/> for the current quarterYear-year.
		/// </summary>
		/// <returns>An <see cref="HttpResponseMessage"/> that redirects to <see cref="GetCurrentTaxRates(int, int)"/>.</returns>
		[Route("rates")]
		public HttpResponseMessage GetCurrentTaxRates()
		{
			var qy = new QuarterYear(DateTime.Now);
			string newUrl = this.Request.RequestUri.ToString().TrimEnd('/') + string.Format("/{0}/{1}", qy.Year, qy.Quarter);
			var response = this.Request.CreateResponse(System.Net.HttpStatusCode.Redirect);
			response.Headers.Location = new Uri(newUrl);
			return response;
		}


		[Route("boundaries/{year:min(2008)}/{quarter:range(1,4)}")]
		[CacheOutput(ServerTimeSpan = _defaultCache, ClientTimeSpan = _defaultCache)]
		public HttpResponseMessage GetSalesTaxJursitictionBoundaries(int year, int quarter)
		{
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(new QuarterYear(year, quarter)).Select(feature => new {
				LocationCode = feature.Attributes["LocationCode"],
				Wkt = feature.Geometry != null ? feature.Geometry.AsText() : null
			}).ToDictionary(k => k.LocationCode, v => v.Wkt);
			var response = this.Request.CreateResponse(boundaries);
			return response;
		}

		[Route("boundaries")]
		public HttpResponseMessage GetCurrentSalesTaxJuristictionBoundaries()
		{
			var qy = new QuarterYear(DateTime.Now);
			string newUrl = this.Request.RequestUri.ToString().TrimEnd('/') + string.Format("/{0}/{1}", qy.Year, qy.Quarter);
			var response = this.Request.CreateResponse(System.Net.HttpStatusCode.Redirect);
			response.Headers.Location = new Uri(newUrl);
			return response;
		}
	}
}
