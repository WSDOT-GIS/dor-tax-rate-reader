using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Wsdot.Dor.Tax.DataContracts;
using TaxRateDict = System.Collections.Generic.Dictionary<string, Wsdot.Dor.Tax.DataContracts.TaxRateItem>;

namespace Wsdot.Dor.Tax
{
	using GeoAPI.Geometries;
	using NetTopologySuite.Geometries;
	using QuarterDict = Dictionary<QuarterYear, TaxRateDict>;

	public class DorTaxRateReader
	{
		const string _urlPattern = "http://dor.wa.gov/downloads/Add_Data/Rates{0}Q{1}.zip";
		// LOCCODE_PUBLIC_14Q4.zip
		const string _locCodeBoundariesShpUrlPattern = "http://dor.wa.gov/downloads/LocBounds/LOCCODE_PUBLIC_{0:yy}Q{1}.zip";
		const string _csv_pattern = "Rates{0}Q{1}.csv";
		const string _date_format = "yyyyMMdd";

		static QuarterDict _storedRates = new QuarterDict();

		public static IEnumerable<KeyValuePair<string, IGeometry>> EnumerateLocationCodeBoundaries(DateTime date = default(DateTime))
		{
			if (date == default(DateTime))
			{
				date = DateTime.Today;
			}
			int quarter = QuarterYear.GetQuarter(date);
			var uri = new Uri(string.Format(_locCodeBoundariesShpUrlPattern, date, quarter));
			// Get the path to the TEMP directory.
			string tempDirPath = Path.GetTempPath();
			string dir = Path.Combine(tempDirPath, Path.GetRandomFileName());
			DirectoryInfo dirInfo = Directory.CreateDirectory(dir);
			string shp_name = null;

			try
			{
				var client = new HttpClient();
				client.GetStreamAsync(uri).ContinueWith(st =>
				{
					// Write the shapefile to a temporary location.
					using (var zipArchive = new ZipArchive(st.Result, ZipArchiveMode.Read))
					{
						foreach (var entry in zipArchive.Entries)
						{
							using (var outfile = File.Create(Path.Combine(dir, entry.Name)))
							using (var sourcefile = entry.Open())
							{
								sourcefile.CopyToAsync(outfile).Wait();
							}
							if (entry.Name.EndsWith(".shp", StringComparison.InvariantCultureIgnoreCase))
							{
								shp_name = Path.Combine(dir, entry.Name);
							}
						}
					}
				}).Wait();

				foreach (var kvp in EnumerateLocationCodeBoundaries(shp_name))
				{
					yield return kvp;
				}

			}
			finally
			{
				dirInfo.Delete(true);
			}
		}

		public static IEnumerable<KeyValuePair<string, IGeometry>> EnumerateLocationCodeBoundaries(string shapePath)
		{
			using (var shapefileReader = new ShapefileDataReader(shapePath, new OgcCompliantGeometryFactory()))
			{
				int locCodeId = shapefileReader.GetOrdinal("LOCCODE");

				while (shapefileReader.Read())
				{
					string locCode = shapefileReader.GetString(locCodeId);
					var shape = shapefileReader.Geometry;
					yield return new KeyValuePair<string, IGeometry>(locCode, shape);
				}
			}
		}

		public static Dictionary<string, byte[]> GetTaxBoundaries(DateTime date = default(DateTime))
		{
			var dict = new Dictionary<string, byte[]>();

			foreach (var kvp in EnumerateLocationCodeBoundaries(date))
			{
				dict.Add(kvp.Key,  kvp.Value != null ? kvp.Value.AsBinary() : null);
			}


			return dict;
		}

		/// <summary>
		/// Gets the tax rates for the given date. If no date is given, <see cref="DateTime.Today"/> is assumed.
		/// </summary>
		/// <param name="date">A date. If no date is given, <see cref="DateTime.Today"/> is assumed</param>
		/// <returns></returns>
		public static TaxRateDict GetTaxRates(DateTime date = default(DateTime))
		{
			return GetTaxRates(new QuarterYear(date));
		}

		/// <summary>
		/// Gets the tax rates for the given date. If no date is given, <see cref="DateTime.Today"/> is assumed.
		/// </summary>
		/// <param name="quarterYear">A date. If no date is given, <see cref="DateTime.Today"/> is assumed</param>
		/// <returns></returns>
		public static TaxRateDict GetTaxRates(QuarterYear quarterYear)
		{
			TaxRateDict output = null;

			if (_storedRates.ContainsKey(quarterYear))
			{
				output = _storedRates[quarterYear];
			}
			else
			{

				Uri uri = new Uri(string.Format(_urlPattern, quarterYear.Year, quarterYear.Quarter));

				var client = new HttpClient();
				client.GetStreamAsync(uri).ContinueWith(responseTask =>
				{
					output = new TaxRateDict();
					foreach (var item in EnumerateZippedTaxRateCsv(responseTask.Result))
					{
						output.Add(item.LocationCode, item);
					}
				}).Wait();

				// Store the rates for this quarter so they don't need to be retrieved again.
				_storedRates.Add(quarterYear, output);
			}



			return output;
		}

		public static IEnumerable<TaxRateItem> EnumerateZippedTaxRateCsv(Stream zipFile)
		{
			using (var zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
			{
				ZipArchiveEntry csvEntry = zipArchive.Entries[0];
				using (var csvStream = csvEntry.Open())
				{
					foreach (var item in EnumerateTaxRateCsv(csvStream))
					{
						yield return item;
					}
				}
			}
		}

		public static IEnumerable<TaxRateItem> EnumerateTaxRateCsv(Stream csvFile)
		{
			using (var streamReader = new StreamReader(csvFile))
			{
				// Skip the first CSV line of column headings.
				string line = streamReader.ReadLine();
				while (!streamReader.EndOfStream)
				{
					line = streamReader.ReadLine();
					var taxRateItem = ToTaxRateItem(line);
					yield return taxRateItem;
				}
			}
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
				LocationCode = parts[1],
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
