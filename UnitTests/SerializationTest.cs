using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using System.IO;
using Wsdot.Dor.Tax;
using Wsdot.Dor.Tax.DataContracts;

namespace UnitTests
{
	[TestClass]
	public class SerializationTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestGeoJsonSerialization()
		{
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(QuarterYear.Current, null);
			var featureCollection = new FeatureCollection();
			featureCollection.CRS = new NetTopologySuite.CoordinateSystems.NamedCRS("urn:ogc:def:crs:EPSG::2927");
			foreach (var boundary in boundaries)
			{
				featureCollection.Add(boundary);
			}

			var outPath = Path.Combine(TestContext.TestRunResultsDirectory, "boundaries.json");

			using (var textWriter = new StreamWriter(outPath))
			{
				var serializer = new GeoJsonSerializer();
				serializer.Serialize(textWriter, featureCollection);
			}

			TestContext.AddResultFile(outPath);

		}
	}
}
