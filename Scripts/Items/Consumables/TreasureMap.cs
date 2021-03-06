using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.ContextMenus;
using Server.Regions;
using Server.Multis;
using Server.Engines.CannedEvil;
using Server.Misc;

namespace Server.Items
{
    public class TreasureMap : ZMapItem
    {
        private int m_Level;
        private bool m_Completed;
        private Mobile m_CompletedBy;
        private Mobile m_Decoder;
        private Map m_Map;
        private Point2D m_Location;

        public static bool NewChestLocations = Config.Get("TreasureMaps.Enabled", true);
        public static double LootChance = Config.Get("TreasureMaps.LootChance", .01);
        private static TimeSpan ResetTime = TimeSpan.FromDays(Config.Get("TreasureMaps.ResetTime", 30.0));

        public void ResetLocation()
        {
            if (!m_Completed)
            {
                ClearPins();
                LootType = LootType.Regular;
                m_Decoder = null;
                InvalidateProperties();
                NextReset = DateTime.UtcNow + ResetTime;
            }
        }

        public void GetWidthAndHeight(Map map, out int width, out int height)
        {
            if (map == Map.Trammel || map == Map.Felucca)
            {
                width = 600;
                height = 600;
            }
            if (map == Map.TerMur)
            {
                width = 200;
                height = 200;
            }
            else
            {
                width = 300;
                height = 300;
            }
        }

        public void AdjustMap(Map map, out int x2, out int y2, int x1, int y1, int width, int height)
        {
            AdjustMap(map, out x2, out y2, x1, y1, width, height, false);
        }

