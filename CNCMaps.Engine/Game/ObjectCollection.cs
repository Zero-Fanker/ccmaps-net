﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Game {

	public class ObjectCollection : GameCollection {
		public readonly string[] FireNames;
		public readonly PaletteCollection Palettes;

		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public ObjectCollection(CollectionType type, TheaterType theater, ModConfig config, VirtualFileSystem vfs, IniFile rules, IniFile art,
			IniFile.IniSection objectsList, PaletteCollection palettes)
			: base(type, theater, config, vfs, rules, art) {

			Palettes = palettes;
			if (_config.Engine >= EngineType.RedAlert2) {
				string fireNames = Rules.ReadString(_config.Engine == EngineType.RedAlert2 ? "AudioVisual" : "General",
					"DamageFireTypes", "FIRE01,FIRE02,FIRE03");
				FireNames = fireNames.Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
			}

			foreach (var entry in objectsList.OrderedEntries) {
				if (!string.IsNullOrEmpty(entry.Value)) {
					Logger.Trace("Loading object {0}.{1}", objectsList.Name, entry.Value);
					AddObject(entry.Value);
				}
			}
		}

		protected override Drawable MakeDrawable(string objName) {
			Drawable drawable;
			var rulesSection = Rules.GetOrCreateSection(objName);
			string artSectionName = rulesSection.ReadString("Image", objName);
			var artSection = Art.GetOrCreateSection(artSectionName);

			switch (Type) {
				case CollectionType.Aircraft:
				case CollectionType.Vehicle:
					drawable = new UnitDrawable(_config, _vfs, rulesSection, artSection);
					break;
				case CollectionType.Building:
					drawable = new BuildingDrawable(_config, _vfs, rulesSection, artSection);
					break;
				case CollectionType.Infantry:
				case CollectionType.Overlay:
				case CollectionType.Smudge:
					drawable = new ShpDrawable(_config, _vfs, rulesSection, artSection);
					break;
				case CollectionType.Terrain:
					drawable = new TerrainDrawable(_config, _vfs, rulesSection, artSection);
					break;
				case CollectionType.Animation:
					drawable = new AnimDrawable(_config, _vfs, rulesSection, artSection);
					break;
				default:
					throw new InvalidEnumArgumentException();
			}
			return drawable;
		}

		protected override void LoadDrawable(Drawable drawable) {
			switch (Type) {
				case CollectionType.Aircraft:
				case CollectionType.Vehicle:
					LoadUnitDrawable((UnitDrawable)drawable);
					break;
				case CollectionType.Building:
					LoadBuildingDrawable((BuildingDrawable)drawable);
					break;
				case CollectionType.Infantry:
				case CollectionType.Overlay:
				case CollectionType.Smudge:
					LoadSimpleDrawable((ShpDrawable)drawable);
					break;
				case CollectionType.Terrain:
					LoadTerrainDrawable((TerrainDrawable)drawable);
					break;
				case CollectionType.Animation:
					LoadAnimDrawable((AnimDrawable)drawable);
					break;
				default:
					throw new InvalidEnumArgumentException();
			}

			// overrides from the modconfig
			var cfgOverrides = _config.ObjectOverrides.Where(ovr =>
                // matches collection
                (ovr.CollectionTypes & Type) == Type &&
                // matches collection
                (ovr.TheaterTypes & Theater) == Theater &&
                // matches object regex
                Regex.IsMatch(drawable.Name, ovr.ObjRegex, RegexOptions.IgnoreCase))
				.OrderByDescending(o => o.Priority);

			foreach (var cfgOverride in cfgOverrides) {
				Logger.Debug("Object {0} receives overrides from regex {1}", drawable.Name, cfgOverride.ObjRegex);

				if (cfgOverride.Lighting != LightingType.Default)
					drawable.Props.LightingType = cfgOverride.Lighting;

				if (cfgOverride.Palette != PaletteType.Default) {
					drawable.Props.PaletteType = cfgOverride.Palette;
					drawable.Props.CustomPaletteName = cfgOverride.CustomPaletteFile;
				}
			}
		}

		private void LoadAnimDrawable(AnimDrawable anim) {
			InitDrawableDefaults(anim);
			anim.LoadFromRules();
			anim.Shp = _vfs.Open<ShpFile>(anim.GetFilename());
		}

		private void InitDrawableDefaults(Drawable drawable) {
			drawable.OwnerCollection = this;
			drawable.Props.PaletteType = Defaults.GetDefaultPalette(Type, _config.Engine);
			drawable.Props.LightingType = Defaults.GetDefaultLighting(Type);
			drawable.IsRemapable = Defaults.GetDefaultRemappability(Type, _config.Engine);
			drawable.Props.FrameDecider = Defaults.GetDefaultFrameDecider(Type);

			// apply collection-specific offsets
			switch (Type) {
				case CollectionType.Building:
				case CollectionType.Overlay:
				case CollectionType.Smudge:
					drawable.Props.Offset.Offset(_config.TileWidth / 2, 0);
					break;
				case CollectionType.Terrain:
				case CollectionType.Vehicle:
				case CollectionType.Infantry:
				case CollectionType.Aircraft:
				case CollectionType.Animation:
					drawable.Props.Offset.Offset(_config.TileWidth / 2, _config.TileHeight / 2);
					break;
			}
		}

		private void LoadTerrainDrawable(TerrainDrawable drawable) {
			InitDrawableDefaults(drawable);
			drawable.LoadFromRules();
		}

		private void LoadSimpleDrawable(ShpDrawable drawable) {
			InitDrawableDefaults(drawable);
			drawable.LoadFromRules();

			string shpFile = drawable.GetFilename();
			drawable.Shp = _vfs.Open<ShpFile>(shpFile);

			if (Type == CollectionType.Smudge)
				drawable.Foundation = new Size(drawable.Rules.ReadInt("Width", 1), drawable.Rules.ReadInt("Height", 1));

			if (Type == CollectionType.Overlay)
				LoadOverlayDrawable(drawable);
		}

		private void LoadBuildingDrawable(BuildingDrawable drawable) {
			InitDrawableDefaults(drawable);
			drawable.LoadFromRules();
		}

		private void LoadUnitDrawable(UnitDrawable drawable) {
			InitDrawableDefaults(drawable);
			drawable.LoadFromRules();
		}

		private void LoadOverlayDrawable(ShpDrawable drawable) {
			var ovl = new OverlayObject((byte)drawable.Index, 0);
			var tibType = SpecialOverlays.GetOverlayTibType(ovl, _config.Engine);
			DrawProperties props = drawable.Props;

			if (_config.Engine >= EngineType.RedAlert2) {
				if (tibType != OverlayTibType.NotSpecial) {
					props.FrameDecider = FrameDeciders.OverlayValueFrameDecider;
					props.PaletteType = PaletteType.Overlay;
					props.LightingType = LightingType.None;
				}
				else if (SpecialOverlays.IsHighBridge(ovl)) {
					props.OffsetHack = OffsetHacks.RA2BridgeOffsets;
					props.ShadowOffsetHack = OffsetHacks.RA2BridgeShadowOffsets;
					drawable.TileElevation = 4; // for lighting
					drawable.Foundation = new Size(3, 1); // ensures they're drawn later --> fixes overlap
				}
			}
			else if (_config.Engine <= EngineType.Firestorm) {
				if (tibType != OverlayTibType.NotSpecial) {
					props.FrameDecider = FrameDeciders.OverlayValueFrameDecider;
					props.PaletteType = PaletteType.Unit;
					props.LightingType = LightingType.None;
					drawable.IsRemapable = true;
				}
				else if (SpecialOverlays.IsHighBridge(ovl) || SpecialOverlays.IsTSHighRailsBridge(ovl)) {
					props.OffsetHack = OffsetHacks.TSBridgeOffsets;
					props.ShadowOffsetHack = OffsetHacks.TSBridgeShadowOffsets;
					drawable.TileElevation = 4; // for lighting
												//drawable.Foundation = new Size(3, 1); // ensures they're drawn later --> fixes overlap
				}
			}
		}

		public string ApplyNewTheaterIfNeeded(string artName, string imageFileName) {
			if (_config.Engine <= EngineType.Firestorm) {
				// the tag will only work if the ID for the object starts with either G, N or C and its second letter is A (for Arctic/Snow theater) or T (for Temperate theater)
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else if (_config.Engine == EngineType.RedAlert2) {
				// In RA2, for the tag to work, it must start with either G, N or C, and its second letter must be A, T or U (Urban theater). 
				if (new[] { 'G', 'N', 'C' }.Contains(artName[0]) && new[] { 'A', 'T', 'U' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			else {
				//  In Yuri's Revenge, the ID can also start with Y."
				// It can also use D, L & N as theater ID's.
				// Ares allows use of any letter as the first letter. This is an experimental change seeing if enabling this behaviour without checking if Ares is in use
				// or not leads to detrimental effects.
				// if (new[] { 'G', 'N', 'C', 'Y' }.Contains(artName[0]) && new[] { 'A', 'T', 'U', 'D', 'L', 'N' }.Contains(artName[1]))
				if (new[] { 'A', 'T', 'U', 'D', 'L', 'N' }.Contains(artName[1]))
					ApplyNewTheater(ref imageFileName);
			}
			return imageFileName;
		}

		private void ApplyNewTheater(ref string imageFileName) {
			var sb = new StringBuilder(imageFileName);

			sb[1] = ModConfig.ActiveTheater.NewTheaterChar;
			if (!_vfs.FileExists(sb.ToString())) {
				sb[1] = 'G'; // generic
				if (!_vfs.FileExists(sb.ToString()))
					sb[1] = imageFileName[1]; // fallback to original
			}
			imageFileName = sb.ToString();
		}

	}
}
