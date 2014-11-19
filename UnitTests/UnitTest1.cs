using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
			const int srid = 4326;
			var targetProjection = ProjectionInfo.FromEpsgCode(srid);
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundaries(QuarterYear.Current, targetProjection);

			Assert.IsNotNull(boundaries);

			foreach (Feature feature in boundaries)
			{
				if (feature.BasicGeometry == null)
				{
					
					Assert.Fail("All features should have geometry.");
					var polygon = feature.BasicGeometry as Polygon;
					if (polygon == null)
					{
						var multipoly = feature.BasicGeometry as MultiPolygon;
						if (multipoly == null)
						{
							Assert.Fail("All geometries must be either polygon or multipolygon.");
						}
					}
					break;
				}
				if (feature.DataRow == null)
				{
					Assert.Fail("All features should have attributes.");
					break;
				} else if (feature.DataRow["LOCCODE"] == null) {
					Assert.Fail("All features should have \"LOCCODE\" attribute.");
					break;

				}
			}
		}

		[TestMethod]
		public void GetCombinedTaxBoundaryFeatures()
		{
			var boundaries = DorTaxRateReader.EnumerateLocationCodeBoundariesWithTaxRates(QuarterYear.Current);

			foreach (var feature in boundaries)
			{
				Assert.IsNotNull(feature, "No featurre should be null.");
				Assert.IsTrue(feature.DataRow.ItemArray.Length >= 8);
				CollectionAssert.AllItemsAreNotNull(feature.DataRow.ItemArray, "All columns should have values.");
			}
		}
	}
}
