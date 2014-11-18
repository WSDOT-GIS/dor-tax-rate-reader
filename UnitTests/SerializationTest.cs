using DotSpatial.Data;
using DotSpatial.Topology;
using Geo.IO.GeoJson;
using Geo.IO.Wkb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Wsdot.Dor.Tax;
using Wsdot.Dor.Tax.DataContracts;

namespace UnitTests
{
	[TestClass]
	public class SerializationTest
	{
		public static Geo.Abstractions.Interfaces.IGeometry ToGeoJsonGeometry(IBasicGeometry inGeometry, WkbReader reader)
		{
			// TODO: Make this work. Reading binary currently fails.
			if (inGeometry == null)
			{
				return null;
			}
			byte[] wkb = inGeometry.ToBinary();
			return reader.Read(wkb);
		}

		public static Dictionary<string, object> ToDictionary(DataRow row)
		{
			var output = new Dictionary<string, object>(row.ItemArray.Length);
			foreach (DataColumn column in row.Table.Columns)
			{
				output.Add(column.ColumnName, row[column]);
			}
			return output;
		}


		public static Geo.IO.GeoJson.Feature ToGeoJsonFeature(IFeature inFeature, WkbReader reader)
		{
			var g = ToGeoJsonGeometry(inFeature.BasicGeometry, reader);
			var attr = ToDictionary(inFeature.DataRow);
			var outFeature = new Geo.IO.GeoJson.Feature(g, attr);
			return outFeature;
		}

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestGeoJsonSerialization()
		{
			var wkbReader = new WkbReader();
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(QuarterYear.Current).Select(f => ToGeoJsonFeature(f, wkbReader));
			var featureSet = new FeatureCollection(boundaries);
			var serializer = JsonSerializer.CreateDefault();
			var outPath = Path.Combine(TestContext.TestRunResultsDirectory, "boundaries.json");

			using (var writer = new StreamWriter(outPath))
			{
				serializer.Serialize(writer, featureSet);
			}

			TestContext.AddResultFile(outPath);

			
			////var featureCollection = new FeatureCollection();
			////featureCollection.CRS = new NetTopologySuite.CoordinateSystems.NamedCRS("urn:ogc:def:crs:EPSG::2927");
			////foreach (var boundary in boundaries)
			////{
			////	featureCollection.Add(boundary);
			////}

			////var outPath = Path.Combine(TestContext.TestRunResultsDirectory, "boundaries.json");

			////using (var textWriter = new StreamWriter(outPath))
			////{
			////	var serializer = new GeoJsonSerializer();
			////	serializer.Serialize(textWriter, featureCollection);
			////}

			////TestContext.AddResultFile(outPath);

		}
	}
}
