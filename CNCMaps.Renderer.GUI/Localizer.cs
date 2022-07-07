using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CNCMaps.GUI {
	using StringDictionary = Dictionary<string, string>;
	public class Localizer {
		const string defaultLanguage = "ZH-CN";
		string defaultLocalizationFile = Environment.CurrentDirectory + '/' + defaultLanguage + ".xml";

		public static string Translate(string label) {
			string ret;
			if (inst == null) {
				System.Windows.Forms.MessageBox.Show("inst is null");
			}
			if (inst.stringtable == null) {
				System.Windows.Forms.MessageBox.Show("String table is null");
			}
			if (string.IsNullOrEmpty(label)) {
				System.Windows.Forms.MessageBox.Show("Label is null");
			}
			inst.stringtable.TryGetValue(label, out ret);
			return ret.Length > 0 ? ret : label;
		}


		private Localizer() {
			init();
		}

		private void init() {
			loadFromFile();
		}

		private void loadFromFile() {
			var doc = XDocument.Load(defaultLocalizationFile);
			var rootNodes = doc.Root.DescendantNodes().OfType<XElement>();
			stringtable = rootNodes.ToDictionary(n => n.Name.ToString(), n => n.Value);
		}

		private static readonly Localizer inst = new Localizer();

		StringDictionary stringtable = new StringDictionary();
	}
}
