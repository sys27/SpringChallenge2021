using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

class Cell
{
    public int index;
    public int richness;
    public int[] neighbours;

    public Cell(int index, int richness, int[] neighbours)
    {
        this.index = index;
        this.richness = richness;
        this.neighbours = neighbours;
    }
}

class Tree
{
    public int cellIndex;
    public int size;
    public bool isMine;
    public bool isDormant;

    public Tree(int cellIndex, int size, bool isMine, bool isDormant)
    {
        this.cellIndex = cellIndex;
        this.size = size;
        this.isMine = isMine;
        this.isDormant = isDormant;
    }
}

class Action
{
    public const string WAIT = "WAIT";
    public const string SEED = "SEED";
    public const string GROW = "GROW";
    public const string COMPLETE = "COMPLETE";

    public static Action Parse(string action)
    {
        var parts = action.Split(" ");
        switch (parts[0])
        {
            case WAIT:
                return new Action(WAIT);
            case SEED:
                return new Action(SEED, int.Parse(parts[1]), int.Parse(parts[2]));
            case GROW:
            case COMPLETE:
            default:
                return new Action(parts[0], int.Parse(parts[1]));
        }
    }

    public string type;
    public int targetCellIdx;
    public int sourceCellIdx;

    public Action(string type, int sourceCellIdx, int targetCellIdx)
    {
        this.type = type;
        this.targetCellIdx = targetCellIdx;
        this.sourceCellIdx = sourceCellIdx;
    }

    public Action(string type, int targetCellIdx)
        : this(type, 0, targetCellIdx)
    {
    }

    public Action(string type)
        : this(type, 0, 0)
    {
    }

    public override string ToString()
    {
        if (type == WAIT)
        {
            return Action.WAIT;
        }

        if (type == SEED)
        {
            return string.Format("{0} {1} {2}", SEED, sourceCellIdx, targetCellIdx);
        }

        return string.Format("{0} {1}", type, targetCellIdx);
    }
}

class Game
{
    public int day;
    public int nutrients;
    public Dictionary<int, Cell> board;
    public List<Action> possibleActions;
    public Dictionary<int, Tree> trees;
    public int mySun, opponentSun;
    public int myScore, opponentScore;
    public bool opponentIsWaiting;

    public Game()
    {
        board = new Dictionary<int, Cell>();
        possibleActions = new List<Action>();
        trees = new Dictionary<int, Tree>();
    }

    private Tree[] GetMyTrees(int size)
        => trees.Values.Where(x => x.isMine && x.size == size).ToArray();

    private int GetGrowCost(Tree tree)
    {
        return tree.size switch
        {
            2 => 7 + GetMyTrees(3).Length,
            1 => 3 + GetMyTrees(2).Length,
            0 => 1 + GetMyTrees(1).Length,
            _ => throw new Exception(),
        };
    }

    private int GetSeedCost() => GetMyTrees(0).Length;

    private int CompleteCost = 4;

    private int MapRichness(int richness)
        => richness switch
        {
            3 => 4,
            2 => 2,
            _ => 0,
        };

    private int GetSun()
        => trees.Values
            .Where(x => x.isMine)
            .Sum(x => x.size);

    private (double score, double sun) ActionScore(Action action)
    {
        return action.type switch
        {
            Action.SEED => SeedScore(action),
            Action.GROW => GrowScore(action),
            Action.COMPLETE => CompleteScore(action),
            Action.WAIT => WaitScore(),
        };
    }

    private (double score, double sun) SeedScore(Action action)
    {
        var dayIndex = day < 1 ? 5.0 : 7.0 / day;
        var cell = board[action.targetCellIdx];
        var seedCost = GetSeedCost();

        if (mySun < seedCost)
            return (double.MinValue, double.MinValue);

        var score = MapRichness(cell.richness) * dayIndex;
        var sun = GetSun() - seedCost;

        return (score, sun);
    }

    private (double score, double sun) GrowScore(Action action)
    {
        var dayIndex = day >= 1 && day < 21 ? 5.0 : 1.0;
        var tree = trees[action.targetCellIdx];
        var growCost = GetGrowCost(tree);

        if (mySun < growCost)
            return (double.MinValue, double.MinValue);

        var score = 1 * (tree.size + 1) * dayIndex;
        var sun = GetSun() - growCost + 1;

        return (score, sun);
    }

