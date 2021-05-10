using System;
using System.Collections.Generic;
using System.Linq;

public class Action
{
    public static readonly Action NO_ACTION = new Action();

    public virtual bool IsGrow() => false;

    public virtual bool IsComplete() => false;

    public virtual bool IsSeed() => false;

    public virtual bool IsWait() => false;

    public int SourceId { get; protected set; }

    public int TargetId { get; protected set; }
}

public class CompleteAction : Action
{
    public CompleteAction(int targetId) => TargetId = targetId;

    public override bool IsComplete() => true;

    public override string ToString() => $"COMPLETE {TargetId}";
}

public class GrowAction : Action
{
    public GrowAction(int targetId) => TargetId = targetId;

    public override bool IsGrow() => true;

    public override string ToString() => $"GROW {TargetId}";
}


public class SeedAction : Action
{
    public SeedAction(int sourceId, int targetId)
        => (SourceId, TargetId) = (sourceId, targetId);

    public override bool IsSeed() => true;

    public override string ToString() => $"SEED {SourceId} {TargetId}";
}

public class WaitAction : Action
{
    public override bool IsWait() => true;

    public override string ToString() => $"WAIT";
}

static class Constants
{
    public const int RICHNESS_NULL = 0;
    public const int RICHNESS_POOR = 1;
    public const int RICHNESS_OK = 2;
    public const int RICHNESS_LUSH = 3;

    public const int TREE_SEED = 0;
    public const int TREE_SMALL = 1;
    public const int TREE_MEDIUM = 2;
    public const int TREE_TALL = 3;

    public static readonly int[] TREE_BASE_COST = new int[] { 0, 1, 3, 7 };
    public const int TREE_COST_SCALE = 1;
    public const int LIFECYCLE_END_COST = 4;
    public const int STARTING_TREE_COUNT = 2;
    public const int RICHNESS_BONUS_OK = 2;
    public const int RICHNESS_BONUS_LUSH = 4;

    public const int STARTING_SUN = 0;
    public const int MAP_RING_COUNT = 3;
    public const int STARTING_NUTRIENTS = 20;
    public const int MAX_ROUNDS = 24;
    public const int MAX_EMPTY_CELLS = 10;
}

enum FrameType
{
    GATHERING,
    ACTIONS,
    SUN_MOVE,
    INIT
}

enum TreeSize : int
{
    Seed = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
}

class CubeCoord
{
    static int[][] directions = new int[][]
    {
        new int[] { 1, -1, 0 },
        new int[] { +1, 0, -1 },
        new int[] { 0, +1, -1 },
        new int[] { -1, +1, 0 },
        new int[] { -1, 0, +1 },
        new int[] { 0, -1, +1 },
    };

    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public CubeCoord(int x, int y, int z)
        => (X, Y, Z) = (x, y, z);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public override bool Equals(object obj)
    {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        if (GetType() != obj.GetType())
            return false;

        var other = (CubeCoord)obj;

        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public CubeCoord Neighbor(int orientation)
    {
        return Neighbor(orientation, 1);
    }

    public CubeCoord Neighbor(int orientation, int distance)
    {
        var nx = X + directions[orientation][0] * distance;
        var ny = Y + directions[orientation][1] * distance;
        var nz = Z + directions[orientation][2] * distance;

        return new CubeCoord(nx, ny, nz);
    }

    public int DistanceTo(CubeCoord dst)
        => (Math.Abs(X - dst.X) + Math.Abs(Y - dst.Y) + Math.Abs(Z - dst.Z)) / 2;

    public CubeCoord GetOpposite()
        => new CubeCoord(-X, -Y, -Z);

    public CubeCoord Clone()
        => new CubeCoord(X, Y, Z);
}

class Tree
{
    public int Size { get; set; }
    public Player Owner { get; set; }
    public int FatherIndex { get; set; } = -1;
    public bool IsDormant { get; set; }

    public void Grow() => Size++;

    public Tree Clone(GameManager gameManager)
        => new Tree
        {
            Size = this.Size,
            Owner = gameManager.GetPlayer(this.Owner.Index),
            FatherIndex = this.FatherIndex,
            IsDormant = this.IsDormant,
        };
}

class Cell
{
    public static readonly Cell NO_CELL = new Cell(-1);

    public int Richness { get; set; }
    public int Index { get; }

