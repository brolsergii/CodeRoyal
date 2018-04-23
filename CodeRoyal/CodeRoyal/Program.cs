using System;
using System.Collections.Generic;
using System.Linq;

#region Entities
enum Owner
{
    None = -1,
    Me = 0,
    Enemy = 1
}

enum StructureType
{
    None = -1,
    Mine = 0,
    Tower = 1,
    Caserne = 2
}

enum UnitType
{
    Queen = -1,
    Knight = 0,
    Archer = 1,
    Giant = 2
}

class Site
{
    public int SiteId;
    public int X;
    public int Y;
    public int Radius;
    public override string ToString() => $" ({SiteId}) [{X},{Y},{Radius}]";
}

class Building
{
    public int SiteId;
    public int Ignore1;
    public int Ignore2;
    public StructureType StructureType;
    public Owner Owner;
    public int Param1;
    public UnitType Param2;
    public override string ToString() => $" ({SiteId}) [{StructureType},{Owner},{Ignore1},{Ignore2},{Param1},{Param2}]";
}

class Unit
{
    public int X;
    public int Y;
    public Owner Owner;
    public UnitType UnitType;
    public int HP;
    public override string ToString() => $" {Owner} [{X},{Y},{HP},{UnitType}] ";
}

class Queen : Unit
{
    public int TouchId;
    public override string ToString() => $"Queen {Owner} [{X},{Y},{HP}] ->{TouchId}";
}
#endregion

class Player
{
    #region Auxilary methods
    static void TryCatch(Action a) { try { a(); } catch (Exception) { } }
    static int TryGetInt(Func<int> a) { try { return a(); } catch (Exception) { return 0; } }
    static void Deb(object o) => Console.Error.WriteLine(o);
    static void DebList(IEnumerable<object> e) => Console.Error.WriteLine(e.Aggregate((x, y) => $"{x} {y}"));
    static void DebObjList(IEnumerable<object> e) => TryCatch(() => Console.Error.WriteLine(e.Aggregate((x, y) => $"{x}\n{y}")));
    static void DebDict(Dictionary<int, int> d)
    {
        foreach (var pair in d)
            Console.Error.WriteLine($"[{pair.Key}]:{pair.Value}");
    }
    #endregion

    #region Global game state
    public const int knightsCost = 80;
    public const int archersCost = 100;
    public const int giantCost = 140;

    public static List<string> BuildHistory = new List<string>();

    public static Dictionary<int, Site> sites = new Dictionary<int, Site>();
    public static int saveX = -1;
    public static int saveY = -1;

    public static int Gold = 0;
    public static Queen Queen = new Queen();
    public static Queen EnemyQueen = new Queen();
    public static Dictionary<int, Building> buildings = new Dictionary<int, Building>();
    public static Dictionary<Owner, List<Unit>> units = new Dictionary<Owner, List<Unit>>();

    public const int optimalKnigntBarracks = 1;
    public const int optimalGiantBarracks = 0;
    public const int optimalTowers = 4;
    public const int optimalTowerHP = 400;
    public const int optimalMines = 1;
    public const int optimalMinesSize = 2;

    public const int optimalKnights = 8;
    public const int optimalGiants = 0;
    #endregion

    static double Distance(int x1, int y1, int x2, int y2) => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

