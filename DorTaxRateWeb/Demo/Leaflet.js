﻿/*global L*/
(function (L) {
	"use strict";
	var map, taxBoundariesLayer, osmLayer, layersControl, geojsonRequest;

	function QuarterYear(date) {
		if (!date) {
			date = new Date(Date.now());
		}
		this.year = date.getFullYear();
		this.quarter = Math.ceil((date.getMonth() + 1) / 3);
	}

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

	map = L.map('map').setView([47.41322033015946, -120.80566406246835], 7);

	osmLayer = L.tileLayer('//{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
		attribution: 'Map data &copy; <a href="//openstreetmap.org">OpenStreetMap</a> contributors, <a href="//creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>',
		maxZoom: 18
	}).addTo(map);

	layersControl = L.control.layers({ "OpenStreetMap": osmLayer }).addTo(map);

	L.control.scale().addTo(map);

	geojsonRequest = new XMLHttpRequest();
	var quarterYear = new QuarterYear();
	geojsonRequest.open("GET", ["../tax/boundaries/rates", quarterYear.year, quarterYear.quarter, "4326"].join("/"));
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