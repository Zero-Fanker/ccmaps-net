﻿using NLog;

namespace CNCMaps.FileFormats.Map {
	public class Lighting {
		public double Level { get; set; }
		public double Ambient { get; set; }
		public double Red { get; set; }
		public double Green { get; set; }
		public double Blue { get; set; }
		public double Ground { get; set; }

		static Logger logger = LogManager.GetCurrentClassLogger();

		public Lighting() {
			Level = 0.0;
			Ambient = 1.0;
			Red = 1.0;
			Green = 1.0;
			Blue = 1.0;
			Ground = 0.0;
		}

		public Lighting(IniFile.IniSection iniSection) {
			Level = iniSection.ReadDouble("Level", 0.032);
			Ambient = iniSection.ReadDouble("Ambient", 1.0);
			Red = iniSection.ReadDouble("Red", 1.0);
			Green = iniSection.ReadDouble("Green", 1.0);
			Blue = iniSection.ReadDouble("Blue", 1.0);
			Ground = iniSection.ReadDouble("Ground", 0.0);

			logger.Trace("Lighting loaded: level: {0}, ambient: {1}, red: {2}, green: {3}, blue: {4}, ground: {5}",
				Level, Ambient, Red, Green, Blue, Ground);
		}

	}
}