        public void AdjustMap(Map map, out int x2, out int y2, int x1, int y1, int width, int height, bool eodon)
        {
            x2 = x1 + width;
            y2 = y1 + height;

            if (map == Map.Trammel || map == Map.Felucca)
            {
                if (x2 >= 5120)
                    x2 = 5119;

                if (y2 >= 4096)
                    y2 = 4095;
            }
            else if (map == Map.Ilshenar)
            {
                if (x2 >= 1890)
                    x2 = 1889;

                if (x2 <= 120)
                    x2 = 121;

                if (y2 >= 1465)
                    y2 = 1464;

                if (y2 <= 105)
                    y2 = 106;
            }
            else if (map == Map.Malas)
            {
                if (x2 >= 2522)
                    x2 = 2521;

                if (x2 <= 515)
                    x2 = 516;

                if (y2 >= 1990)
                    y2 = 1989;

                if (y2 <= 0)
                    y2 = 1;
            }
            else if (map == Map.Tokuno)
            {
                if (x2 >= 1428)
                    x2 = 1427;

                if (x2 <= 0)
                    x2 = 1;

                if (y2 >= 1420)
                    y2 = 1419;

                if (y2 <= 0)
                    y2 = 1;
            }
            else if (map == Map.TerMur)
            {
                if (eodon)
                {
                    if (x2 <= 62)
                        x2 = 63;

                    if (x2 >= 960)
                        x2 = 959;

                    if (y2 <= 1343)
                        y2 = 1344;

                    if (y2 >= 2240)
                        y2 = 2239;
                }
                else
                {
                    if (x2 >= 1271)
                        x2 = 1270;

                    if (x2 <= 260)
                        x2 = 261;

                    if (y2 >= 4094)
                        y2 = 4083;

                    if (y2 <= 2760)
                        y2 = 2761;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextReset { get; set; }

        public static bool ValidateLocation(int x, int y, Map map)
        {
            var lt = map.Tiles.GetLandTile(x, y);
            var ld = TileData.LandTable[lt.ID];

            //Checks for impassable flag..cant walk, cant have a chest
            if (lt.Ignored || (ld.Flags & TileFlag.Impassable) > 0)
            {
                return false;
            }

            //Checks for roads
            for (var i = 0; i < HousePlacement.RoadIDs.Length; i += 2)
            {
                if (lt.ID >= HousePlacement.RoadIDs[i] && lt.ID <= HousePlacement.RoadIDs[i + 1])
                {
                    return false;
                }
            }

            var reg = Region.Find(new Point3D(x, y, lt.Z), map);

            //no-go in towns, houses, dungeons and champspawns
            if (reg != null)
            {
                if (reg.IsPartOf<TownRegion>() || reg.IsPartOf<DungeonRegion>() ||
                    reg.IsPartOf<ChampionSpawnRegion>() || reg.IsPartOf<HouseRegion>())
                {
                    return false;
                }
            }

            var n = (ld.Name ?? String.Empty).ToLower();

            if(Shard.DebugEnabled)
                Shard.Debug("Nome do tile: " + n);

            if (n != "dirt" && n != "grass" && n != "jungle" && n != "forest" && n != "snow")
            {
                return false;
            }

            //Rare occrunces where a static tile needs to be checked
            foreach (var tile in map.Tiles.GetStaticTiles(x, y, true))
            {
                var td = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if ((td.Flags & TileFlag.Impassable) > 0)
                {
                    return false;
                }

                n = (td.Name ?? String.Empty).ToLower();

                if (n != "dirt" && n != "grass" && n != "jungle" && n != "forest" && n != "snow")
                {
                    return false;
                }
            }

            //check for house within 5 tiles
            for (int xx = x - 5; xx <= x + 5; xx++)
            {
                for (int yy = y - 5; yy <= y + 5; yy++)
                {
                    if (BaseHouse.FindHouseAt(new Point3D(xx, yy, lt.Z), map, Region.MaxZ - lt.Z) != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public TreasureMap()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed { get { return m_Completed; } set { m_Completed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile CompletedBy { get { return m_CompletedBy; } set { m_CompletedBy = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder { get { return m_Decoder; } set { m_Decoder = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap { get { return m_Map; } set { m_Map = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation { get { return m_Location; } set { m_Location = value; } }

        private static Point2D[] m_Locations;
        private static Point2D[] m_HavenLocations;

        private static Type[][] m_SpawnTypes = new Type[][]
        {
            new Type[]{ typeof( HeadlessOne ), typeof( Skeleton ) },
            new Type[]{ typeof( Mongbat ), typeof( Ratman ), typeof( HeadlessOne ), typeof( Skeleton ), typeof( Zombie ) },
            new Type[]{ typeof( OrcishMage ), typeof( Gargoyle ), typeof( Gazer ), typeof( HellHound ), typeof( EarthElemental ) },
            new Type[]{ typeof( Lich ), typeof( OgreLord ), typeof( DreadSpider ), typeof( AirElemental ), typeof( FireElemental ) },
            new Type[]{ typeof( DreadSpider ), typeof( LichLord ), typeof( Daemon ), typeof( ElderGazer ), typeof( OgreLord ) },
            new Type[]{ typeof( LichLord ), typeof( Daemon ), typeof( ElderGazer ), typeof( PoisonElemental ), typeof( BloodElemental ) },
            new Type[]{ typeof( AncientWyrm ), typeof( Balron ), typeof( BloodElemental ), typeof( PoisonElemental ), typeof( Titan ) }
        };

        public virtual void OnMapComplete(Mobile from, TreasureMapChest chest)
        {
        }

        public virtual void OnChestOpened(Mobile from, TreasureMapChest chest)
        {
        }

        public static Point2D GetRandomClassicLocation()
        {
            if (m_Locations == null)
            {
                LoadLocations();
            }

            if (m_Locations.Length > 0)
            {
                return m_Locations[Utility.Random(m_Locations.Length)];
            }

            return Point2D.Zero;
        }

        public static Point2D GetRandomLocation()
        {
            if (m_Locations == null)
                LoadLocations();

            if (m_Locations.Length > 0)
                return m_Locations[Utility.Random(m_Locations.Length)];

            return Point2D.Zero;
        }

        public static Point2D GetRandomHavenLocation()
        {
            if (m_HavenLocations == null)
                LoadLocations();

            if (m_HavenLocations.Length > 0)
                return m_HavenLocations[Utility.Random(m_HavenLocations.Length)];

            return Point2D.Zero;
        }

        private static void LoadLocations()
        {
            string filePath = Path.Combine(Core.BaseDirectory, "Data/treasure.cfg");

            List<Point2D> list = new List<Point2D>();
            List<Point2D> havenList = new List<Point2D>();

            if (File.Exists(filePath))
            {
                using (StreamReader ip = new StreamReader(filePath))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        try
                        {
                            string[] split = line.Split(' ');

                            int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                            Point2D loc = new Point2D(x, y);
                            list.Add(loc);

                            if (IsInHavenIsland(loc))
                                havenList.Add(loc);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            m_Locations = list.ToArray();
            m_HavenLocations = havenList.ToArray();
        }

        public static bool IsInHavenIsland(IPoint2D loc)
        {
            return (loc.X >= 3314 && loc.X <= 3814 && loc.Y >= 2345 && loc.Y <= 3095);
        }

        public static BaseCreature Spawn(int level, Point3D p, bool guardian)
        {
            if (level >= 0 && level < m_SpawnTypes.Length)
            {
                BaseCreature bc;

                try
                {
                    bc = (BaseCreature)Activator.CreateInstance(m_SpawnTypes[level][Utility.Random(m_SpawnTypes[level].Length)]);
                }
                catch
                {
                    return null;
                }

                bc.Home = p;
                bc.RangeHome = 5;

                if (guardian && level == 0)
                {
                    bc.Name = "a chest guardian";
                    bc.Hue = 0x835;
                }

                return bc;
            }

            return null;
        }

        public static BaseCreature Spawn(int level, Point3D p, Map map, Mobile target, bool guardian)
        {
            if (map == null)
                return null;

            BaseCreature c = Spawn(level, p, guardian);

            if (c != null)
            {
                bool spawned = false;

                for (int i = 0; !spawned && i < 10; ++i)
                {
                    int x = p.X - 3 + Utility.Random(7);
                    int y = p.Y - 3 + Utility.Random(7);

                    if (map.CanSpawnMobile(x, y, p.Z))
                    {
                        c.MoveToWorld(new Point3D(x, y, p.Z), map);
                        spawned = true;
                    }
                    else
                    {
                        int z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            c.MoveToWorld(new Point3D(x, y, z), map);
                            spawned = true;
                        }
                    }
                }

                if (!spawned)
                {
                    c.Delete();
                    return null;
                }

                if (target != null)
                    c.Combatant = target;

                return c;
            }

            return null;
        }

        [Constructable]
        public TreasureMap(int level, Map map) : base(-1)
        {
            map = Map.Felucca;
            m_Level = level;
            m_Map = map;

            m_Location = GetRandomLocation();

            Width = 200;
            Height = 200;

            int width = 200;
            int height = 200;

            int x1 = m_Location.X - Utility.RandomMinMax(width / 4, (width / 4) * 3);
            int y1 = m_Location.Y - Utility.RandomMinMax(height / 4, (height / 4) * 3);

            if (x1 < 0)
                x1 = 0;

            if (y1 < 0)
                y1 = 0;

            int x2 = x1 + width;
            int y2 = y1 + height;

            if (x2 >= 5120)
                x2 = 5119;

            if (y2 >= 4096)
                y2 = 4095;

            x1 = x2 - width;
            y1 = y2 - height;

            Bounds = new Rectangle2D(x1, y1, width, height);
            Protected = true;

            SetDisplay(m_Location.X, m_Location.Y);
            AddWorldPin(m_Location.X, m_Location.Y);
        }

        public TreasureMap(Serial serial) : base(serial)
        {
        }

        public static bool HasDiggingTool(Mobile m)
        {
            if (m.Backpack == null)
                return false;

            List<Item> items = m.Backpack.FindItemsByType<Item>();

            foreach (Item tool in items)
            {
                if(tool is Pickaxe || tool is Shovel)
                    return true;
            }

            return false;
        }

        public virtual void OnBeginDig(Mobile from)
        {
            if (m_Completed)
            {
                from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
            }
            /*
			else if ( from != m_Decoder )
			{
				from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
			}
			*/
            else if (m_Decoder != from && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
            }
            else if (!from.CanBeginAction(typeof(TreasureMap)))
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else if (from.Map != this.m_Map)
            {
                from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public class DigTarget : Target
        {
            private TreasureMap m_Map;

            public DigTarget(TreasureMap map) : base(6, true, TargetFlags.None)
            {
                m_Map = map;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Map.Deleted)
                    return;

                Map map = m_Map.m_Map;

                if (m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
                }
                /*
				else if ( from != m_Map.m_Decoder )
				{
					from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
				}
				*/
                else if (m_Map.m_Decoder != from && !m_Map.HasRequiredSkill(from))
                {
                    from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                    return;
                }
                else if (!from.CanBeginAction(typeof(TreasureMap)))
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else if (!HasDiggingTool(from))
                {
                    from.SendMessage("Voce precisa ter uma picareta para cavar o tesouro.");
                }
                else if (from.Map != map)
                {
                    from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
                }
                else
                {
                    IPoint3D p = targeted as IPoint3D;

                    Point3D targ3D;
                    if (p is Item)
                        targ3D = ((Item)p).GetWorldLocation();
                    else
                        targ3D = new Point3D(p);

                    int maxRange;
                    double skillValue = from.Skills[SkillName.Mining].Value;

                    if (skillValue >= 100.0)
                        maxRange = 4;
                    else if (skillValue >= 91.0)
                        maxRange = 3;
                    else if (skillValue >= 81.0)
                        maxRange = 2;
                    else
                        maxRange = 1;

                    Point2D loc = m_Map.m_Location;
                    int x = loc.X, y = loc.Y;

                    Point3D chest3D0 = new Point3D(loc, 0);

                    if (Utility.InRange(targ3D, chest3D0, maxRange))
                    {
                        if (from.Location.X == x && from.Location.Y == y)
                        {
                            from.SendLocalizedMessage(503030); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            int z = map.GetAverageZ(x, y);

                            if (!map.CanFit(x, y, z, 16, true, true))
                            {
                                from.SendLocalizedMessage(503021); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction(typeof(TreasureMap)))
                            {
                                new DigTimer(from, m_Map, new Point3D(x, y, z), map).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (m_Map.Level > 0)
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                        }
                        else
                        {
                            from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                        }
                    }
                    else
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendAsciiMessage(0x44, "The treasure chest is very close!");
                        }
                        else
                        {
                            Direction dir = Utility.GetDirection(targ3D, chest3D0);

                            string sDir;
                            switch (dir)
                            {
                                case Direction.North: sDir = "north"; break;
                                case Direction.Right: sDir = "northeast"; break;
                                case Direction.East: sDir = "east"; break;
                                case Direction.Down: sDir = "southeast"; break;
                                case Direction.South: sDir = "south"; break;
                                case Direction.Left: sDir = "southwest"; break;
                                case Direction.West: sDir = "west"; break;
                                default: sDir = "northwest"; break;
                            }

                            from.SendAsciiMessage(0x44, "Try looking for the treasure chest more to the {0}.", sDir);
                        }
                    }
                }
            }
        }

        private class DigTimer : Timer
        {
            private Mobile m_From;
            private TreasureMap m_TreasureMap;

            private Point3D m_Location;
            private Map m_Map;

            private TreasureChestDirt m_Dirt1;
            private TreasureChestDirt m_Dirt2;
            private TreasureMapChest m_Chest;

            private int m_Count;

            private long m_NextSkillTime;
            private long m_NextSpellTime;
            private long m_NextActionTime;
            private long m_LastMoveTime;

            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D location, Map map) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_From = from;
                m_TreasureMap = treasureMap;

                m_Location = location;
                m_Map = map;

                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;

                Priority = TimerPriority.TenMS;
            }

            private void Terminate()
            {
                Stop();
                m_From.EndAction(typeof(TreasureMap));

                if (m_Chest != null)
                    m_Chest.Delete();

                if (m_Dirt1 != null)
                {
                    m_Dirt1.Delete();
                    m_Dirt2.Delete();
                }
            }

            protected override void OnTick()
            {
                if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime || m_NextActionTime != m_From.NextActionTime)
                {
                    Terminate();
                    return;
                }

                if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.
                    Terminate();
                    return;
                }

                int z = (m_Chest != null) ? m_Chest.Z + m_Chest.ItemData.Height : int.MinValue;
                int height = 16;

                if (z > m_Location.Z)
                    height -= (z - m_Location.Z);
                else
                    z = m_Location.Z;

                if (!m_Map.CanFit(m_Location.X, m_Location.Y, z, height, true, true, false))
                {
                    m_From.SendLocalizedMessage(503024); // You stop digging because something is directly on top of the treasure chest.
                    Terminate();
                    return;
                }

                m_Count++;

                m_From.RevealingAction();
                m_From.Direction = m_From.GetDirectionTo(m_Location);

                if (m_Count > 1 && m_Dirt1 == null)
                {
                    m_Dirt1 = new TreasureChestDirt();
                    m_Dirt1.MoveToWorld(m_Location, m_Map);

                    m_Dirt2 = new TreasureChestDirt();
                    m_Dirt2.MoveToWorld(new Point3D(m_Location.X, m_Location.Y - 1, m_Location.Z), m_Map);
                }

                if (m_Count == 5)
                {
                    m_Dirt1.Turn1();
                }
                else if (m_Count == 10)
                {
                    m_Dirt1.Turn2();
                    m_Dirt2.Turn2();
                }
                else if (m_Count > 10)
                {
                    if (m_Chest == null)
                    {
                        m_Chest = new TreasureMapChest(m_From, m_TreasureMap.Level, true);
                        m_Chest.MoveToWorld(new Point3D(m_Location.X, m_Location.Y, m_Location.Z - 15), m_Map);
                    }
                    else
                    {
                        m_Chest.Z++;
                    }

                    Effects.PlaySound(m_Chest, m_Map, 0x33B);
                }

                if (m_Chest != null && m_Chest.Location.Z >= m_Location.Z)
                {
                    Stop();
                    m_From.EndAction(typeof(TreasureMap));

                    m_Chest.Temporary = false;
                    m_Chest.Movable = true;
                    m_TreasureMap.Completed = true;
                    m_TreasureMap.CompletedBy = m_From;

                    int spawns;
                    switch (m_TreasureMap.Level)
                    {
                        case 0: spawns = 3; break;
                        case 1: spawns = 0; break;
                        default: spawns = 4; break;
                    }

                    for (int i = 0; i < spawns; ++i)
                    {
                        BaseCreature bc = Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, true);

                        if (bc != null)
                            m_Chest.Guardians.Add(bc);
                    }
                }
                else
                {
                    if (m_From.Body.IsHuman && !m_From.Mounted)
                        m_From.Animate(11, 5, 1, true, false, 0);

                    new SoundTimer(m_From, 0x125 + (m_Count % 2)).Start();
                }
            }

            private class SoundTimer : Timer
            {
                private Mobile m_From;
                private int m_SoundID;

                public SoundTimer(Mobile from, int soundID) : base(TimeSpan.FromSeconds(0.9))
                {
                    m_From = from;
                    m_SoundID = soundID;

                    Priority = TimerPriority.TenMS;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {

            if(this.m_Map != Map.Felucca)
            {
                from.SendMessage("Este mapa parece nao ter nada importante...");
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!m_Completed && m_Decoder == null)
                Decode(from);
            else
            {
                if (from.Skills.Cartography.Value > 100)
                {
                    from.QuestArrow = new QuestArrow(from, Pins[0].ToPoint3D());
                    from.QuestArrow.Update();
                } else
                {
                    from.SendMessage(78, "Quem sabe com mais de 100 de cartografia nao ficaria mais facil encontrar o local...");
                }
                DisplayTo(from);
            }
        }

        private double GetMinSkillLevel()
        {
            switch (m_Level)
            {
                case 1: return -3.0;
                case 2: return 41.0;
                case 3: return 51.0;
                case 4: return 61.0;
                case 5: return 70.0;
                case 6: return 70.0;

                default: return 0.0;
            }
        }

        public virtual bool HasRequiredSkill(Mobile from)
        {
            return (from.Skills[SkillName.Cartography].Value >= GetMinSkillLevel());
        }

        public virtual void Decode(Mobile from)
        {
            if (m_Completed || m_Decoder != null)
                return;

            double minSkill = GetMinSkillLevel();

            if (from.Skills[SkillName.Cartography].Value < minSkill)
                from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.

            double maxSkill = minSkill + 60.0;

            if (!from.CheckSkillMult(SkillName.Cartography, minSkill, maxSkill))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                return;
            }

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
            Decoder = from;

            SkillCheck.Gain(from, from.Skills.Cartography, 5);

            PlayerMobile player = from as PlayerMobile;
            //if (player != null)
            //    player.MapsDecodedThisSession++;

            DisplayTo(from);
        }

        public override void DisplayTo(Mobile from)
        {
            if (m_Completed)
            {
                SendLocalizedMessageTo(from, 503014); // This treasure hunt has already been completed.
            }
            else if (m_Decoder != from && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                return;
            }
            else
            {
                SendLocalizedMessageTo(from, 503017); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
            }

            from.PlaySound(0x249);
            base.DisplayTo(from);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (!m_Completed)
            {
                if (m_Decoder == null)
                {
                    list.Add(new DecodeMapEntry(this));
                }
                else
                {
                    bool digTool = HasDiggingTool(from);

                    list.Add(new OpenMapEntry(this));
                    list.Add(new DigEntry(this, digTool));
                }
            }
        }

        private class DecodeMapEntry : ContextMenuEntry
        {
            private TreasureMap m_Map;

            public DecodeMapEntry(TreasureMap map) : base(6147, 2)
            {
                m_Map = map;
            }

            public override void OnClick()
            {
                if (!m_Map.Deleted)
                    m_Map.Decode(Owner.From);
            }
        }

        private class OpenMapEntry : ContextMenuEntry
        {
            private TreasureMap m_Map;

            public OpenMapEntry(TreasureMap map) : base(6150, 2)
            {
                m_Map = map;
            }

            public override void OnClick()
            {
                if (!m_Map.Deleted)
                    m_Map.DisplayTo(Owner.From);
            }
        }

        private class DigEntry : ContextMenuEntry
        {
            private TreasureMap m_Map;

            public DigEntry(TreasureMap map, bool enabled) : base(6148, 2)
            {
                m_Map = map;

                if (!enabled)
                    this.Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if (m_Map.Deleted)
                    return;

                Mobile from = Owner.From;

                if (HasDiggingTool(from))
                    m_Map.OnBeginDig(from);
                else
                    from.SendMessage("Voce precisa ter a digging tool to dig for treasure.");
            }
        }

        public override int LabelNumber
        {
            get
            {
                if (m_Decoder != null)
                {
                    if (m_Level == 6)
                        return 1063453;
                    else
                        return 1041516 + m_Level;
                }
                else if (m_Level == 6)
                    return 1063452;
                else
                    return 1041510 + m_Level;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Completed)
            {
                list.Add(1041507, m_CompletedBy == null ? "someone" : m_CompletedBy.Name); // completed by ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Completed)
            {
                from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, String.Format(" completed by {0}", m_CompletedBy == null ? "someone" : m_CompletedBy.Name), ""));
            }
            else if (m_Decoder != null)
            {
                if (m_Level == 6)
                    LabelTo(from, 1063453);
                else
                    LabelTo(from, 1041516 + m_Level);
            }
            else
            {
                if (m_Level == 6)
                    LabelTo(from, 1063452);
                else
                    LabelTo(from, 1041510 + m_Level);

            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write((Mobile)m_CompletedBy);

            writer.Write(m_Level);
            writer.Write(m_Completed);
            writer.Write(m_Decoder);
            writer.Write(m_Map);
            writer.Write(m_Location);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_CompletedBy = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Level = (int)reader.ReadInt();
                        m_Completed = reader.ReadBool();
                        m_Decoder = reader.ReadMobile();
                        m_Map = reader.ReadMap();
                        m_Location = reader.ReadPoint2D();

                        if (version == 0 && m_Completed)
                            m_CompletedBy = m_Decoder;

                        break;
                    }
            }
            if (m_Decoder != null && LootType == LootType.Regular)
            {
                LootType = LootType.Blessed;
            }
        }
    }

    public class TreasureChestDirt : Item
    {
        public TreasureChestDirt() : base(0x912)
        {
            Movable = false;

            Timer.DelayCall(TimeSpan.FromMinutes(2.0), new TimerCallback(Delete));
        }

        public TreasureChestDirt(Serial serial) : base(serial)
        {
        }

        public void Turn1()
        {
            this.ItemID = 0x913;
        }

        public void Turn2()
        {
            this.ItemID = 0x914;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            Delete();
        }
    }
}