    public Cell(int index) => Index = index;

    public bool IsValid() => Index >= 0;

    public Cell Clone()
        => new Cell(this.Index) { Richness = this.Richness };
}

class Seed
{
    public int Owner { get; set; }
    public int SourceCell { get; set; }
    public int TargetCell { get; set; }

    public Seed Clone()
        => new Seed
        {
            Owner = this.Owner,
            SourceCell = this.SourceCell,
            TargetCell = this.TargetCell,
        };
}

class Sun
{
    private int orientation;

    public int Orientation
    {
        get => orientation;
        set => orientation = value % 6;
    }

    public void Move() => orientation = (orientation + 1) % 6;

    public Sun Clone()
        => new Sun { orientation = this.orientation };
}

class Board
{
    public readonly Dictionary<CubeCoord, Cell> map;
    public readonly List<CubeCoord> coords;

    public Board(Dictionary<CubeCoord, Cell> map)
    {
        this.map = map;
        coords = map
            .OrderBy(x => x.Value.Index)
            .Select(x => x.Key)
            .ToList();
    }

    public Board Clone()
    {
        var map = this.map
            .Select(kv => (Key: kv.Key.Clone(), Value: kv.Value.Clone()))
            .ToDictionary(x => x.Key, x => x.Value);

        return new Board(map);
    }
}

class BoardGenerator
{
    static Dictionary<CubeCoord, Cell> board;
    static int index;

    public static void GenerateCell(CubeCoord coord, int richness)
    {
        board[coord] = new Cell(index++)
        {
            Richness = richness
        };
    }

    public static Board From(List<Cell> cells)
    {
        board = new Dictionary<CubeCoord, Cell>();
        index = 0;
        var centre = new CubeCoord(0, 0, 0);

        board[centre] = cells[index++];

        var coord = centre.Neighbor(0);

        for (var distance = 1; distance <= Constants.MAP_RING_COUNT; distance++)
        {
            for (var orientation = 0; orientation < 6; orientation++)
            {
                for (var count = 0; count < distance; count++)
                {
                    board[coord] = cells[index++];

                    coord = coord.Neighbor((orientation + 2) % 6);
                }
            }

            coord = coord.Neighbor(0);
        }

        return new Board(board);
    }

    public static Board Generate(Random random)
    {
        board = new Dictionary<CubeCoord, Cell>();
        index = 0;
        var centre = new CubeCoord(0, 0, 0);

        GenerateCell(centre, Constants.RICHNESS_LUSH);

        var coord = centre.Neighbor(0);

        for (var distance = 1; distance <= Constants.MAP_RING_COUNT; distance++)
        {
            for (var orientation = 0; orientation < 6; orientation++)
            {
                for (var count = 0; count < distance; count++)
                {
                    if (distance == Constants.MAP_RING_COUNT)
                        GenerateCell(coord, Constants.RICHNESS_POOR);
                    else if (distance == Constants.MAP_RING_COUNT - 1)
                        GenerateCell(coord, Constants.RICHNESS_OK);
                    else
                        GenerateCell(coord, Constants.RICHNESS_LUSH);

                    coord = coord.Neighbor((orientation + 2) % 6);
                }
            }

            coord = coord.Neighbor(0);
        }

        var coordList = new List<CubeCoord>(board.Keys);
        var coordListSize = coordList.Count;
        var wantedEmptyCells = random.Next(Constants.MAX_EMPTY_CELLS + 1);
        var actualEmptyCells = 0;

        while (actualEmptyCells < wantedEmptyCells - 1)
        {
            var randIndex = random.Next(coordListSize);
            var randCoord = coordList[randIndex];
            if (board[randCoord].Richness != Constants.RICHNESS_NULL)
            {
                board[randCoord].Richness = Constants.RICHNESS_NULL;
                actualEmptyCells++;
                if (!randCoord.Equals(randCoord.GetOpposite()))
                {
                    board[randCoord.GetOpposite()].Richness = Constants.RICHNESS_NULL;
                    actualEmptyCells++;
                }
            }
        }

        return new Board(board);
    }
}

class Player
{
    public int Index { get; }
    public int Score { get; set; }
    public Action Action { get; set; }
    public int Sun { get; set; }
    public bool Waiting { get; set; }

    private Player() { }

