using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Linq;
using Wsdot.Dor.Tax;
using Wsdot.Dor.Tax.DataContracts;

namespace UnitTests
{
	[TestClass]
	public class UnitTest1
	{

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestQuarterYear()
		{
			var qy = new QuarterYear(new DateTime(2014, 1, 1));
			Assert.AreEqual(2014, qy.Year);
			Assert.AreEqual(1, qy.Quarter);
			Assert.IsTrue(qy.IsValid);

			qy = new QuarterYear();
			Assert.IsFalse(qy.IsValid);
		}

		[TestMethod]
		public void TestGetRates()
		{
			var taxRates = DorTaxRateReader.EnemerateTaxRates(QuarterYear.Current).ToArray();

			Assert.IsNotNull(taxRates);
			CollectionAssert.AllItemsAreNotNull(taxRates);
		}

		[TestMethod]
		public void GetLocationCodeBoundaries()
		{
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(QuarterYear.Current, null);

			Assert.IsNotNull(boundaries);

			foreach (var feature in boundaries)
			{
				Assert.IsNotNull(feature);
				if (feature.Geometry == null)
				{
					Assert.Fail("All features should have geometry.");
					var polygon = feature.Geometry as Polygon;
					if (polygon == null)
					{
						var multipoly = feature.Geometry as MultiPolygon;
						if (multipoly == null)
						{
							Assert.Fail("All geometries must be either polygon or multipolygon.");
						}
					}
					break;
				}
				else if (feature.Attributes.Count < 1)
				{
					Assert.Fail("All features should have attributes.");
					break;
				}
			}

			boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(QuarterYear.Current, 3857);
			boundaries.First();
		}
	}
}
