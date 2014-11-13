using NetTopologySuite.Features;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web;

namespace DorTaxRateWeb.Formatters
{
	/// <summary>
	/// Writes GeoJSON.
	/// </summary>
	public class GeoJsonFormatter : BufferedMediaTypeFormatter
	{
		public GeoJsonFormatter()
		{
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.geo+json"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
			SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
		}

		public override bool CanReadType(Type type)
		{
			return false;
		}

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