    public Player(int index)
    {
        Index = index;
        Sun = Constants.STARTING_SUN;
        Action = Action.NO_ACTION;
    }

    public void AddScore(int score) => Score += score;

    public void Reset() => Action = Action.NO_ACTION;

    public void AddSun(int sun) => Sun += sun;

    public void RemoveSun(int amount) => Sun = Math.Max(0, Sun - amount);

    public Player Clone()
    {
        return new Player(this.Index)
        {
            Score = this.Score,
            Action = this.Action,
            Sun = this.Sun,
            Waiting = this.Waiting,
        };
    }
}

class GameManager
{
    private List<Player> players;

    public GameManager()
    {
        players = new List<Player>();
    }

    public int GetPlayerCount() => players.Count;

    public List<Player> GetPlayers() => players;

    public Player GetPlayer(int index) => players[index];

    public Player AddPlayer()
    {
        var player = new Player(players.Count);

        players.Add(player);

        return player;
    }

    public GameManager Clone()
    {
        return new GameManager
        {
            players = this.players.Select(x => x.Clone()).ToList()
        };
    }
}

class Game
{
    private GameManager gameManager;

    public GameManager Manager => gameManager;

    private int nutrients = Constants.STARTING_NUTRIENTS;
    private Board board;
    private Dictionary<int, Tree> trees;
    private List<CubeCoord> dyingTrees;
    private List<int> availableSun;
    private List<Seed> sentSeeds;
    private Sun sun;
    private Dictionary<int, int> shadows;
    private List<Cell> cells;
    private Random random;
    private int round = 0;
    private int turn = 0;
    private FrameType currentFrameType = FrameType.INIT;
    private FrameType nextFrameType = FrameType.GATHERING;

    public const int MAX_ROUNDS = 24;
    public const int STARTING_TREE_COUNT = 2;
    public const int STARTING_TREE_SIZE = (int)TreeSize.Small;
    public const int STARTING_TREE_DISTANCE = 2;

    public int Nutrients
    {
        get => nutrients;
        set => nutrients = value;
    }

    public int Round
    {
        get => round;
        set => round = value;
    }

    public Dictionary<int, Tree> Trees
    {
        get => trees;
        set => trees = value;
    }

    private Game(GameManager gameManager)
    {
        this.gameManager = gameManager;
        random = new Random();
    }

    public Game(GameManager gameManager, List<Cell> cells)
        : this(gameManager)
    {
        board = BoardGenerator.From(cells);
        trees = new Dictionary<int, Tree>();
        dyingTrees = new List<CubeCoord>();
        this.cells = cells;
        availableSun = new List<int>(gameManager.GetPlayerCount());
        sentSeeds = new List<Seed>();

        sun = new Sun();
        shadows = new Dictionary<int, int>();
        sun.Orientation = 0;

        round = 0;

        CalculateShadows();
    }

    public Game(GameManager gameManager, int? seed = null)
        : this(gameManager)
    {
        if (seed.HasValue)
            random = new Random(seed.Value);

        board = BoardGenerator.Generate(random);
        trees = new Dictionary<int, Tree>();
        dyingTrees = new List<CubeCoord>();
        cells = new List<Cell>();
        availableSun = new List<int>(gameManager.GetPlayerCount());
        sentSeeds = new List<Seed>();
        InitStartingTrees();

        sun = new Sun();
        shadows = new Dictionary<int, int>();
        sun.Orientation = 0;

        round = 0;

        CalculateShadows();
    }

    private void InitStartingTrees()
    {
        var startingCoords = GetBoardEdges();

        startingCoords = startingCoords
            .Where(coord => board.map[coord].Richness != Constants.RICHNESS_NULL)
            .ToList();

        var validCoords = new List<CubeCoord>();

        while (validCoords.Count < STARTING_TREE_COUNT * 2)
            validCoords = TryInitStartingTrees(startingCoords);

        var players = gameManager.GetPlayers();
        for (var i = 0; i < STARTING_TREE_COUNT; i++)
        {
            PlaceTree(players[0], board.map[validCoords[2 * i]].Index, STARTING_TREE_SIZE);
            PlaceTree(players[1], board.map[validCoords[2 * i + 1]].Index, STARTING_TREE_SIZE);
        }
    }