    static String GetQueenAction()
    {
        bool buildingDone = false;
        // Build if close to empty spot
        int numberOfMyKnightBarracks = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                              x.Value.StructureType == StructureType.Caserne &&
                                              x.Value.Param2 == UnitType.Knight).Count();
        int numberOfMyGiantBarracks = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                              x.Value.StructureType == StructureType.Caserne &&
                                              x.Value.Param2 == UnitType.Giant).Count();
        int numberOfMyTowers = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                               x.Value.StructureType == StructureType.Tower).Count();
        int numberOfMyMines = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                              x.Value.StructureType == StructureType.Mine).Count();
        int totalMineProduction = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                                  x.Value.StructureType == StructureType.Mine).Select(x => x.Value.Param1).Sum();
        if (numberOfMyMines >= optimalMines &&
            numberOfMyKnightBarracks >= optimalKnigntBarracks &&
            numberOfMyGiantBarracks >= optimalGiantBarracks &&
            numberOfMyTowers >= optimalTowers)
        {
            buildingDone = true;
        }
        if (!buildingDone && Queen.TouchId != -1)
        {
            bool doubleAction = BuildHistory.Take(1) == BuildHistory.Skip(1).Take(1);

            if (numberOfMyMines < optimalMines || totalMineProduction < optimalMines * optimalMinesSize)
            {
                if ((buildings[Queen.TouchId].StructureType == StructureType.None || buildings[Queen.TouchId].StructureType == StructureType.Mine) &&
                    buildings[Queen.TouchId].Ignore2 > buildings[Queen.TouchId].Param1 /* can extract */)
                {
                    Deb($"Build a mine {numberOfMyMines}");
                    return $"BUILD {Queen.TouchId} MINE";
                }
            }
            if (numberOfMyKnightBarracks < optimalKnigntBarracks)
            {
                if (buildings[Queen.TouchId].StructureType == StructureType.None)
                {
                    Deb($"Build a knight barrack {numberOfMyKnightBarracks}");
                    return $"BUILD {Queen.TouchId} BARRACKS-KNIGHT";
                }
            }
            if (numberOfMyTowers < optimalTowers)
            {
                if (!doubleAction &&
                    (buildings[Queen.TouchId].StructureType == StructureType.None || buildings[Queen.TouchId].StructureType == StructureType.Tower) &&
                    (int)buildings[Queen.TouchId].Param2 < optimalTowerHP)
                {
                    Deb($"Build a tower {numberOfMyTowers}");
                    return $"BUILD {Queen.TouchId} TOWER";
                }
            }
            if (numberOfMyGiantBarracks < optimalGiantBarracks)
            {
                if (buildings[Queen.TouchId].StructureType == StructureType.None)
                {
                    Deb($"Build a giant barrack {numberOfMyGiantBarracks}");
                    return $"BUILD {Queen.TouchId} BARRACKS-GIANT";
                }
            }
        }
        if (buildingDone) // Save from the enemy
        {
            /*
            Deb($"Try to save");
            */
            var towers = buildings.Values.Where(x => x.Owner == Owner.Me && x.StructureType == StructureType.Tower)
                                         .OrderBy(x => Distance(sites[x.SiteId].X, sites[x.SiteId].Y, Queen.X, Queen.Y));
            var towerToPower = towers.Where(x => (int)x.Param2 < optimalTowerHP).FirstOrDefault();
            if (towerToPower != null)
            {
                if (Queen.TouchId == towerToPower.SiteId)
                {
                    Deb($"Power tower {towerToPower.SiteId}");
                    return $"BUILD {Queen.TouchId} TOWER";
                }
                else
                {
                    Deb($"Save to {towerToPower.SiteId}");
                    return $"MOVE {sites[towerToPower.SiteId].X} {sites[towerToPower.SiteId].Y}";
                }
            }
            return $"MOVE {saveX} {saveY}";
        }
        else // Go to closest empty site to build
        {
            Deb($"Try to go to the closest buildable spot");
            int siteId = -1;
            double distance = double.MaxValue;
            foreach (var emptySite in buildings.Where(x => x.Value.StructureType == StructureType.None))
            {
                double tmpDistance = Distance(sites[emptySite.Key].X, sites[emptySite.Key].Y, Queen.X, Queen.Y);
                if (distance > tmpDistance)
                {
                    distance = tmpDistance;
                    siteId = emptySite.Key;
                }
            }
            if (siteId != -1)
            {
                return $"MOVE {sites[siteId].X} {sites[siteId].Y}";
            }
        }
        return "WAIT"; // default action
    }

    static String GetTrainingAction()
    {
        int numberOfMyKnights = units[Owner.Me].Where(x => x.UnitType == UnitType.Knight).Count();
        int numberOfMyGiants = units[Owner.Me].Where(x => x.UnitType == UnitType.Giant).Count();
        if (numberOfMyKnights < optimalKnights)
        {
            Deb("Try build knights");
            var knightsList = new HashSet<int>();
            foreach (var knightsBarrack in buildings.Where(x => x.Value.Owner == Owner.Me &&
                                                           x.Value.StructureType == StructureType.Caserne &&
                                                           x.Value.Param1 == 0 &&
                                                           x.Value.Param2 == UnitType.Knight))
            {
                if (Gold > knightsCost)
                {
                    Gold -= knightsCost;
                    knightsList.Add(knightsBarrack.Key);
                }
            }
            return $"TRAIN" + (knightsList.Any() ? " " + string.Join(" ", knightsList) : "");
        }
        else if (numberOfMyGiants < optimalGiants)
        {
            Deb("Try build a giant");

            if (Gold > giantCost)
            {
                var giantBarrack = buildings.Where(x => x.Value.Owner == Owner.Me &&
                                                   x.Value.StructureType == StructureType.Caserne &&
                                                   x.Value.Param1 == 0 &&
                                                   x.Value.Param2 == UnitType.Giant)?.FirstOrDefault();
                if (giantBarrack != null)
                {
                    return $"TRAIN {giantBarrack.Value.Key}";
                }
            }
        }
        return "TRAIN";
    }

    static void Main(string[] args)
    {
        #region Init game state
        string[] inputs;
        int numSites = int.Parse(Console.ReadLine());
        for (int i = 0; i < numSites; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int siteId = int.Parse(inputs[0]);
            int x = int.Parse(inputs[1]);
            int y = int.Parse(inputs[2]);
            int radius = int.Parse(inputs[3]);
            sites[siteId] = new Site() { SiteId = siteId, X = x, Y = y, Radius = radius };
        }
        Deb("Sites:");
        DebList(sites.Values.ToList());
        #endregion

        while (true)
        {
            #region Init turn state
            inputs = Console.ReadLine().Split(' ');
            Gold = int.Parse(inputs[0]);
            Deb($"Gold: {Gold}");
            Queen.TouchId = int.Parse(inputs[1]);
            for (int i = 0; i < numSites; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int siteId = int.Parse(inputs[0]);
                int ignore1 = int.Parse(inputs[1]); // used in future leagues
                int ignore2 = int.Parse(inputs[2]); // used in future leagues
                int structureType = int.Parse(inputs[3]); // -1 = No structure, 2 = Barracks
                int owner = int.Parse(inputs[4]); // -1 = No structure, 0 = Friendly, 1 = Enemy
                int param1 = int.Parse(inputs[5]);
                int param2 = int.Parse(inputs[6]);
                buildings[siteId] = new Building()
                {
                    SiteId = siteId,
                    Ignore1 = ignore1,
                    Ignore2 = ignore2,
                    StructureType = (StructureType)structureType,
                    Owner = (Owner)owner,
                    Param1 = param1,
                    Param2 = (UnitType)param2
                };
            }
            Deb("Buildings:");
            DebList(buildings.Values.ToList());

            units.Clear();
            units[Owner.Me] = new List<Unit>();
            units[Owner.Enemy] = new List<Unit>();
            int numUnits = int.Parse(Console.ReadLine());
            for (int i = 0; i < numUnits; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int owner = int.Parse(inputs[2]);
                int unitType = int.Parse(inputs[3]); // -1 = QUEEN, 0 = KNIGHT, 1 = ARCHER
                int health = int.Parse(inputs[4]);
                if (unitType == -1)
                {
                    if (owner == (int)Owner.Me)
                    {
                        if (saveX == -1)
                        {
                            if (x < 500)
                            {
                                saveX = 0;
                                saveY = 0;
                            }
                            else
                            {
                                saveX = 1920;
                                saveY = 1000;
                            }
                        }
                        Queen.X = x;
                        Queen.Y = y;
                        Queen.HP = health;
                    }
                    else
                    {
                        EnemyQueen.X = x;
                        EnemyQueen.Y = y;
                        EnemyQueen.HP = health;
                    }
                }
                else
                    units[(Owner)owner].Add(new Unit()
                    {
                        X = x,
                        Y = y,
                        Owner = (Owner)owner,
                        UnitType = (UnitType)unitType,
                        HP = health
                    });
            }
            if (units[Owner.Me].Any())
            {
                Deb("My units:");
                DebList(units[Owner.Me]);
            }
            if (units[Owner.Enemy].Any())
            {
                Deb("Enemy units:");
                DebList(units[Owner.Enemy]);
            }

            Deb($"My: {Queen}");
            Deb($"Enemy: {EnemyQueen}");
            #endregion  

            // First line: A valid queen action
            var buildAction = GetQueenAction();
            BuildHistory.Insert(0, buildAction);
            Console.WriteLine(buildAction);
            // Second line: A set of training instructions
            Console.WriteLine(GetTrainingAction());
        }
    }
}