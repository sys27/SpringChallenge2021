using System.Linq;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using SpringChallenge2021.AI;

//class Minimax
//{
//    private readonly Game _game;

//    public Minimax(Game game)
//    {
//        _game = game;
//    }

//    private List<Action> Solve(Game game)
//    {
//        game = game.Clone();

//        // GATHERING
//        game.ResetGameTurnData();
//        game.PerformGameUpdate();

//        // ACTIONS
//        while (game.CurrentFrameType == FrameType.ACTIONS)
//        {
//            var p1 = game.Manager.GetPlayer(0);
//            var p1Moves = game.GetPossibleMoves(p1);

//            var p2 = game.Manager.GetPlayer(1);
//            var p2Moves = game.GetPossibleMoves(p2);

//            game.ResetGameTurnData();
//            game.PerformGameUpdate();
//        }

//        // SUN_MOVE
//        game.ResetGameTurnData();
//        game.PerformGameUpdate();

//        return scope;
//    }

//    private IEnumerable<Action> P1(Game game)
//    {
//        var p1 = game.Manager.GetPlayer(0);
//        var moves = game.GetPossibleMoves(p1);

//        foreach (var move in moves)
//        {
//            yield return move;
//        }
//    }

//    //public Action Solve()
//    //{
//    //    var p1 = _game.Manager.GetPlayer(0);
//    //    var moves = _game.GetPossibleMoves(p1);

//    //    var action = default(Action);
//    //    var value = double.MinValue;

//    //    foreach (var move in moves)
//    //    {
//    //        var score = Solve((move, null), _game, 5, false, int.MinValue, int.MaxValue);
//    //        if (value < score)
//    //        {
//    //            value = score;
//    //            action = move;
//    //        }
//    //    }

//    //    return action;
//    //}

//    //private double Solve((Action move1, Action move2) moves, Game game, int depth, bool maximizingPlayer, double alpha, double beta)
//    //{
//    //    game = game.Clone();

//    //    var p1 = game.Manager.GetPlayer(0);
//    //    var p2 = game.Manager.GetPlayer(1);

//    //    if (maximizingPlayer)
//    //    {
//    //        game.ResetGameTurnData();

//    //        p1.Action = moves.move1;
//    //        p2.Action = moves.move2;

//    //        game.PerformGameUpdate();
//    //    }

//    //    game.ResetGameTurnData();
//    //    game.PerformGameUpdate();

//    //    if ((depth <= 0 && maximizingPlayer) || game.GameOver())
//    //        return Score(game);

//    //    if (maximizingPlayer)
//    //    {
//    //        var value = double.MinValue;

//    //        var actions = game.GetPossibleMoves(game.Manager.GetPlayer(0));
//    //        foreach (var child in actions)
//    //        {
//    //            value = Math.Max(value, Solve((child, moves.move2), game, depth - 1, false, alpha, beta));

//    //            alpha = Math.Max(alpha, value);
//    //            if (alpha >= beta)
//    //                break;
//    //        }

//    //        return value;
//    //    }
//    //    else
//    //    {
//    //        var value = double.MaxValue;

//    //        var actions = game.GetPossibleMoves(game.Manager.GetPlayer(1));
//    //        foreach (var child in actions)
//    //        {
//    //            value = Math.Min(value, Solve((moves.move1, child), game, depth - 1, true, alpha, beta));

//    //            beta = Math.Min(beta, value);
//    //            if (beta <= alpha)
//    //                break;
//    //        }

//    //        return value;
//    //    }
//    //}

//    //private double Score(Game game)
//    //{
//    //    var p1 = game.Manager.GetPlayer(0);
//    //    var trees = game.Trees
//    //        .Values
//    //        .Where(x => x.Owner == p1)
//    //        .Sum(x => x.Size);

//    //    return p1.Score + p1.Sun * 0.3 + trees;
//    //}
//}