    private List<CubeCoord> GetBoardEdges()
    {
        var centre = new CubeCoord(0, 0, 0);

        return board.coords
            .Where(coord => coord.DistanceTo(centre) == Constants.MAP_RING_COUNT)
            .ToList();
    }

    private List<CubeCoord> TryInitStartingTrees(List<CubeCoord> startingCoords)
    {
        var coordinates = new List<CubeCoord>();

        var availableCoords = new List<CubeCoord>(startingCoords);
        for (var i = 0; i < STARTING_TREE_COUNT; i++)
        {
            if (availableCoords.Count == 0)
                return coordinates;

            var r = random.Next(availableCoords.Count);
            var normalCoord = availableCoords[r];
            var oppositeCoord = normalCoord.GetOpposite();

            availableCoords = availableCoords
                .Where(coord => !(coord.DistanceTo(normalCoord) <= STARTING_TREE_DISTANCE ||
                                  coord.DistanceTo(oppositeCoord) <= STARTING_TREE_DISTANCE))
                .ToList();

            coordinates.Add(normalCoord);
            coordinates.Add(oppositeCoord);
        }

        return coordinates;
    }

    private void CalculateShadows()
    {
        shadows.Clear();

        foreach (var (index, tree) in trees)
        {
            var coord = board.coords[index];

            for (var i = 1; i <= tree.Size; i++)
            {
                var tempCoord = coord.Neighbor(sun.Orientation, i);

                if (board.map.ContainsKey(tempCoord))
                {
                    var key = board.map[tempCoord].Index;
                    if (shadows.TryGetValue(key, out var value))
                        shadows[key] = Math.Max(value, tree.Size);
                    else
                        shadows[key] = tree.Size;
                }
            }
        }
    }

    private void PlantSeed(Player player, int index, int fatherIndex)
    {
        var seed = PlaceTree(player, index, Constants.TREE_SEED);
        seed.IsDormant = true;
        seed.FatherIndex = fatherIndex;
    }

    private Tree PlaceTree(Player player, int index, int size)
        => trees[index] = new Tree
        {
            Size = size,
            Owner = player,
        };

    public void ResetGameTurnData()
    {
        dyingTrees.Clear();
        availableSun.Clear();
        sentSeeds.Clear();

        foreach (var p in gameManager.GetPlayers())
        {
            availableSun.Add(p.Sun);
            p.Reset();
        }

        currentFrameType = nextFrameType;
    }

    public void PerformGameUpdate()
    {
        turn++;

        switch (currentFrameType)
        {
            case FrameType.GATHERING:
                PerformSunGatheringUpdate();
                nextFrameType = FrameType.ACTIONS;
                break;
            case FrameType.ACTIONS:
                PerformActionUpdate();
                if (AllPlayersAreWaiting())
                    nextFrameType = FrameType.SUN_MOVE;
                break;
            case FrameType.SUN_MOVE:
                PerformSunMoveUpdate();
                nextFrameType = FrameType.GATHERING;
                break;
            default:
                Console.Error.WriteLine("Error: " + currentFrameType);
                break;
        }

        // if (GameOver())
        //     Console.Error.WriteLine("Game Over");
    }

    private void PerformSunGatheringUpdate()
    {
        // Wake players
        foreach (var player in gameManager.GetPlayers())
            player.Waiting = false;

        foreach (var (_, tree) in trees)
            tree.IsDormant = false;

        // Harvest
        GiveSun();
    }

    private void PerformSunMoveUpdate()
    {
        round++;
        if (round < MAX_ROUNDS)
        {
            sun.Move();

            CalculateShadows();
        }
    }

    private void PerformActionUpdate()
    {
        foreach (var player in gameManager.GetPlayers().Where(x => !x.Waiting))
        {
            try
            {
                var action = player.Action;
                if (action.IsGrow())
                    DoGrow(player, action);
                else if (action.IsSeed())
                    DoSeed(player, action);
                else if (action.IsComplete())
                    DoComplete(player, action);
                else
                    player.Waiting = true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                player.Waiting = true;
            }
        }

        if (!SeedsAreConflicting())
        {
            foreach (var seed in sentSeeds)
                PlantSeed(gameManager.GetPlayer(seed.Owner), seed.TargetCell, seed.SourceCell);

            foreach (var player in gameManager.GetPlayers())
                player.Sun = availableSun[player.Index];
        }

        RemoveDyingTrees();

        UpdateNutrients();
    }

