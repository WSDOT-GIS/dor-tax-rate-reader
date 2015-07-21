using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Wsdot.Dor.Tax
{
	/// <summary>
	/// Gets data from <see href="http://epsg.io"/>.
	/// </summary>
	public class EpsgRetriever
	{
		private Dictionary<int, string> _wktDict = new Dictionary<int, string>();
		private Dictionary<int, string> _xmlDict = new Dictionary<int, string>();

		private Dictionary<int, int> _exceptions = new Dictionary<int, int>();

		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		public EpsgRetriever()
		{
			// Reroute requests for 3857 to 102113.
			_exceptions.Add(3857, 102113);
			////// epsg.io
			////// Missing projection parameter 'lattitude_of_origin'
			////// It is also not defined as 'lattitude_of_center'.
			////_wktDict.Add(3857, "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]]");

			////// http://spatialreference.org/ref/sr-org/7150/ogcwkt/
			////// System.NotSupportedException: Projection Mercator_Auxiliary_Sphere is not supported.
			////_wktDict.Add(3857, "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]");

////			// http://www.epsg-registry.org/export.htm?wkt=urn:ogc:def:crs:EPSG::3857
////			// 'PROJCRS' is not recognized.
////			_wktDict.Add(3857, @"PROJCRS[""WGS 84 / Pseudo-Mercator"",
////  BASEGEODCRS[""WGS 84"",
////    DATUM[""World Geodetic System 1984"",
////      ELLIPSOID[""WGS 84"",6378137,298.257223563,LENGTHUNIT[""metre"",1.0]]]],
////  CONVERSION[""Popular Visualisation Pseudo-Mercator"",
////    METHOD[""Popular Visualisation Pseudo Mercator"",ID[""EPSG"",1024]],
////    PARAMETER[""Latitude of natural origin"",0,ANGLEUNIT[""degree"",0.01745329252]],
////    PARAMETER[""Longitude of natural origin"",0,ANGLEUNIT[""degree"",0.01745329252]],
////    PARAMETER[""False easting"",0,LENGTHUNIT[""metre"",1.0]],
////    PARAMETER[""False northing"",0,LENGTHUNIT[""metre"",1.0]]],
////  CS[cartesian,2],
////    AXIS[""easting (X)"",east,ORDER[1]],
////    AXIS[""northing (Y)"",north,ORDER[2]],
////    LENGTHUNIT[""metre"",1.0],
////  ID[""EPSG"",3857]]");

		}

		/// <summary>
		/// Gets the Well-Known Identifier (WKID) for associated with the ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<string> GetWkt(int id)
		{
			if (!_wktDict.ContainsKey(id))
			{
				var client = new HttpClient();
				string url = string.Format("http://epsg.io/{0}.wkt", id);
				var wkt = await client.GetStringAsync(url);
				_wktDict.Add(id, wkt);
			}
			return _wktDict[id];
		}

		public async Task<string> GetXml(int id)
		{
			if (!_xmlDict.ContainsKey(id))
			{
				var client = new HttpClient();
				string url = string.Format("http://epsg.io/{0}.xml", id);
				return await client.GetStringAsync(url);
			}
			return _xmlDict[id];
		}
	}
}
