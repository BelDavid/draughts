//#define EVA
#define SIM

using Draughts;
using Draughts.BoardEvaluators;
using Draughts.Game;
using Draughts.GUI;
using Draughts.Pieces;
using Draughts.Players;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Controller
{
    class Program
    {
        public const string separator = "------------------------------------------------------";

        static void Main(string[] args)
        {
            // TrainEvA();
            RunSimulation();

            Console.Write("Press Enter to exit..."); 
            Console.ReadLine();
        }

        private static void TrainEvA()
        {
            string evaID = "basic0_vs_basic1";
            var eva = new EvolutionaryAlgorithm(evaID, new int[] { 10, 10, 10, }, RulesType.Czech)
            {
                paralelisedMatches = true,
                minimaxDepth = 3,
                numberOfGenerations = 30,
                populationSize = 30,
                mutationBitRate = 1d,
                crossoverRate = .5d,
                numberOfCompetetiveMatches = 50,
            };
            var gen = eva.Run();
        }

        private static void RunSimulation()
        {
            // Initialise
            const string simFolderPath = "../../../local/sim";
            if (!Directory.Exists(simFolderPath))
            {
                Directory.CreateDirectory(simFolderPath);
            }

            //var netId0 = $"{id}/gen29_net0";
            //var nn0 = Utils.LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/run_{netId0}.{Utils.neuralNetworkFileExt}");

            // Setup bots
            var bot_minimax_basic = new Bot
            {
                id = "minimax_basic",
                botFactory = (id, depth) => new MinimaxBot(id, depth, new BoardEvaluatorBasic(), null),
description = (id) => $@"- {id}:
   - Depth-limited minimax with state evaluation:
      - White man:   1
      - White King:  5
      - Black man:  -1
      - Black King: -5
"
            };
            var bot_randomized = new Bot
            {
                id = "randomized",
                botFactory = (id, _) => new RandomizedBot(id),
description = (id) => $@"- {id}:
   - Plays random moves
"
            };
            var minimax_basic_depth_2 = new Bot
            {
                id = "minimax_basic_depth=2",
                botFactory = (id, _) => new MinimaxBot(id, 2, new BoardEvaluatorBasic(), null),
description = (id) => $@"- {id}:
   - Depth-limited minimax with state evaluation and depth fixed at 2:
      - White man:   1
      - White King:  5
      - Black man:  -1
      - Black King: -5
"
            };

            // Choose bots
            var bot0 = bot_minimax_basic;
            var bot1 = minimax_basic_depth_2;

            // Setup game
            int numberOfGames = 1000;

            // Setup log
            string simID = $"{bot0.id}_vs_{bot1.id}";
            string simFilePath = $"{simFolderPath}/{simID}.txt";
            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine("Bots:");
                sw.Write(bot0.GetDescription());
                sw.Write(bot1.GetDescription());
                
                sw.WriteLine();
                sw.WriteLine($"Number of games per simulation: {numberOfGames}");
                sw.WriteLine(separator);
            }

            // Run
            for (int i = 1; i < 6; i++)
            {
                run($"run{i}", i);
            }
            void run(string runID, int depth)
            {
                var simOut = SimulateParallel(
                    runID,
                    RulesType.Czech,
                    bot0.GetBotFactory(depth),
                    bot1.GetBotFactory(depth),
                    numberOfGames
                );
                string message = $"[{runID}] {bot0.id}: {simOut.player0Wins} (w:{simOut.player0WinsWhite} b:{simOut.player0WinsBlack}) | ties: {simOut.ties} | {bot1.id}: {simOut.player1Wins} (w:{simOut.player1WinsWhite} b:{simOut.player1WinsBlack})";
                Console.WriteLine(message);
                using (var sw = new StreamWriter(simFilePath, true))
                {
                    sw.WriteLine($"Run: {runID}");

                    sw.WriteLine($"Settings:");
                    sw.WriteLine($" - depth: {depth}");

                    sw.WriteLine($"Score:");
                    sw.WriteLine($" - Bot '{bot0.id}' wins: {simOut.player0Wins}");
                    sw.WriteLine($"    - white: {simOut.player0WinsWhite}");
                    sw.WriteLine($"    - black: {simOut.player0WinsBlack}");

                    sw.WriteLine($" - Ties: {simOut.ties}");

                    sw.WriteLine($" - Bot '{bot1.id}' wins: {simOut.player1Wins}");
                    sw.WriteLine($"    - white: {simOut.player1WinsWhite}");
                    sw.WriteLine($"    - black: {simOut.player1WinsBlack}");

                    sw.WriteLine(separator);
                }
            }

            // Finalise
            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine(separator);
            }
        }

        private class Bot
        {
            public string id;
            public Func<string, int, Player> botFactory; // id, depth, out Player
            public Func<string, string> description;

            public Func<Player> GetBotFactory(int depth) => () => botFactory(id, depth);
            public string GetDescription() => description(id);
        }

        public static SimulationOutput SimulateSerial(string simulationID, RulesType rules, Func<Player> bot0Factory, Func<Player> bot1Factory, int numberOfRuns)
        {
            var output = new SimulationOutput() { total = numberOfRuns, };

            for (int i = 0; i < numberOfRuns; i++)
            {
                var bot0 = bot0Factory();
                var bot1 = bot1Factory();

                var firstPlayer = i % 2 == 0 ? bot0 : bot1;
                var secondPlayer = i % 2 == 0 ? bot1 : bot0;

                var gameControl = new GameControl($"{simulationID}", rules, firstPlayer, secondPlayer);

                var finishReason = gameControl.Run();

                if (finishReason == FinishReason.OnePlayerWon)
                {
                    if (gameControl.Winner == bot0)
                    {
                        if (bot0.Color == PieceColor.White)
                        {
                            output.player0WinsWhite += 1;
                        }
                        else if (bot0.Color == PieceColor.Black)
                        {
                            output.player0WinsBlack += 1;
                        }
                    }
                    else if (gameControl.Winner == bot1)
                    {
                        if (bot1.Color == PieceColor.White)
                        {
                            output.player1WinsWhite += 1;
                        }
                        else if (bot1.Color == PieceColor.Black)
                        {
                            output.player1WinsBlack += 1;
                        }
                    }
                }
                else if (finishReason == FinishReason.MoveLimitReached)
                {
                    output.ties += 1;
                }
            }

            return output;
        }
        public static SimulationOutput SimulateParallel(string simulationID, RulesType rules, Func<Player> bot0Factory, Func<Player> bot1Factory, int numberOfRuns)
        {
            var output = new SimulationOutput() { total = numberOfRuns, };
            
            int done = 0;
            var objectLock = new object();
            Console.Write($"[{simulationID}] 0/{numberOfRuns}");

            Parallel.For(0, numberOfRuns, i =>
            {
                var bot0 = bot0Factory();
                var bot1 = bot1Factory();

                var firstPlayer = i % 2 == 0 ? bot0 : bot1;
                var secondPlayer = i % 2 == 0 ? bot1 : bot0;

                var gameControl = new GameControl($"{simulationID}", rules, firstPlayer, secondPlayer);

                var finishReason = gameControl.Run();

                lock (objectLock)
                {
                    if (finishReason == FinishReason.OnePlayerWon)
                    {
                        //Debug.WriteLine($"{gameControl.Winner.id} won");

                        if (gameControl.Winner == bot0)
                        {
                            if (bot0.Color == PieceColor.White)
                            {
                                output.player0WinsWhite += 1;
                            }
                            else if (bot0.Color == PieceColor.Black)
                            {
                                output.player0WinsBlack += 1;
                            }
                        }
                        else if (gameControl.Winner == bot1)
                        {
                            if (bot1.Color == PieceColor.White)
                            {
                                output.player1WinsWhite += 1;
                            }
                            else if (bot1.Color == PieceColor.Black)
                            {
                                output.player1WinsBlack += 1;
                            }
                        }
                    }
                    else if (finishReason == FinishReason.MoveLimitReached)
                    {
                        output.ties += 1;
                    }

                    done += 1;
                    Console.CursorLeft = 0;
                    Console.Write($"[{simulationID}] {done}/{numberOfRuns}".PadRight(30));
                }
            });

            Console.CursorLeft = 0;
            Console.Write(new string(' ', 30));
            Console.CursorLeft = 0;


            return output;
        }
    }
}