    private CubeCoord GetCoordByIndex(int index)
    {
        return board.map
            .FirstOrDefault(e => e.Value.Index == index)
            .Key ?? throw new Exception();
    }

    private void DoGrow(Player player, Action action)
    {
        var coord = GetCoordByIndex(action.TargetId);
        var cell = board.map[coord];
        var targetTree = trees[cell.Index];

        if (targetTree == null)
            throw new Exception();
        if (targetTree.Owner != player)
            throw new Exception();
        if (targetTree.IsDormant)
            throw new Exception();
        if (targetTree.Size >= Constants.TREE_TALL)
            throw new Exception();

        var costOfGrowth = GetGrowthCost(targetTree);
        var currentSun = availableSun[player.Index];
        if (currentSun < costOfGrowth)
            throw new Exception();

        availableSun[player.Index] = currentSun - costOfGrowth;

        targetTree.Grow();

        targetTree.IsDormant = true;
    }

    private void DoComplete(Player player, Action action)
    {
        var coord = GetCoordByIndex(action.TargetId);
        var cell = board.map[coord];
        var targetTree = trees[cell.Index];

        if (targetTree == null)
            throw new Exception();
        if (targetTree.Owner != player)
            throw new Exception();
        if (targetTree.Size < Constants.TREE_TALL)
            throw new Exception();
        if (targetTree.IsDormant)
            throw new Exception();

        var costOfGrowth = GetGrowthCost(targetTree);
        var currentSun = availableSun[player.Index];
        if (currentSun < costOfGrowth)
            throw new Exception();

        availableSun[player.Index] = currentSun - costOfGrowth;

        dyingTrees.Add(coord);

        targetTree.IsDormant = true;
    }

    private void DoSeed(Player player, Action action)
    {
        var targetCoord = GetCoordByIndex(action.TargetId);
        var sourceCoord = GetCoordByIndex(action.SourceId);

        var targetCell = board.map[targetCoord];
        var sourceCell = board.map[sourceCoord];

        if (trees.ContainsKey(targetCell.Index))
            throw new Exception();

        var sourceTree = trees[sourceCell.Index];
        if (sourceTree == null)
            throw new Exception();
        if (sourceTree.Size == Constants.TREE_SEED)
            throw new Exception();
        if (sourceTree.Owner != player)
            throw new Exception();
        if (sourceTree.IsDormant)
            throw new Exception();

        var distance = sourceCoord.DistanceTo(targetCoord);
        if (distance > sourceTree.Size)
            throw new Exception();
        if (targetCell.Richness == Constants.RICHNESS_NULL)
            throw new Exception();

        var costOfSeed = GetSeedCost(player);
        var currentSun = availableSun[player.Index];
        if (currentSun < costOfSeed)
            throw new Exception();

        availableSun[player.Index] = currentSun - costOfSeed;
        sourceTree.IsDormant = true;
        var seed = new Seed
        {
            Owner = player.Index,
            SourceCell = sourceCell.Index,
            TargetCell = targetCell.Index
        };

        sentSeeds.Add(seed);
    }

    private int GetGrowthCost(Tree targetTree)
    {
        var targetSize = targetTree.Size + 1;
        if (targetSize > Constants.TREE_TALL)
            return Constants.LIFECYCLE_END_COST;

        return GetCostFor(targetSize, targetTree.Owner);
    }

    private int GetSeedCost(Player player) => GetCostFor(0, player);

    private int GetCostFor(int size, Player owner)
    {
        var baseCost = Constants.TREE_BASE_COST[size];
        var sameTreeCount = trees
            .Values
            .Count(t => t.Size == size && t.Owner == owner);

        return baseCost + sameTreeCount;
    }

    private void GiveSun()
    {
        var givenToPlayer = new int[2];

        foreach (var (index, tree) in trees)
        {
            if (!shadows.ContainsKey(index) || shadows[index] < tree.Size)
            {
                var owner = tree.Owner;
                owner.AddSun(tree.Size);
                givenToPlayer[owner.Index] += tree.Size;
            }
        }
    }

    private bool SeedsAreConflicting()
    {
        var cellIdsWithSeeds = sentSeeds
            .Select(seed => seed.TargetCell)
            .Distinct()
            .Count();

        return sentSeeds.Count != cellIdsWithSeeds;
    }

