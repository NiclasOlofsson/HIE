namespace Hie.Core
{
	public class TestUtils
	{
		public static string BuildHl7JsonString()
		{
			string retJson =
				"{" +
				"  'HL7Message': {" +
				"    'MSH': {" +
				"      'MSH.1': '|'," +
				"      'MSH.2': '^~\\&'," +
				"      'MSH.3': { 'MSH.3.1': 'ULTRAGENDAPRO' }," +
				"      'MSH.4': { 'MSH.4.1': 'BMI' }," +
				"      'MSH.5': { 'MSH.5.1': 'WinPath HL7' }," +
				"      'MSH.6': { 'MSH.6.1': 'WinPath HL7' }," +
				"      'MSH.7': { 'MSH.7.1': '20120723020203' }," +
				"      'MSH.9': {" +
				"        'MSH.9.1': 'ADT'," +
				"        'MSH.9.2': 'A01'" +
				"      }," +
				"      'MSH.10': { 'MSH.10.1': '634786057279082040' }," +
				"      'MSH.11': { 'MSH.11.1': 'P' }," +
				"      'MSH.12': { 'MSH.12.1': '2.3' }," +
				"      'MSH.15': { 'MSH.15.1': 'NE' }," +
				"      'MSH.16': { 'MSH.16.1': 'AL' }," +
				"      'MSH.18': { 'MSH.18.1': 'ASCII' }" +
				"    }," +
				"    'EVN': {" +
				"      'EVN.1': { 'EVN.1.1': 'A01' }," +
				"      'EVN.2': { 'EVN.2.1': '20120723020203' }" +
				"    }," +
				"    'PID': {" +
				"      'PID.3': {" +
				"        'PID.3.1': 'CCH3194057'," +
				"        'PID.3.5': 'PASID'" +
				"      }," +
				"      'PID.4': { 'PID.4.1': '11242757' }," +
				"      'PID.5': {" +
				"        'PID.5.1': 'Surnom1'," +
				"        'PID.5.2': 'Forename1'," +
				"        'PID.5.5': 'Mrs'" +
				"      }," +
				"      'PID.7': { 'PID.7.1': '19580525000000' }," +
				"      'PID.8': { 'PID.8.1': 'F' }," +
				"      'PID.9': { 'PID.9.7': 'PG' }," +
				"      'PID.11': {" +
				"        'PID.11.1': 'Add1'," +
				"        'PID.11.2': 'add2'," +
				"        'PID.11.3': 'town'," +
				"        'PID.11.5': 'postcode'," +
				"        'PID.11.7': 'P'" +
				"      }," +
				"      'PID.13': {" +
				"        'PID.13.10': '~'," +
				"        'PID.13.12': 'FX'" +
				"      }," +
				"      'PID.30': { 'PID.30.1': '0' }" +
				"    }," +
				"    'PV1': {" +
				"      'PV1.2': { 'PV1.2.1': 'I' }," +
				"      'PV1.3': {" +
				"        'PV1.3.1': 'CCHWDDOW'," +
				"        'PV1.3.2': 'Ward Downing'," +
				"        'PV1.3.7': 'CCH'," +
				"        'PV1.3.9': 'The Clementine Churchill Hospital'" +
				"      }," +
				"      'PV1.9': {" +
				"        'PV1.9.1': 'CBMI7655'," +
				"        'PV1.9.2': 'Pathology Interface'," +
				"        'PV1.9.3': 'CCH'," +
				"        'PV1.9.9': '~CBMI7655'" +
				"      }," +
				"      'PV1.10': { 'PV1.10.1': 'INP' }," +
				"      'PV1.19': { 'PV1.19.1': '37436686' }," +
				"      'PV1.44': { 'PV1.44.1': '20120723' }" +
				"    }" +
				"  }" +
				"}";

			return retJson;
		}
	}
}
