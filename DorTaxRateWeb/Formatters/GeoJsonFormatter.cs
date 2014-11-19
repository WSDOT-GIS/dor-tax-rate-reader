using NetTopologySuite.Features;
using NetTopologySuite.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Wsdot.Dor.Tax.Web.Formatters
{
	/// <summary>
	/// Writes GeoJSON.
	/// </summary>
	public class GeoJsonFormatter : BufferedMediaTypeFormatter
	{
		/// <summary>
		/// Creates a new instance of this object.
		/// </summary>
		public GeoJsonFormatter()
		{
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.geo+json"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
		}

		/// <summary>
		/// Determines if a type can be read.
		/// </summary>
		/// <param name="type"></param>
		/// <returns><see langword="false"/>.</returns>
		/// <remarks>This class cannot read any types</remarks>
		public override bool CanReadType(Type type)
		{
			return false;
		}

		/// <summary>
		/// Determines if a type can be written by this class.
		/// </summary>
		/// <param name="type">A type.</param>
		/// <returns>Returns a <see cref="bool"/> indicating if the type is supported for writing.</returns>
		public override bool CanWriteType(Type type)
		{
			if (type == typeof(FeatureCollection))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Serializes the object to the stream as GeoJson.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="writeStream"></param>
		/// <param name="content"></param>
		public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
		{
			using (var writer = new StreamWriter(writeStream) { AutoFlush = true })
			{
				var serializer = new GeoJsonSerializer();
				serializer.Serialize(writer, value);
			}
		}
	}
}