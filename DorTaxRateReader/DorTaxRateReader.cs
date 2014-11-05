using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using NetTopologySuite.IO;
using TaxRateDict = System.Collections.Generic.Dictionary<string, Wsdot.Dor.Tax.DataContracts.TaxRateItem>;
using System.Linq;

namespace Wsdot.Dor.Tax
{
	using Wsdot.Dor.Tax.DataContracts;
	using QuarterDict = Dictionary<int, TaxRateDict>;
	using NetTopologySuite.Geometries;
	using System.Diagnostics;
	using GeoAPI.Geometries;

	public class DorTaxRateReader
	{
		const string _url_pattern = "http://dor.wa.gov/downloads/Add_Data/Rates{0}Q{1}.zip";
		// LOCCODE_PUBLIC_14Q4.zip
		const string _loc_code_boundaries_shp_url_pattern = "http://dor.wa.gov/downloads/LocBounds/LOCCODE_PUBLIC_{0:yy}Q{1}.zip";
		const string _csv_pattern = "Rates{0}Q{1}.csv";
		const string _date_format = "yyyyMMdd";

		static Dictionary<int, QuarterDict> _storedRates = new Dictionary<int, QuarterDict>();

		/// <summary>
		/// Gets the quarter for the given date.
		/// </summary>
		/// <param name="date"></param>
		/// <returns>Returns the quarter that the given month falls into (1-4).</returns>
		public static int GetQuarter(DateTime date)
		{
			double mDiv3 = date.Month / 3;
			return Convert.ToInt32(Math.Ceiling(mDiv3));
		}

		public static Dictionary<string, byte[]> GetTaxBoundaries(DateTime date = default(DateTime))
		{
			if (date == default(DateTime))
			{
				date = DateTime.Today;
			}
			int quarter = GetQuarter(date);
			var uri = new Uri(string.Format(_loc_code_boundaries_shp_url_pattern, date, quarter));
			// Get the path to the TEMP directory.
			string tempDirPath = Path.GetTempPath();
			string dir = Path.Combine(tempDirPath, Path.GetRandomFileName());
			DirectoryInfo dirInfo = Directory.CreateDirectory(dir);
			string shp_name = null;

			var dict = new Dictionary<string, byte[]>();

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

				using (var shapefileReader = new ShapefileDataReader(shp_name, new OgcCompliantGeometryFactory()))
				{
					int locCodeId = shapefileReader.GetOrdinal("LOCCODE");

					while (shapefileReader.Read())
					{
						string locCode = shapefileReader.GetString(locCodeId);
						IGeometry shape = shapefileReader.Geometry;
						dict.Add(locCode, shape != null ? shape.AsBinary() : null);
					}
				}

			}
			finally
			{
				dirInfo.Delete(true);
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
								output.Add(taxRateItem.LocationCode, taxRateItem);
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
