using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Map;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using NLog;

namespace CNCMaps.Engine.Game {
	internal class TerrainDrawable : Drawable {

		private ShpDrawable terrainShp;
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public TerrainDrawable(IniFile.IniSection rules, IniFile.IniSection art)
			: base(rules, art) { }

		public override void LoadFromRules() {
			base.LoadFromRules();
		}

		public override void Draw(GameObject obj, DrawingSurface ds, bool shadows = true) {
            terrainShp = new ShpDrawable(Rules, Art);
			terrainShp.OwnerCollection = OwnerCollection;
			terrainShp.LoadFromArtEssential();
			terrainShp.Props = Props;
			terrainShp.Shp = VFS.Open<ShpFile>(terrainShp.GetFilename());

			foreach (var sub in SubDrawables.OfType<AlphaDrawable>()) {
				sub.Draw(obj, ds, false);
			}

			if (shadows)
				terrainShp.DrawShadow(obj, ds);
			terrainShp.Draw(obj, ds, false);
		}

		public override Rectangle GetBounds(GameObject obj) {
            if (InvisibleInGame) {
                return Rectangle.Empty;
            }
            if (terrainShp == null || terrainShp.Shp == null) {
                Logger.Debug("TerrainDrawable {0} image not found", Name);
                return Rectangle.Empty;
            }

			var bounds = ShpRenderer.GetBounds(obj, terrainShp.Shp, Props);
			bounds.Offset(obj.Tile.Dx * TileWidth / 2, (obj.Tile.Dy - obj.Tile.Z) * TileHeight / 2);
			bounds.Offset(Props.GetOffset(obj));
			return bounds;
		}
	}
}