class P
{
    static void Main(string[] args)
    {
        //var random = new Random();

        //var gameManager = new GameManager();
        //var player1 = gameManager.AddPlayer();
        //var player2 = gameManager.AddPlayer();

        //var game = new Game(gameManager, 1000);

        //var sw = new Stopwatch();

        //while (!game.GameOver())
        //{
        //    game.ResetGameTurnData();

        //    var minimax = new Minimax(game);

        //    sw = Stopwatch.StartNew();
        //    var p1Move = minimax.Solve();

        //    // var p1Move = game.GetPossibleMoves(player1)
        //    //     .OrderBy(_ => random.Next())
        //    //     .First();
        //    sw.Stop();

        //    var p2Move = game.GetPossibleMoves(player2)
        //        .OrderBy(_ => random.Next())
        //        .First();

        //    player1.Action = p1Move;
        //    player2.Action = p2Move;

        //    game.PerformGameUpdate();

        //    Console.WriteLine("-----");
        //    Console.WriteLine($"Turn #{game.Round} ({sw.ElapsedMilliseconds} ms)");
        //    Console.WriteLine($"P1: {player1.Score} - {p1Move}");
        //    Console.WriteLine($"P2: {player2.Score} - {p2Move}");
        //}



        string[] inputs;

        int numberOfCells = int.Parse(Console.ReadLine()); // 37
        var cells = new List<Cell>(numberOfCells);
        for (int i = 0; i < numberOfCells; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int index = int.Parse(inputs[0]); // 0 is the center cell, the next cells spiral outwards
            int richness = int.Parse(inputs[1]); // 0 if the cell is unusable, 1-3 for usable cells
            var cell = new Cell(index) { Richness = richness };
            cells.Add(cell);
        }

        var gameManager = new GameManager();
        var player1 = gameManager.AddPlayer();
        var player2 = gameManager.AddPlayer();
        var game = new Game(gameManager, cells);

        // game loop
        while (true)
        {
            var day = int.Parse(Console.ReadLine()); // the game lasts 24 days: 0-23
            var nutrients = int.Parse(Console.ReadLine()); // the base score you gain from the next COMPLETE action
            inputs = Console.ReadLine().Split(' ');
            var mySun = int.Parse(inputs[0]); // your sun points
            var myScore = int.Parse(inputs[1]); // your current score
            inputs = Console.ReadLine().Split(' ');
            var opponentSun = int.Parse(inputs[0]); // opponent's sun points
            var opponentScore = int.Parse(inputs[1]); // opponent's score
            var opponentIsWaiting = inputs[2] != "0"; // whether your opponent is asleep until the next day

            int numberOfTrees = int.Parse(Console.ReadLine()); // the current amount of trees
            var trees = new Dictionary<int, Tree>(numberOfTrees);
            for (int i = 0; i < numberOfTrees; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int cellIndex = int.Parse(inputs[0]); // location of this tree
                int size = int.Parse(inputs[1]); // size of this tree: 0-3
                bool isMine = inputs[2] != "0"; // 1 if this is your tree
                bool isDormant = inputs[3] != "0"; // 1 if this tree is dormant
                var tree = new Tree
                {
                    Size = size,
                    Owner = isMine ? player1 : player2,
                    IsDormant = isDormant,
                };
                trees[cellIndex] = tree;
            }

            var possibleActions = new List<string>();
            int numberOfPossibleMoves = int.Parse(Console.ReadLine());
            for (int i = 0; i < numberOfPossibleMoves; i++)
            {
                string possibleMove = Console.ReadLine();
                possibleActions.Add(possibleMove);
            }

            game.Round = day;
            game.Nutrients = nutrients;
            game.Trees = trees;

            player1.Sun = mySun;
            player1.Score = myScore;

            player2.Sun = opponentSun;
            player2.Score = opponentScore;
            player2.Waiting = opponentIsWaiting;

            var minimax = new Minimax(game);
            var action = minimax.Solve();

            Console.WriteLine(action);
        }
    }
}