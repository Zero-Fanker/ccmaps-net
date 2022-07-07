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
		string defaultLocalizationFile = Environment.CurrentDirectory + '\\' + defaultLanguage + ".xml";

		public static string Translate(string label) {
			string ret;
			if (instance() == null) {
				return null;
			}
			if (instance().stringtable == null) {
				return null;
			}
			if (string.IsNullOrEmpty(label)) {
				return null;
			}
			instance().stringtable.TryGetValue(label, out ret);
			return !string.IsNullOrEmpty(ret) ? ret : null;
		}


		private Localizer() {
			init();
		}

		private void init() {
			loadFromFile();
		}

		private void loadFromFile() {
			try {
				var doc = XDocument.Load(defaultLocalizationFile);
				var rootNodes = doc.Root.DescendantNodes().OfType<XElement>();
				stringtable = rootNodes.ToDictionary(n => n.Name.ToString(), n => n.Value);
			} catch {

			}
		}

		private static Localizer instance() {
			if (inst == null) {
				inst = new Localizer();
			}
			return inst;
		}

		private static Localizer inst;

		StringDictionary stringtable = new StringDictionary();
	}
}
