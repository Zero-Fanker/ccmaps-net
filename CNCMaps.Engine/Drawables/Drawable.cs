using System.Collections.Generic;
using System.Drawing;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;
using NLog;

namespace CNCMaps.Engine.Drawables {
	public abstract class Drawable {
		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Name of rules section
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Index of object in it's owner collection array
		/// </summary>
		public int Index { get; set; }

		internal IniFile.IniSection Rules { get; private set; }
		internal IniFile.IniSection Art { get; private set; }
		protected readonly ModConfig _config;
		protected readonly VirtualFileSystem _vfs;
		internal ObjectCollection OwnerCollection { get; set; }
		public DrawProperties Props = new DrawProperties();
		public readonly List<Drawable> SubDrawables = new List<Drawable>();

		public bool IsRemapable { get; set; }
		public bool InvisibleInGame { get; set; }
		public Size Foundation { get; set; } = new Size(1, 1);

		public bool Overrides { get; set; }
		public bool IsWall { get; set; }
		public bool IsActualWall { get; set; }
		public bool IsGate { get; set; }
		public bool IsRubble { get; set; }
		public bool IsVeins { get; set; }
		public bool IsVeinHoleMonster { get; set; }
		public int TileElevation { get; set; }
		public bool Flat { get; set; }
		public int StartWalkFrame { get; set; }
		public int StartStandFrame { get; set; }
		public int StandingFrames { get; set; }
		public int WalkFrames { get; set; }
		public int Facings { get; set; }
		public int Ready_Start { get; set; } = 0;
		public int Ready_Count { get; set; } = 1;
		public int Ready_CountNext { get; set; } = 1;
		public bool Theater { get; set; }
		public bool IsBuildingPart = true;

		public bool IsVoxel { get; set; }
		public bool NewTheater { get; set; }
		public string Image { get; set; }
		public bool TheaterExtension { get; set; }

		protected Drawable() { }
		protected Drawable(ModConfig config, VirtualFileSystem vfs, IniFile.IniSection rules, IniFile.IniSection art) {
			_config = config;
			_vfs = vfs;
			Rules = rules;
			Art = art;
			Name = rules != null ? rules.Name : "";
		}

		public virtual void LoadFromRules() {
			LoadFromArtEssential();
			LoadFromRulesFull();
		}

		public virtual void LoadFromArtEssential() {
			Image = Art.ReadString("Image", Art.Name);
			IsVoxel = Art.ReadBool("Voxel");
			TheaterExtension = Art.ReadBool("Theater");
			NewTheater = _config.Engine >= EngineType.RedAlert2 || Art.ReadBool("NewTheater");
		}

		public virtual void LoadFromRulesFull() {
			if (Art.ReadString("Remapable") != string.Empty) {
				// does NOT work in RA2
				if (_config.Engine <= EngineType.Firestorm)
					IsRemapable = Art.ReadBool("Remapable");
			}

			// Used palet can be overriden
			bool noUseTileLandType = Rules.ReadString("NoUseTileLandType") != "";
			if (noUseTileLandType) {
				Props.PaletteType = PaletteType.Iso;
				Props.LightingType = LightingType.Full;
			}
			if (Art.ReadBool("TerrainPalette")) {
				Props.PaletteType = PaletteType.Iso;
				IsRemapable = false;
			}
			else if (Art.ReadBool("AnimPalette")) {
				Props.PaletteType = PaletteType.Anim;
				Props.LightingType = LightingType.None;
				IsRemapable = false;
			}
			else if (Art.ReadString("Palette") != string.Empty) {
				Props.PaletteType = PaletteType.Custom;
				Props.CustomPaletteName = Art.ReadString("Palette");
			}

			if (Rules.ReadString("AlphaImage") != "") {
				string alphaImageFile = Rules.ReadString("AlphaImage") + ".shp";
				if (_vfs.FileExists(alphaImageFile)) {
					var ad = new AlphaDrawable(new ShpRenderer(_config, _vfs), _vfs.Open<ShpFile>(alphaImageFile));
					ad.OwnerCollection = OwnerCollection;
					SubDrawables.Add(ad);
				}
			}

			Props.HasShadow = Art.ReadBool("Shadow", Defaults.GetShadowAssumption(OwnerCollection.Type));
			Props.HasShadow &= !Rules.ReadBool("NoShadow");
			Props.Cloakable = Rules.ReadBool("Cloakable");
			Flat = Rules.ReadBool("DrawFlat", Defaults.GetFlatnessAssumption(OwnerCollection.Type))
				|| Rules.ReadBool("Flat");

			if (Rules.ReadBool("Gate")) {
				IsGate = true;
				Flat = false;
				IsBuildingPart = true;
				Props.PaletteType = PaletteType.Unit;
				Props.FrameDecider = FrameDeciders.NullFrameDecider;
			}

			if (Rules.ReadBool("Wall")) {
				IsWall = true;
				Flat = false;
				IsBuildingPart = true;
				// RA2 walls appear a bit higher
				if (_config.Engine >= EngineType.RedAlert2) {
					Props.Offset.Offset(0, 3); // seems walls are located 3 pixels lower
				}
				Props.PaletteType = PaletteType.Unit;
				Props.LightingType = LightingType.Ambient;
				Props.FrameDecider = FrameDeciders.OverlayValueFrameDecider;
			}

			// Overlays with IsRubble are not drawn.
			if (Rules.ReadBool("IsRubble")) {
				InvisibleInGame = true;
			}
			if (Rules.ReadBool("IsVeins")) {
				Props.LightingType = LightingType.None;
				Props.PaletteType = PaletteType.Unit;
				IsVeins = true;
				Flat = true;
				Props.Offset.Y = -1; // why is this needed???
			}
			if (Rules.ReadBool("IsVeinholeMonster")) {
				Props.Offset.Y = -49; // why is this needed???
				Props.LightingType = LightingType.None;
				Props.PaletteType = PaletteType.Unit;
				IsVeinHoleMonster = true;
			}

			if (Rules.ReadString("Land") == "Rock") {
				Props.Offset.Y += _config.TileHeight / 2;
				//mainProps.ZBufferAdjust += Drawable.TileHeight / 2;
			}
			else if (Rules.ReadString("Land") == "Road") {
				Props.Offset.Y += _config.TileHeight / 2;
				// Some silly crap with low bridges not rendering.
				if (Name.ToUpper().Contains("LOBRDG") || Name.ToUpper().Contains("LOBRDB")) 
					Props.ZAdjust += _config.TileHeight;
			}
			else if (Rules.ReadString("Land") == "Railroad") {
				if (_config.Engine <= EngineType.Firestorm)
					Props.Offset.Y = 11;
				else
					Props.Offset.Y = 14;
				Props.LightingType = LightingType.Full;
				Props.PaletteType = PaletteType.Iso;
				// Foundation = new Size(2, 2); // hack to get these later in the drawing order
			}
			if (Rules.ReadBool("SpawnsTiberium")) {
				// For example on TIBTRE / Ore Poles
				Props.Offset.Y = -1;
				Props.LightingType = LightingType.None; // todo: verify it's not NONE
				Props.PaletteType = PaletteType.Unit;
			}

			Facings = Art.ReadInt("Facings", 8);
			StartStandFrame = Art.ReadInt("StartStandFrame", 0);
			StandingFrames = Art.ReadInt("StandingFrames", 0);
			StartWalkFrame = Art.ReadInt("StartWalkFrame", 0);
			WalkFrames = Art.ReadInt("WalkFrames", 0);

			Props.Offset.Offset(Art.ReadInt("XDrawOffset"), Art.ReadInt("YDrawOffset"));

			string sequence = Art.ReadString("Sequence");
			if (sequence != string.Empty) {
				IniFile.IniSection seqSection = OwnerCollection.Art.GetOrCreateSection(sequence);
				string seqReady = seqSection.ReadString("Ready");
				string[] readyParts = seqReady.Split(',');
				int start, frames, facingcount;
				if (readyParts.Length == 3 && int.TryParse(readyParts[0], out start) && int.TryParse(readyParts[1], out frames) && int.TryParse(readyParts[2], out facingcount)) {
					Ready_Start = start;
					Ready_Count = frames;
					Ready_CountNext = facingcount;
				}
			}
		}

