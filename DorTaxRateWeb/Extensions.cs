using NetTopologySuite.CoordinateSystems;
using NetTopologySuite.Features;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using DotSpatial.Topology;

namespace Wsdot.Dor.Tax.Web
{
	/// <summary>
	/// Provides extension methods.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Converts a <see cref="DataRow"/> into an <see cref="AttributesTable"/>.
		/// </summary>
		/// <param name="dataRow">A <see cref="DataRow"/></param>
		/// <param name="omittedFields">Names of fields that will be omitted.</param>
		/// <returns>An <see cref="AttributesTable"/> containing the data in the input <see cref="DataRow"/>.</returns>
		public static AttributesTable ToAttributesTable(this DataRow dataRow, IEnumerable<string> omittedFields=null, IDictionary<string, string> aliases=null)
		{
			var attributesTable = new AttributesTable();
			var columns = dataRow.Table.Columns;
			foreach (DataColumn column in columns)
			{
				if (omittedFields != null && omittedFields.Contains(column.ColumnName))
				{
					continue;
				}
				attributesTable.AddAttribute(aliases != null && aliases.ContainsKey(column.ColumnName) ? aliases[column.ColumnName] 
					: column.ColumnName, dataRow[column]);
			}
			return attributesTable;
		}

		/// <summary>
		/// Converts DotSpatial features into a NetTopologySuite features.
		/// </summary>
		/// <param name="features">DotSpatial features.</param>
		/// <param name="omittedFields">Names of fields that will be omitted.</param>
		/// <returns>NetTopologySuite features</returns>
		public static IEnumerable<Feature> AsNtsFeatures(this IEnumerable<DotSpatial.Data.IFeature> features, IEnumerable<string> omittedFields = null, IDictionary<string, string> aliases = null)
		{
			foreach (var f in features)
			{
				yield return new Feature(f.ToShape().ToGeoAPI(), f.DataRow.ToAttributesTable(omittedFields, aliases));
			}
		}

		/// <summary>
		/// Converts DotSpatial features into a NetTopologySuite feature collection.
		/// </summary>
		/// <param name="features">DotSpatial features.</param>
		/// <param name="outSR">The EPSG identifier for a spatial reference system.</param>
		/// <param name="omittedFields">Names of fields that will be omitted.</param>
		/// <returns>A NetTopologySuite feature collection</returns>
		public static FeatureCollection ToNtsFeatureCollection(this IEnumerable<DotSpatial.Data.IFeature> features, int outSR, IEnumerable<string> omittedFields = null, IDictionary<string, string> aliases = null)
		{
			var featureCollection = new FeatureCollection();
			// Omit CRS for WGS 84, which is the default for GeoJSON.
			if (outSR != 4326)
			{
				featureCollection.CRS = new NamedCRS(string.Format("urn:ogc:def:crs:EPSG::{0}", outSR));
			}
			foreach (var f in features.AsNtsFeatures(omittedFields, aliases))
			{
				featureCollection.Add(f);
			}
			return featureCollection;
		}
	}
}