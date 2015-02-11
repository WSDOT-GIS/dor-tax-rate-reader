Department of Revenue Tax Rate data importer
============================================

[![bitHound Score](https://www.bithound.io/WSDOT-GIS/dor-tax-rate-reader/badges/score.svg?)](https://www.bithound.io/WSDOT-GIS/dor-tax-rate-reader)

This project was created by Washington State Department of Transportation (WSDOT) to read [tax rate data provided by Washington State Department of Revenue].

## Requirements ##

* Visual Studio 2013 Update 4
* Microsoft .NET Framework 4.5.1 (or higher)

## Projects ##

### DorTaxRateReader ###

This project controls downloading and reading data from the [DOR website].

### DorTaxRateWeb ###

A [WebAPI] project that defines REST endpoints to access data read by the *DorTaxRateReader*.

### DorTaxDataContracts ###

This project defines common classes used in the other projects.

### GeoConversion ###

This project provides methods for converting between data types used in different topology libraries.

Uses code provided by the [NTS-DotSpatial GeometryConverter] NuGet package.


## Resources ##

### [DotSpatial] ###

Used for reading and projecting shapefiles.

### [NetTopologySuite] ###

Used for serializing shapefile content to GeoJSON.

### [ASP.NET Web API CacheOutput] ###

Controls caching of web service responses.

### [Fabrik.Common.WebAPI] ###

Enables HTTP compression of GeoJSON responses.

[DOR website]:http://dor.wa.gov/content/FindTaxesAndRates/Downloads.aspx
[tax rate data provided by Washington State Department of Revenue]:http://dor.wa.gov/content/FindTaxesAndRates/Downloads.aspx

[DotSpatial]:https://dotspatial.codeplex.com/
[NetTopologySuite]:https://github.com/NetTopologySuite/NetTopologySuite
[NTS-DotSpatial GeometryConverter]:https://www.nuget.org/packages/NetTopologySuite.DotSpatial.Converter/
[ASP.NET Web API CacheOutput]:https://github.com/filipw/AspNetWebApi-OutputCache
[WebAPI]:http://www.asp.net/web-api
[Fabrik.Common.WebAPI]:http://www.nuget.org/packages/Fabrik.Common.WebAPI/