		public abstract void Draw(GameObject obj, DrawingSurface ds, bool shadow = true);
		public virtual void DrawShadow(GameObject obj, DrawingSurface ds) { }
		public abstract Rectangle GetBounds(GameObject obj);

		private static readonly Pen BoundsRectPenVoxel = new Pen(Color.Blue);
		private static readonly Pen BoundsRectPenSHP = new Pen(Color.Red);
		private static readonly Pen BoundsRectPenISO = new Pen(Color.Purple);
		public virtual void DrawBoundingBox(GameObject obj, Graphics gfx) {
			if (IsVoxel)
				gfx.DrawRectangle(BoundsRectPenVoxel, obj.GetBounds());
			else
				gfx.DrawRectangle(BoundsRectPenSHP, obj.GetBounds());
			var top = obj.TopTile;
			var left = obj.Tile.Layer.GetTileR(obj.TopTile.Rx, obj.TopTile.Ry + obj.Drawable.Foundation.Height);
			var bottom = obj.Tile.Layer.GetTileR(obj.TopTile.Rx + obj.Drawable.Foundation.Width, obj.TopTile.Ry + obj.Drawable.Foundation.Height);
			var right = obj.Tile.Layer.GetTileR(obj.TopTile.Rx + obj.Drawable.Foundation.Width, obj.TopTile.Ry);

			List<Point> verts = new List<Point>();
			// Fail-safe because these don't always seem to get initialized properly with buildings places near edges of the map for some reason.
			if (top != null) verts.Add(new Point(top.Dx * _config.TileWidth / 2, top.Dy * _config.TileHeight / 2));
			if (left != null) verts.Add(new Point(left.Dx * _config.TileWidth / 2 - _config.TileWidth / 4, left.Dy * _config.TileHeight / 2 + _config.TileHeight / 4));
			if (bottom!= null) verts.Add(new Point(bottom.Dx * _config.TileWidth / 2, bottom.Dy * _config.TileHeight / 2 + _config.TileHeight / 2));
			if (right != null) verts.Add(new Point(right.Dx * _config.TileWidth / 2 + _config.TileHeight / 2, right.Dy * _config.TileHeight / 2 + _config.TileHeight / 4));
			if (top != null) verts.Add(new Point(top.Dx * _config.TileWidth / 2, top.Dy * _config.TileHeight / 2));

			List<Point> verts2 = new List<Point>();
			foreach (var p in verts) {
				p.Offset(30, -15);
				verts2.Add(p);
			}
			gfx.DrawLines(BoundsRectPenISO, verts2.ToArray());

		}


		internal Drawable Clone() {
			var ret = (Drawable)MemberwiseClone();
			ret.Props = this.Props.Clone();
			return ret;
		}

		public override string ToString() {
			return Name;
		}

	}
}
