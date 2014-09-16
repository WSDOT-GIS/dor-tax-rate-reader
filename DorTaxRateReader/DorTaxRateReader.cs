using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using TaxRateDict = System.Collections.Generic.Dictionary<string, Wsdot.Dor.Tax.DataContracts.TaxRateItem>;

namespace Wsdot.Dor.Tax
{
	using Wsdot.Dor.Tax.DataContracts;
	using QuarterDict = Dictionary<int, TaxRateDict>;

	public class DorTaxRateReader
	{
		const string _url_pattern = "http://dor.wa.gov/downloads/Add_Data/Rates{0}Q{1}.zip";
		const string _csv_pattern = "Rates{0}Q{1}.csv";
		const string _date_format = "yyyyMMdd";

		static Dictionary<int, QuarterDict> _storedRates = new Dictionary<int, QuarterDict>();

		/// <summary>
		/// Gets the quarter for the given date.
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static int GetQuarter(DateTime date)
		{
			switch (date.Month)
			{
				case 1:
				case 2:
				case 3:
					return 1;
				case 4:
				case 5:
				case 6:
					return 2;
				case 7:
				case 8:
				case 9:
					return 3;
				case 10:
				case 11:
				case 12:
				default:
					return 4;
			}
		}

		/// <summary>
		/// Gets the tax rates for the given date. If no date is given, <see cref="DateTime.Today"/> is assumed.
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public static TaxRateDict GetTaxRates(DateTime date = default(DateTime))
		{
			if (date == default(DateTime)) {
				date = DateTime.Today;
			}
			int quarter = GetQuarter(date);

			TaxRateDict output = null;

			if (_storedRates.ContainsKey(date.Year) && _storedRates[date.Year].ContainsKey(quarter))
			{
				output = _storedRates[date.Year][quarter];
			}
			else
			{

				Uri uri = new Uri(string.Format(_url_pattern, date.Year, quarter));

				var client = new HttpClient();
				client.GetStreamAsync(uri).ContinueWith(responseTask =>
				{
					output = new TaxRateDict();
					using (var zipArchive = new ZipArchive(responseTask.Result, ZipArchiveMode.Read))
					{
						ZipArchiveEntry csvEntry = zipArchive.GetEntry(string.Format(_csv_pattern, date.Year, quarter));
						using (var csvStream = csvEntry.Open())
						using (var streamReader = new StreamReader(csvStream))
						{
							// Skip the first CSV line of column headings.
							string line = streamReader.ReadLine();
							while (!streamReader.EndOfStream)
							{
								line = streamReader.ReadLine();
								var taxRateItem = ToTaxRateItem(line);
								output.Add(taxRateItem.Code, taxRateItem);
							}
						}
					}
				}).Wait();

				// Store the rates for this quarter so they don't need to be retrieved again.
				if (!_storedRates.ContainsKey(date.Year))
				{
					_storedRates.Add(date.Year, new QuarterDict());
				}
				_storedRates[date.Year][quarter] = output;
			}



			return output;
		}

		/// <summary>
		/// Converts a line from a CSV file into a <see cref="TaxRateItem"/>.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		private static TaxRateItem ToTaxRateItem(string line)
		{
			var parts = line.Split(new char[] { ',' }, StringSplitOptions.None);
			var taxRateItem = new TaxRateItem
			{
				Name = parts[0],
				Code = parts[1],
				State = float.Parse(parts[2]),
				Local = float.Parse(parts[3]),
				Rta = float.Parse(parts[4]),
				Rate = float.Parse(parts[5]),
				EffectiveDate = DateTime.ParseExact(parts[6], _date_format, DateTimeFormatInfo.CurrentInfo),
				ExpirationDate = DateTime.ParseExact(parts[7], _date_format, DateTimeFormatInfo.CurrentInfo),
			};
			return taxRateItem;
		}
	}
}
