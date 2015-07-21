using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NetTopologySuite.CoordinateSystems.Transformations;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
		const string _dorSRWkt = "PROJCS[\"NAD83(HARN) / Washington South (ftUS)\",GEOGCS[\"NAD83(HARN)\",DATUM[\"NAD83_High_Accuracy_Reference_Network\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6152\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4152\"]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"standard_parallel_1\",47.33333333333334],PARAMETER[\"standard_parallel_2\",45.83333333333334],PARAMETER[\"latitude_of_origin\",45.33333333333334],PARAMETER[\"central_meridian\",-120.5],PARAMETER[\"false_easting\",1640416.667],PARAMETER[\"false_northing\",0],UNIT[\"US survey foot\",0.3048006096012192,AUTHORITY[\"EPSG\",\"9003\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],AUTHORITY[\"EPSG\",\"2927\"]]";
		private static EpsgRetriever _epsg = new EpsgRetriever();

		/// <summary>
		/// Enumerates through location code boundary features.
		/// </summary>
		/// <param name="quarterYear">A quarter year.</param>
		/// <param name="outCS">EPSG WKID of a coordinate system. Defaults to 2927 if omitted</param>
		/// <returns>Returns an <see cref="IEnumerable&lt;T&gt;"/> of <see cref="Feature"/> objects.</returns>
		public static IEnumerable<Feature> EnumerateLocationCodeBoundaries(QuarterYear quarterYear, int outCS = 2927)
		{
			ICoordinateSystem cs = null;
			if (outCS != 2927)
			{
				_epsg.GetWkt(outCS).ContinueWith(t =>
				{
					string wkt = t.Result;
					var csFactory = new CoordinateSystemFactory();
					cs = csFactory.CreateFromWkt(wkt);
				}).Wait();
			}
			return EnumerateLocationCodeBoundaries(quarterYear, cs);
		}

		/// <summary>
		/// Enumerates through location code boundary features.
		/// </summary>
		/// <param name="quarterYear">A quarter year.</param>
		/// <returns>Returns an <see cref="IEnumerable&lt;T&gt;"/> of <see cref="Feature"/> objects.</returns>
		public static IEnumerable<Feature> EnumerateLocationCodeBoundaries(QuarterYear quarterYear, ICoordinateSystem outCS=null)
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
					var zipStream = st.Result;
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

				// If the user specified an output coordinate system, set up transformation.
				ICoordinateTransformation xForm = null;
				IGeometryFactory gFactory = null;
				if (outCS != null) {
					var xFormFactory = new CoordinateTransformationFactory();
					var csFactory = new CoordinateSystemFactory();
					var inCS = csFactory.CreateFromWkt(_dorSRWkt);
					xForm = xFormFactory.CreateFromCoordinateSystems(inCS, outCS);
					gFactory = GeometryFactory.Default;
				}
				foreach (var feature in EnumerateLocationCodeBoundaries(shp_name))
				{
					// Project the geometry if a transformation was specified.
					if (xForm != null)
					{
						var projectedGeometry = GeometryTransform.TransformGeometry(gFactory, feature.Geometry, xForm.MathTransform);
						feature.Geometry = projectedGeometry;
					}
					yield return feature;
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
		/// <returns>Returns an <see cref="IEnumerable&lt;T&gt;"/> of <see cref="Feature"/> objects.</returns>
		protected static IEnumerable<Feature> EnumerateLocationCodeBoundaries(string shapePath)
		{
			using (var shapefileReader = new ShapefileDataReader(shapePath, GeometryFactory.Default))
			{
				int locCodeId = shapefileReader.GetOrdinal("LOCCODE");

				while (shapefileReader.Read())
				{
					string locCode = shapefileReader.GetString(locCodeId);
					var shape = shapefileReader.Geometry;
					var attributes = new AttributesTable();
					attributes.AddAttribute("LocationCode", locCode);
					yield return new Feature(shape, attributes);
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
		protected static IEnumerable<TaxRateItem> EnumerateZippedTaxRateCsv(Stream zipFile)
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
		protected static IEnumerable<TaxRateItem> EnumerateTaxRateCsv(Stream csvFile)
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
			var parts = line.Split(new char[] { ',', '\t' }, StringSplitOptions.None);
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