    private (double score, double sun) CompleteScore(Action action)
    {
        var dayIndex = day >= 21 ? 5.0 : day * 1.0 / 33;

        if (mySun < CompleteCost)
            return (double.MinValue, double.MinValue);

        var score = (nutrients + MapRichness(board[action.targetCellIdx].richness)) * dayIndex;
        var sun = GetSun() - 3;

        return (score, sun);
    }

    private (double score, double sun) WaitScore()
    {
        var dayIndex = day >= 21 ? 0.5 : 1.0;

        var sun = GetSun();
        var score = (sun / 3.0) * dayIndex;

        return (score, sun);
    }

    private double TotalScore((double score, double sun) tuple)
    {
        var (score, sun) = tuple;

        return score + (sun * 0.3);
    }

    public Action GetNextAction()
    {
        // var move = possibleActions
        //     .OrderByDescending(x => TotalScore(ActionScore(x)))
        //     .First();

        var move = default(Action);
        var moveScore = double.MinValue;

        foreach (var action in possibleActions)
        {
            var score = ActionScore(action);
            var total = TotalScore(score);

            if (moveScore < total)
            {
                move = action;
                moveScore = total;
            }

            Console.Error.WriteLine($"Action: {action}, Score: {score}, Total: {total}");
        }

        return move;
    }
}

class Player
{
    static void Main(string[] args)
    {
        string[] inputs;

        Game game = new Game();

        int numberOfCells = int.Parse(Console.ReadLine()); // 37
        for (int i = 0; i < numberOfCells; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int index = int.Parse(inputs[0]); // 0 is the center cell, the next cells spiral outwards
            int richness = int.Parse(inputs[1]); // 0 if the cell is unusable, 1-3 for usable cells
            int neigh0 = int.Parse(inputs[2]); // the index of the neighbouring cell for each direction
            int neigh1 = int.Parse(inputs[3]);
            int neigh2 = int.Parse(inputs[4]);
            int neigh3 = int.Parse(inputs[5]);
            int neigh4 = int.Parse(inputs[6]);
            int neigh5 = int.Parse(inputs[7]);
            int[] neighs = new int[] { neigh0, neigh1, neigh2, neigh3, neigh4, neigh5 };
            Cell cell = new Cell(index, richness, neighs);
            game.board.Add(index, cell);

            // Console.Error.WriteLine($"Parent: {index}");
            // foreach (var neigh in neighs)
            //     Console.Error.WriteLine(neigh);
            // Console.Error.WriteLine();
        }

        // game loop
        while (true)
        {
            game.day = int.Parse(Console.ReadLine()); // the game lasts 24 days: 0-23
            game.nutrients = int.Parse(Console.ReadLine()); // the base score you gain from the next COMPLETE action
            inputs = Console.ReadLine().Split(' ');
            game.mySun = int.Parse(inputs[0]); // your sun points
            game.myScore = int.Parse(inputs[1]); // your current score
            inputs = Console.ReadLine().Split(' ');
            game.opponentSun = int.Parse(inputs[0]); // opponent's sun points
            game.opponentScore = int.Parse(inputs[1]); // opponent's score
            game.opponentIsWaiting = inputs[2] != "0"; // whether your opponent is asleep until the next day

            game.trees.Clear();
            int numberOfTrees = int.Parse(Console.ReadLine()); // the current amount of trees
            for (int i = 0; i < numberOfTrees; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int cellIndex = int.Parse(inputs[0]); // location of this tree
                int size = int.Parse(inputs[1]); // size of this tree: 0-3
                bool isMine = inputs[2] != "0"; // 1 if this is your tree
                bool isDormant = inputs[3] != "0"; // 1 if this tree is dormant
                Tree tree = new Tree(cellIndex, size, isMine, isDormant);
                game.trees.Add(cellIndex, tree);
            }

            game.possibleActions.Clear();
            int numberOfPossibleMoves = int.Parse(Console.ReadLine());
            for (int i = 0; i < numberOfPossibleMoves; i++)
            {
                string possibleMove = Console.ReadLine();
                game.possibleActions.Add(Action.Parse(possibleMove));

                // Console.Error.WriteLine(possibleMove);
            }

            Action action = game.GetNextAction();
            Console.WriteLine(action);
        }
    }
}