    private bool AllPlayersAreWaiting()
    {
        var waitingCount = gameManager
            .GetPlayers()
            .Count(x => x.Waiting);

        return waitingCount == gameManager.GetPlayerCount();
    }

    private void RemoveDyingTrees()
    {
        foreach (var coord in dyingTrees)
        {
            var cell = board.map[coord];
            var points = nutrients;

            if (cell.Richness == Constants.RICHNESS_OK)
                points += Constants.RICHNESS_BONUS_OK;
            else if (cell.Richness == Constants.RICHNESS_LUSH)
                points += Constants.RICHNESS_BONUS_LUSH;

            var player = trees[cell.Index].Owner;
            player.AddScore(points);
            trees.Remove(cell.Index);
        }
    }

    private void UpdateNutrients()
    {
        foreach (var coord in dyingTrees)
            nutrients = Math.Max(0, nutrients - 1);
    }

    public bool GameOver() => round >= MAX_ROUNDS;

    public List<Action> GetPossibleMoves(Player player)
    {
        var lines = new List<Action>
        {
            new WaitAction()
        };

        var possibleSeeds = new List<Action>();
        var possibleGrows = new List<Action>();
        var possibleCompletes = new List<Action>();

        if (player.Waiting)
            return lines;

        //For each tree, where they can seed.
        //For each tree, if they can grow.
        var seedCost = GetSeedCost(player);
        foreach (var (index, tree) in trees.Where(e => e.Value.Owner == player))
        {
            var coord = board.coords[index];

            if (PlayerCanSeedFrom(player, tree, seedCost))
            {
                foreach (var targetCoord in GetCoordsInRange(coord, tree.Size))
                {
                    var targetCell = board.map.GetValueOrDefault(targetCoord, Cell.NO_CELL);
                    if (PlayerCanSeedTo(targetCell))
                        possibleSeeds.Add(new SeedAction(index, targetCell.Index));
                }
            }

            var growCost = GetGrowthCost(tree);
            if (growCost <= player.Sun && !tree.IsDormant)
            {
                if (tree.Size == Constants.TREE_TALL)
                    possibleCompletes.Add(new CompleteAction(index));
                else
                    possibleGrows.Add(new GrowAction(index));
            }
        }

        lines.AddRange(
            possibleCompletes
                .Concat(possibleGrows)
                .Concat(possibleSeeds)
                .OrderBy(_ => random.Next())
        );

        return lines;
    }

    private static CubeCoord CubeAdd(CubeCoord a, CubeCoord b)
        => new CubeCoord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    private List<CubeCoord> GetCoordsInRange(CubeCoord center, int N)
    {
        var results = new List<CubeCoord>();
        for (var x = -N; x <= +N; x++)
        {
            for (var y = Math.Max(-N, -x - N); y <= Math.Min(+N, -x + N); y++)
            {
                var z = -x - y;
                results.Add(CubeAdd(center, new CubeCoord(x, y, z)));
            }
        }

        return results;
    }

    private bool PlayerCanSeedFrom(Player player, Tree tree, int seedCost)
        => seedCost <= player.Sun &&
           tree.Size > Constants.TREE_SEED &&
           !tree.IsDormant;

    private bool PlayerCanSeedTo(Cell targetCell)
        => targetCell.IsValid() &&
           targetCell.Richness != Constants.RICHNESS_NULL &&
           !trees.ContainsKey(targetCell.Index);

    public Game Clone()
    {
        var gameManager = this.gameManager.Clone();
        var game = new Game(gameManager)
        {
            nutrients = this.nutrients,
            board = this.board.Clone(),
            trees = this.trees.ToDictionary(x => x.Key, x => x.Value.Clone(gameManager)),
            dyingTrees = this.dyingTrees.Select(x => x.Clone()).ToList(),
            availableSun = this.availableSun.Select(x => x).ToList(),
            sentSeeds = this.sentSeeds.Select(x => x.Clone()).ToList(),
            sun = this.sun.Clone(),
            shadows = this.shadows.ToDictionary(x => x.Key, x => x.Value),
            cells = this.cells.Select(x => x.Clone()).ToList(),
            round = this.round,
            turn = this.turn,
            currentFrameType = this.currentFrameType,
            nextFrameType = this.nextFrameType,
        };

        return game;
    }
}