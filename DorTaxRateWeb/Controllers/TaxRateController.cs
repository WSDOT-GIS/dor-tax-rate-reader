using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using WebApi.OutputCache.V2;
using Wsdot.Dor.Tax;
using Wsdot.Dor.Tax.DataContracts;

namespace DorTaxRateWeb.Controllers
{
	public class TaxRateController : ApiController
	{
		const int _defaultCache = 365*24*60*60*60;

		[Route("taxrates/{year:min(2008)}/{quarter:range(1,4)}")]
		[CacheOutput(ServerTimeSpan=_defaultCache, ClientTimeSpan=_defaultCache)]
		public HttpResponseMessage GetTaxRates(int year, int quarter)
		{
			IEnumerable<TaxRateItem> taxRates = DorTaxRateReader.GetTaxRates(new QuarterYear(year, quarter)).Select(kvp => kvp.Value);
			var response = this.Request.CreateResponse<IEnumerable<TaxRateItem>>(taxRates);
			var cache = new System.Net.Http.Headers.CacheControlHeaderValue();
			return response;
		}
	}
}
