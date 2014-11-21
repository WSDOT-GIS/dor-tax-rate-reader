using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NetTopologySuite.CoordinateSystems;
using NetTopologySuite.CoordinateSystems.Transformations;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
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
		const int _webMercatorSrid = 3857;
		const int _wgs84Srid = 4326;

		#region Coordinate System WKT
		const string wkt2927 = @"PROJCS[""NAD83(HARN) / Washington South (ftUS)"",
    GEOGCS[""NAD83(HARN)"",
        DATUM[""NAD83_High_Accuracy_Regional_Network"",
            SPHEROID[""GRS 1980"",6378137,298.257222101,
                AUTHORITY[""EPSG"",""7019""]],
            AUTHORITY[""EPSG"",""6152""]],
        PRIMEM[""Greenwich"",0,
            AUTHORITY[""EPSG"",""8901""]],
        UNIT[""degree"",0.01745329251994328,
            AUTHORITY[""EPSG"",""9122""]],
        AUTHORITY[""EPSG"",""4152""]],
    UNIT[""US survey foot"",0.3048006096012192,
        AUTHORITY[""EPSG"",""9003""]],
    PROJECTION[""Lambert_Conformal_Conic_2SP""],
    PARAMETER[""standard_parallel_1"",47.33333333333334],
    PARAMETER[""standard_parallel_2"",45.83333333333334],
    PARAMETER[""latitude_of_origin"",45.33333333333334],
    PARAMETER[""central_meridian"",-120.5],
    PARAMETER[""false_easting"",1640416.667],
    PARAMETER[""false_northing"",0],
    AUTHORITY[""EPSG"",""2927""],
    AXIS[""X"",EAST],
    AXIS[""Y"",NORTH]]";

////		const string wktAuxSphere = @"PROJCS[""WGS_1984_Web_Mercator_Auxiliary_Sphere"",
////    GEOGCS[""GCS_WGS_1984"",
////        DATUM[""D_WGS_1984"",
////            SPHEROID[""WGS_1984"",6378137.0,298.257223563]],
////        PRIMEM[""Greenwich"",0.0],
////        UNIT[""Degree"",0.017453292519943295]],
////    PROJECTION[""Mercator_Auxiliary_Sphere""],
////    PARAMETER[""False_Easting"",0.0],
////    PARAMETER[""False_Northing"",0.0],
////    PARAMETER[""Central_Meridian"",0.0],
////    PARAMETER[""Standard_Parallel_1"",0.0],
////    PARAMETER[""Auxiliary_Sphere_Type"",0.0],
////    UNIT[""Meter"",1.0]]";


		const string wktWgs84 = @"GEOGCS[""WGS 84"",
    DATUM[""WGS_1984"",
        SPHEROID[""WGS 84"",6378137,298.257223563,
            AUTHORITY[""EPSG"",""7030""]],
        AUTHORITY[""EPSG"",""6326""]],
    PRIMEM[""Greenwich"",0,
        AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"",0.01745329251994328,
        AUTHORITY[""EPSG"",""9122""]],
    AUTHORITY[""EPSG"",""4326""]]"; 
		#endregion

		

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
			// TODO: Projected output includes Z coordinates but shouldn't.
			if (outSR != _defaultSrid && outSR != _wgs84Srid)
			{
				throw new ArgumentException(string.Format("Unsupported spatial reference:{0}", outSR), "outSR");
			}
			IEnumerable<Feature> boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(new QuarterYear(year, quarter), outSR);
			var featureCollection = new FeatureCollection();

			// Spatial reference does not need to be specified for WGS 84, as it is assumed for GeoJSON if spatial reference is unspecified.
			if (outSR != _wgs84Srid)
			{
				featureCollection.CRS = new NamedCRS(string.Format("urn:ogc:def:crs:EPSG::{0}", outSR));
			}
			ICoordinateTransformation xForm = null;
			IGeometryFactory gFactory = NetTopologySuite.Geometries.GeometryFactory.Default;
			if (outSR != _defaultSrid)
			{
				var xFormFactory = new CoordinateTransformationFactory();
				var csFactory = new CoordinateSystemFactory();
				var inCS = csFactory.CreateFromWkt(wkt2927);
				var outCS = csFactory.CreateFromWkt(wktWgs84);
				xForm = xFormFactory.CreateFromCoordinateSystems(inCS, outCS);
			}
			foreach (var boundary in boundaries)
			{
				if (xForm != null && boundary.Geometry != null)
				{
					boundary.Geometry = GeometryTransform.TransformGeometry(gFactory, boundary.Geometry, xForm.MathTransform);
				}
				featureCollection.Add(boundary);
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
