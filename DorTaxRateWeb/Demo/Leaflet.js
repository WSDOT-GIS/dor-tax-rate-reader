/*global L*/
(function (L) {
	"use strict";
	var map, taxBoundariesLayer, osmLayer, layersControl, geojsonRequest;

	/**
	 * @external Feature
	 * @see {@link http://geojson.org/geojson-spec.html#feature-objects Feature}
	 */

	/**
	 * Represents a quarter of a year.
	 * @class
	 */
	function QuarterYear(/**{Date}*/ date) {
		if (!date) {
			date = new Date(Date.now());
		}
		/** @member {number} - An integer representing a year.*/
		this.year = date.getFullYear();
		/** @member {number} - An integer (1-4) representing a quarter. */
		this.quarter = Math.ceil((date.getMonth() + 1) / 3);
	}

	/**
	 * Creates an HTML table based on properties of a GeoJSON feature.
	 * @param {external:Feature} feature
	 * @returns {HTMLTableElement}
	 */
	function createPropertiesTable(feature) {
		var props = feature.properties;
		var table = document.createElement("table");
		var tbody = document.createElement("tbody");
		table.appendChild(tbody);
		var row, cell, val;
		var percentFields = /((Local)|(Rate)|(State)|(Rta))/i;
		var dateFields = /Date$/i;
		var fields = ["Name", "Rate", "LocationCode", "State", "Local", "Rta", "ExpirationDate", "EffectiveDate"];
		fields.forEach(function (name) {
			if (props.hasOwnProperty(name)) {
				row = document.createElement("tr");
				cell = document.createElement("th");
				cell.textContent = name;
				row.appendChild(cell);
				cell = document.createElement("td");
				val = props[name];
				if (percentFields.test(name)) {
					val = [Math.round(val * 1000) / 10, "%"].join("");
				} else if (dateFields.test(name)) {
					val = new Date(val).toISOString().replace(/T.+$/, "");
				}
				cell.textContent = val;
				row.appendChild(cell);
				tbody.appendChild(row);
			}
		});
		return table;
	}

	// Create the map and set the center and zoom.
	map = L.map('map').setView([47.41322033015946, -120.80566406246835], 7);

	// Add the basemap layer, which is OpenStreetMap tiles.
	osmLayer = L.tileLayer('//{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: 'Map data &copy; <a href="//openstreetmap.org">OpenStreetMap</a> contributors, <a href="//creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
		maxZoom: 18
	}).addTo(map);

	// Add the layer list control. Add the OSM basemap to the list of basemaps.
	layersControl = L.control.layers({ "OpenStreetMap": osmLayer }).addTo(map);

	// Add the scale control  to the map.
	L.control.scale().addTo(map);

	// Execute the request for the tax boundaries GeoJSON for the current quarter year.
	geojsonRequest = new XMLHttpRequest();
	var quarterYear = new QuarterYear();
	geojsonRequest.open("GET", ["../tax/boundaries/rates", quarterYear.year, quarterYear.quarter, "4326"].join("/"));
	// Specify the output format.
	geojsonRequest.setRequestHeader("Accept", "application/vnd.geo+json,application/json,text/json");
	geojsonRequest.addEventListener("loadend", function () {
		var geoJson = this.responseText;
		geoJson = JSON.parse(geoJson);
		taxBoundariesLayer = L.geoJson(geoJson, {
			onEachFeature: function (feature, layer) {
				layer.bindPopup(createPropertiesTable(feature));
			}
		}).addTo(map);
		layersControl.addOverlay(taxBoundariesLayer, "Sales Tax Boundaries");
	});
	geojsonRequest.send();
}(L));