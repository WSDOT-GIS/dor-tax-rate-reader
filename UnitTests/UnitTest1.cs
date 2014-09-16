using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
	}
}
