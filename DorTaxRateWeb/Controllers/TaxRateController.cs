﻿using DotSpatial.Projections;
using DotSpatial.Topology;
using NetTopologySuite.CoordinateSystems;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
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

namespace Wsdot.Dor.Tax.Web.Controllers
{
	/// <summary>
	/// Provides access to tax rate data from 
	/// <see href="http://dor.wa.gov/content/FindTaxesAndRates/Downloads.aspx">Washington State Department of Revenue</see>.
	/// </summary>
	[RoutePrefix("tax")]
	public class TaxRateController : ApiController
	{
		const int _defaultCache = 365*24*60*60*60;
		const int _defaultSrid = 2927;

		/// <summary>
		/// Gets the tax rates for a specific quarter-year year.
		/// </summary>
		/// <param name="year">A year. Minimum allowed value is 2008.</param>
		/// <param name="quarter">An integer representing a quarterYear: a value of 1 through 4. For 2008, only quarters 3 and 4 are available.</param>
		/// <returns>Returns a list of <see cref="TaxRateItem"/> objects.</returns>
		[Route("rates/{year:min(2008)}/{quarter:range(1,4)}")]
		[Route("rates/{year:min(2008)}/{quarter:range(1,4)}")]
		[CacheOutput(ServerTimeSpan=_defaultCache, ClientTimeSpan=_defaultCache)]
		public IEnumerable<TaxRateItem> GetTaxRates(int year, int quarter)
		{
			return DorTaxRateReader.EnemerateTaxRates(new QuarterYear(year, quarter));
		}

		/// <summary>
		/// Gets the current tax rates by redirecting to <see cref="GetTaxRates(int, int)"/> for the current quarter-year.
		/// </summary>
		/// <returns>An <see cref="HttpResponseMessage"/> that redirects to <see cref="GetTaxRates(int, int)"/>.</returns>
		[Route("rates")]
		public HttpResponseMessage GetCurrentTaxRates()
		{
			var qy = QuarterYear.Current;
			string newUrl = this.Request.RequestUri.ToString().TrimEnd('/') + string.Format("/{0}/{1}", qy.Year, qy.Quarter);
			var response = this.Request.CreateResponse(System.Net.HttpStatusCode.Redirect);
			response.Headers.Location = new Uri(newUrl);
			return response;
		}


		/// <summary>
		/// Gets sales tax juristiction boundaries for the given quarter-year.
		/// </summary>
		/// <param name="year">A year. Minimum allowed value is 2008.</param>
		/// <param name="quarter">An integer representing a quarterYear: a value of 1 through 4. For 2008, only quarters 3 and 4 are available.</param>
		/// <param name="outSR">The EPSG identifier for a coordinate system.</param>
		/// <returns>Returns a GeoJSON FeatureCollection.</returns>
		[Route("boundaries/{year:min(2008)}/{quarter:range(1,4)}")]
		[Route("boundaries/{year:min(2008)}/{quarter:range(1,4)}/{outSR:int}")]
		[CacheOutput(ServerTimeSpan = _defaultCache, ClientTimeSpan = _defaultCache)]
		public FeatureCollection GetSalesTaxJursitictionBoundaries(int year, int quarter, int outSR=_defaultSrid)
		{
			ProjectionInfo targetProjection = outSR == _defaultSrid ? null : ProjectionInfo.FromEpsgCode(outSR);
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(new QuarterYear(year, quarter), targetProjection);
			var featureCollection = new FeatureCollection();
			// Omit CRS for WGS 84, which is the default for GeoJSON.
			if (outSR != 4326)
			{
				featureCollection.CRS = new NamedCRS(string.Format("urn:ogc:def:crs:EPSG::{0}", outSR));
			}
			// Get the index of the LOCCODE column.
			int locCodeColumn = boundaries.First().DataRow.Table.Columns["LOCCODE"].Ordinal;
			foreach (var boundary in boundaries)
			{
				var geometry = boundary.ToShape().ToGeoAPI();
				var locCode = (string)boundary.DataRow[locCodeColumn];
				var attributesTable = new AttributesTable();
				attributesTable.AddAttribute("LocationCode", locCode);
				var feature = new Feature(geometry, attributesTable);
				featureCollection.Add(feature);
			}

			return featureCollection;
		}

		/// <summary>
		/// Gets current juristiction boundaries by redirecting to the current quarter's juristiction boundaries endpoint.
		/// </summary>
		/// <returns>An <see cref="HttpResponseMessage"/> that redirects to <see cref="GetSalesTaxJursitictionBoundaries(int, int, int)"/>.</returns>
		[Route("boundaries")]
		[Route("boundaries/current")]
		[Route("boundaries/current/{outSR:int}")]
		public HttpResponseMessage GetCurrentSalesTaxJuristictionBoundaries(int outSR=_defaultSrid)
		{
			var qy = QuarterYear.Current;
			string newUrl = this.Request.RequestUri.ToString().Replace("current", string.Empty).TrimEnd('/') + string.Format("/{0}/{1}/{2}", qy.Year, qy.Quarter, outSR);
			var response = this.Request.CreateResponse(System.Net.HttpStatusCode.Redirect);
			response.Headers.Location = new Uri(newUrl);
			return response;
		}
	}
}
