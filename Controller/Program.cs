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
            //string simID = "basic_vs_basic(d=2)";
            string simID = "basic_vs_random";

            const string simFolderPath = "../../../local/sim";
            if (!Directory.Exists(simFolderPath))
            {
                Directory.CreateDirectory(simFolderPath);
            }

            // Load
            //var netId0 = $"{id}/gen29_net0";
            //var nn0 = Utils.LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/run_{netId0}.{Utils.neuralNetworkFileExt}");

            // Setup
            int numberOfGames = 500;
            string simFilePath = $"{simFolderPath}/{simID}.txt";

            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine("Bots:");
                sw.Write(
@"- basic:
   - Depth-limited minimax with state evaluation:
      - White man:   1
      - White King:  5
      - Black man:  -1
      - Black King: -5
");
                sw.Write(
@"- random:
   - Plays random moves
");
                //                sw.Write(
                //@"- basic(d=2):
                //   - Depth-limited minimax with state evaluation and depth fixed at 2:
                //      - White man:   1
                //      - White King:  5
                //      - Black man:  -1
                //      - Black King: -5
                //");
                sw.WriteLine();
                sw.WriteLine($"Number of games per simulation: {numberOfGames}");
                sw.WriteLine(separator);
            }

            // Run
            for (int i = 1; i < 7; i++)
            {
                run($"run{i}", i);
            }
            void run(string runID, int depth)
            {
                string bot0Id = null;
                string bot1Id = null;

                var simOut = SimulateParallel(
                    runID,
                    RulesType.Czech,
                    //() => new MinimaxBot(bot0Id = netId0, depth, new BoardEvaluatorNeuralNetwork(nn0), null),
                    () => new MinimaxBot(bot0Id = "basic", depth, new BoardEvaluatorBasic(), null),

                    //() => new MinimaxBot(bot1Id = "basic(d=2)", 2, new BoardEvaluatorBasic(), null),
                    //() => new MinimaxBot(bot1Id = "basic1", depth, new BoardEvaluatorBasic1(), null),
                    () => new RandomizedBot(bot1Id = "random"),
                    numberOfGames
                );
                string message = $"[{runID}] {bot0Id}: {simOut.player0Wins} (w:{simOut.player0WinsWhite} b:{simOut.player0WinsBlack}) | ties: {simOut.ties} | {bot1Id}: {simOut.player1Wins} (w:{simOut.player1WinsWhite} b:{simOut.player1WinsBlack})";
                Console.WriteLine(message);
                using (var sw = new StreamWriter(simFilePath, true))
                {
                    sw.WriteLine($"runID: {runID}");
                    sw.WriteLine($"depth: {depth}");

                    sw.WriteLine($"{bot0Id} wins: {simOut.player0Wins} [white: {simOut.player0WinsWhite}, black: {simOut.player0WinsBlack}]");
                    sw.WriteLine($"ties: {simOut.ties}");
                    sw.WriteLine($"{bot1Id} wins: {simOut.player1Wins} [white: {simOut.player1WinsWhite}, black: {simOut.player1WinsBlack}]");

                    sw.WriteLine(separator);
                }
            }

            // Finalise
            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine();
                sw.WriteLine(separator);
            }
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
