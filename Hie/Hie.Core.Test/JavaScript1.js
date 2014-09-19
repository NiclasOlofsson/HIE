﻿
var json = {
	'MSH': {
		'F1': 'James Newton-King',
		'F2': 'http://james.newtonking.com',
		'F3': 'James Newton-Kings blog.',
		'F4': [
			{
				'C1': 'Json.NET 1.3 + New license + Now on CodePlex',
				'C2': 'Annoucing the release of Json.NET 1.3, the MIT license and the source on CodePlex',
				'C3': 'http://james.newtonking.com/projects/json-net.aspx',
				'C4': [
					'Json.NET',
					'CodePlex'
				]
			}
		]
	}
};

var test = json.MSH.F4;
var shit = test.C1;

json.MSH.F1.C1.forEach(function(element, index, array) {
	element.remove();
});


var t = {
	"resourceType": "Observation",
	"text": {
		"status": "generated",
		"div": "<div>\n      <p>\n        <b>Generated Narrative</b>\n      </p>\n      <p>\n        <b>name</b>: \n        <span title=\"Codes: {http://loinc.org 11557-6}\">Carbon dioxide [Partial pressure] in Blood</span>\n      </p>\n      <p>\n        <b>value[x]</b>: 6.2 mm[Hg]\n      </p>\n      <p>\n        <b>interpretation</b>: \n        <span title=\"Codes: {http://hl7.org/fhir/v2/0078 A}\">abnormal</span>\n      </p>\n      <p>\n        <b>applies[x]</b>: 2-Apr 2013 10:30 --&gt; 5-Apr 2013 10:30\n      </p>\n      <p>\n        <b>issued</b>: 3-Apr 2013 15:30\n      </p>\n      <p>\n        <b>status</b>: final_\n      </p>\n      <p>\n        <b>reliability</b>: ok\n      </p>\n      <p>\n        <b>bodySite</b>: \n        <span title=\"Codes: {http://snomed.info/sct 308046002}\">Superficial forearm vein</span>\n      </p>\n      <p>\n        <b>method</b>: \n        <span title=\"Codes: {http://snomed.info/sct 120220003}\">Injection to forearm</span>\n      </p>\n      <p>\n        <b>identifier</b>: 6325 (official)\n      </p>\n      <p>\n        <b>subject</b>: P. van de Heuvel\n      </p>\n      <p>\n        <b>performer</b>: A. Langeveld\n      </p>\n      <h3>ReferenceRanges</h3>\n      <table class=\"grid\">\n        <tr>\n          <td>\n            <b>Low</b>\n          </td>\n          <td>\n            <b>High</b>\n          </td>\n          <td>\n            <b>Meaning</b>\n          </td>\n          <td>\n            <b>Age</b>\n          </td>\n        </tr>\n        <tr>\n          <td>7.1 mmol/l</td>\n          <td>11.2 mmol/l</td>\n          <td> </td>\n          <td> </td>\n        </tr>\n      </table>\n    </div>"
	},
	"name": {
		"coding": [
			{
				"system": "http://loinc.org",
				"code": "11557-6",
				"display": "Carbon dioxide [Partial pressure] in Blood"
			}
		]
	},
	"valueQuantity": {
		"value": 6.2,
		"units": "mm[Hg]",
		"system": "http://unitsofmeasure.org",
		"code": "mm[Hg]"
	},
	"interpretation": {
		"coding": [
			{
				"system": "http://hl7.org/fhir/v2/0078",
				"code": "A",
				"display": "abnormal"
			}
		]
	},
	"appliesPeriod": {
		"start": "2013-04-02T10:30:10+01:00",
		"end": "2013-04-05T10:30:10+01:00"
	},
	"issued": "2013-04-03T15:30:10+01:00",
	"status": "final",
	"reliability": "ok",
	"bodySite": {
		"coding": [
			{
				"system": "http://snomed.info/sct",
				"code": "308046002",
				"display": "Superficial forearm vein"
			}
		]
	},
	"method": {
		"coding": [
			{
				"system": "http://snomed.info/sct",
				"code": "120220003",
				"display": "Injection to forearm"
			}
		]
	},
	"identifier": {
		"use": "official",
		"system": "http://www.bmc.nl/zorgportal/identifiers/observations",
		"value": "6325"
	},
	"subject": {
		"reference": "Patient/f001",
		"display": "P. van de Heuvel"
	},
	"performer": [
		{
			"reference": "Practitioner/f005",
			"display": "A. Langeveld"
		}
	],
	"referenceRange": [
		{
			"low": {
				"value": 7.1,
				"units": "mmol/l",
				"system": "http://unitsofmeasure.org",
				"code": "mmol/l"
			},
			"high": {
				"value": 11.2,
				"units": "mmol/l",
				"system": "http://unitsofmeasure.org",
				"code": "mmol/l"
			}
		}
	]
}