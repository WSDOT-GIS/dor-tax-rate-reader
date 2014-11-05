using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;
using Wsdot.Dor.Tax;

namespace UnitTests
{
	[TestClass]
	public class UnitTest1
	{

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestGetRates()
		{
			var taxRates = DorTaxRateReader.GetTaxRates();

			Assert.IsNotNull(taxRates);
			CollectionAssert.AllItemsAreNotNull(taxRates.Values);

			string json = JsonConvert.SerializeObject(taxRates, Formatting.Indented);
			TestContext.WriteLine("{0}", json);
		}

		[TestMethod]
		public void GetLocationCodeBoundaries()
		{
			var boundaries = DorTaxRateReader.GetTaxBoundaries();

			Assert.IsNotNull(boundaries);
			CollectionAssert.AllItemsAreNotNull(boundaries);

			// Create the output JSON filename.
			string jsonFN = System.IO.Path.Combine(TestContext.DeploymentDirectory, "boundaries.json");

			// Serialize the results to JSON.
			var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
			using (var streamWriter = new StreamWriter(jsonFN))
			{
				serializer.Serialize(streamWriter, boundaries);
			}
			TestContext.AddResultFile(jsonFN);

			
		}
	}
}
