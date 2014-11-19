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
	public static class Extensions
	{
		/// <summary>
		/// Converts a <see cref="DataRow"/> into an <see cref="AttributesTable"/>.
		/// </summary>
		/// <param name="dataRow">A <see cref="DataRow"/></param>
		/// <returns>An <see cref="AttributesTable"/> containing the data in the input <see cref="DataRow"/>.</returns>
		public static AttributesTable ToAttributesTable(this DataRow dataRow, IEnumerable<string> omittedFields=null)
		{
			var attributesTable = new AttributesTable();
			var columns = dataRow.Table.Columns;
			foreach (DataColumn column in columns)
			{
				if (omittedFields != null && omittedFields.Contains(column.ColumnName))
				{
					continue;
				}
				attributesTable.AddAttribute(column.ColumnName, dataRow[column]);
			}
			return attributesTable;
		}

		public static FeatureCollection ToNtsFeatureCollection(this IEnumerable<DotSpatial.Data.IFeature> boundaries, int outSR, IEnumerable<string> omittedFields = null)
		{
			var featureCollection = new FeatureCollection();
			// Omit CRS for WGS 84, which is the default for GeoJSON.
			if (outSR != 4326)
			{
				featureCollection.CRS = new NamedCRS(string.Format("urn:ogc:def:crs:EPSG::{0}", outSR));
			}
			// Get the index of the LOCCODE column.
			int locCodeColumn = boundaries.First().DataRow.Table.Columns["LOCCODE"].Ordinal;
			foreach (var boundary in boundaries)
			{
				var geometry = boundary.ToShape().ToGeoAPI();
				var locCode = (string)boundary.DataRow[locCodeColumn];
				var attributesTable = boundary.DataRow.ToAttributesTable(omittedFields);
				var feature = new Feature(geometry, attributesTable);
				featureCollection.Add(feature);
			}
			return featureCollection;
		}
	}
}