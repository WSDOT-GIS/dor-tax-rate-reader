using DotSpatial.Data;
using DotSpatial.Projections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Wsdot.Dor.Tax.DataContracts;

namespace Wsdot.Dor.Tax
{

	public class DorTaxRateReader
	{
		const string _urlPattern = "http://dor.wa.gov/downloads/Add_Data/Rates{0}Q{1}.zip";
		// LOCCODE_PUBLIC_14Q4.zip
		const string _locCodeBoundariesShpUrlPattern = "http://dor.wa.gov/downloads/LocBounds/LOCCODE_PUBLIC_{0:yy}Q{1}.zip";
		const string _csv_pattern = "Rates{0}Q{1}.csv";
		const string _date_format = "yyyyMMdd";

		/// <summary>
		/// Enumerates through location code boundary features.
		/// </summary>
		/// <param name="quarterYear">A quarter year.</param>
		/// <param name="targetProjection">Optional. Provide to reproject from 2927.</param>
		/// <returns>Returns an <see cref="IEnumerable&lt;T&gt;"/> of <see cref="Feature"/> objects.</returns>
		public static IEnumerable<IFeature> EnumerateLocationCodeBoundaries(QuarterYear quarterYear, ProjectionInfo targetProjection = null)
		{
			var uri = new Uri(string.Format(_locCodeBoundariesShpUrlPattern, quarterYear.GetDateRange()[0], quarterYear.Quarter));
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

				foreach (var kvp in EnumerateLocationCodeBoundaries(shp_name, targetProjection))
				{
					yield return kvp;
				}

			}
			finally
			{
				dirInfo.Delete(true);
			}
		}

		/// <summary>
		/// Enumerates through location code boundary features in a shapefile.
		/// </summary>
		/// <param name="shapePath">The path to a shapefile.</param>
		/// <param name="targetProjection">Optional. Provide to reproject from 2927.</param>
		/// <returns>Returns an <see cref="IEnumerable&lt;T&gt;"/> of <see cref="Feature"/> objects.</returns>
		public static IEnumerable<IFeature> EnumerateLocationCodeBoundaries(string shapePath, ProjectionInfo targetProjection=null)
		{
			using (var fs = FeatureSet.Open(shapePath))
			{
				if (targetProjection != null)
				{
					fs.Reproject(targetProjection);
				}
				for (int i = 0, l = fs.NumRows(); i < l; i++)
				{
					var feature = fs.GetFeature(i) as IFeature;
					yield return feature;
				}
			}
			
		}

		/// <summary>
		/// Gets the tax rates for the given date. If no date is given, <see cref="DateTime.Today"/> is assumed.
		/// </summary>
		/// <param name="quarterYear">A quarter year.</param>
		/// <returns>Enumeration of <see cref="TaxRateItem"/></returns>
		public static IEnumerable<TaxRateItem> EnemerateTaxRates(QuarterYear quarterYear)
		{
			Uri uri = new Uri(string.Format(_urlPattern, quarterYear.Year, quarterYear.Quarter));

			var client = new HttpClient();
			Stream zipStream = null;
				
			client.GetStreamAsync(uri).ContinueWith(responseTask =>
			{
				zipStream = responseTask.Result;
			}).Wait();

			return EnumerateZippedTaxRateCsv(zipStream);
		}

		/// <summary>
		/// Gets the tax rates from a zipped CSV file.
		/// </summary>
		/// <param name="zipFile">ZIP file containing CSV.</param>
		/// <returns>Enumeration of <see cref="TaxRateItem"/></returns>
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

		/// <summary>
		/// Gets the tax rates from a CSV file.
		/// </summary>
		/// <param name="csvFile">CSV file.</param>
		/// <returns>Enumeration of <see cref="TaxRateItem"/></returns>
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

		public static IEnumerable<IFeature> EnumerateLocationCodeBoundariesWithTaxRates(QuarterYear quarterYear, ProjectionInfo targetProjection = null)
		{
			ILookup<string, TaxRateItem> taxRates = EnemerateTaxRates(quarterYear).ToLookup(item => item.LocationCode);
			IEnumerable<IFeature> boundaries = EnumerateLocationCodeBoundaries(quarterYear, targetProjection);
			foreach (var b in boundaries)
			{
				var taxRateInfo = taxRates[(string)b.DataRow[1]].First();
				// Add data columns.
				b.DataRow.Table.Columns.AddRange(new DataColumn[] {
					new DataColumn("Name", typeof(string)),
					new DataColumn("State", typeof(float)),
					new DataColumn("Local", typeof(float)),
					new DataColumn("Rta", typeof(float)),
					new DataColumn("Rate", typeof(float)),
					new DataColumn("EffectiveDate", typeof(DateTime)),
					new DataColumn("ExpirationDate", typeof(DateTime))
				});
				// Rename the LOCCODE column.
				b.DataRow.Table.Columns["LOCCODE"].ColumnName = "LocationCode";
				b.DataRow.SetField("Name", taxRateInfo.Name);
				b.DataRow.SetField("State", taxRateInfo.State);
				b.DataRow.SetField("Local", taxRateInfo.Local);
				b.DataRow.SetField("Rta", taxRateInfo.Rta);
				b.DataRow.SetField("Rate", taxRateInfo.Rate);
				b.DataRow.SetField("EffectiveDate", taxRateInfo.EffectiveDate);
				b.DataRow.SetField("ExpirationDate", taxRateInfo.ExpirationDate);
				yield return b;
			}
		}
	}
}
