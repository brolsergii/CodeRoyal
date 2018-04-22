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
    Caserne = 2
}

enum UnitType
{
    Queen = -1,
    Knight = 0,
    Archer = 1
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
    public StructureType StructureType;
    public Owner Owner;
    public int CoolDown;
    public UnitType UnitType;
    public override string ToString() => $" ({SiteId}) [{StructureType},{Owner},{CoolDown},{UnitType}]";
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

    public static Dictionary<int, Site> sites = new Dictionary<int, Site>();

    public static int Gold = 0;
    public static Queen Queen = new Queen();
    public static Queen EnemyQueen = new Queen();
    public static Dictionary<int, Building> buildings = new Dictionary<int, Building>();
    public static List<Unit> units = new List<Unit>();
    #endregion

    static double Distance(int x1, int y1, int x2, int y2) => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

    static String GetQueenAction()
    {
        // Build if close to empty spot
        if (Queen.TouchId != -1)
        {
            if (buildings[Queen.TouchId].StructureType == StructureType.None)
            {
                return $"BUILD {Queen.TouchId} BARRACKS-KNIGHT";
            }
        }

        // Go to closest empty site if not
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
        return "WAIT"; // default action
    }

    static String GetTrainingAction()
    {
        var knightsList = new HashSet<int>();
        foreach (var knightsBarrak in buildings.Where(x => x.Value.Owner == Owner.Me &&
                                                      x.Value.StructureType == StructureType.Caserne &&
                                                      x.Value.CoolDown == 0 &&
                                                      x.Value.UnitType == UnitType.Knight))
        {
            if (Gold > knightsCost)
            {
                Gold -= knightsCost;
                knightsList.Add(knightsBarrak.Key);
            }
        }
        return $"TRAIN" + (knightsList.Any() ? " " + string.Join(" ", knightsList) : "");
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
            Deb($"Gold:{Gold}");
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
                    StructureType = (StructureType)structureType,
                    Owner = (Owner)owner,
                    CoolDown = param1,
                    UnitType = (UnitType)param2
                };
            }
            Deb("Buildings:");
            DebList(buildings.Values.ToList());

            units.Clear();
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
                    units.Add(new Unit()
                    {
                        X = x,
                        Y = y,
                        Owner = (Owner)owner,
                        UnitType = (UnitType)unitType,
                        HP = health
                    });
            }
            if (units.Any())
            {
                Deb("Units:");
                DebList(units);
            }

            Deb($"My: {Queen}");
            Deb($"Enemy: {EnemyQueen}");
            #endregion

            // First line: A valid queen action
            Console.WriteLine(GetQueenAction());
            // Second line: A set of training instructions
            Console.WriteLine(GetTrainingAction());
        }
    }
}