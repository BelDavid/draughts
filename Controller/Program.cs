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
            //TrainEvA();
            RunSimulation();

            Console.Write("Press Enter to exit..."); 
            Console.ReadLine();
        }

        private static void TrainEvA()
        {
            string evaID = "test06";
            var eva = new EvolutionaryAlgorithm(evaID, new int[] { 8 }, RulesType.Czech)
            {
                paralelisedMatches = true,
                minimaxDepth = 3,
                numberOfGenerations = 200,
                populationSize = 50,
                mutationRate = .2d,
                mutationBitRate = .2d,
                mutationScatter = 4d,
                crossoverRate = .2d,
                numberOfCompetetiveMatches = 100,
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

            // Setup bots | created using lambda function, so they are not created, unless chosen
            const string bot_randomized_id = "randomized";
            Func<Bot> bot_randomized = () => new Bot
            {
                id = bot_randomized_id,
                botFactory = _ => new RandomizedBot(bot_randomized_id),
                description = $@"- {bot_randomized_id}
   - Plays random moves
"
            };

            const string bot_minimax_basic_id = "minimax_basic";
            Func<Bot> bot_minimax_basic = () => new Bot
            {
                id = bot_minimax_basic_id,
                botFactory = depth => new MinimaxBot(bot_minimax_basic_id, depth, new BoardEvaluatorBasic(), null),
                description = $@"- {bot_minimax_basic_id}:
   - Depth-limited minimax with state evaluation:
      - White man:   {BoardEvaluatorBasic.weightMan}
      - White King:  {BoardEvaluatorBasic.weightKing}
      - Black man:  -{BoardEvaluatorBasic.weightMan}
      - Black King: -{BoardEvaluatorBasic.weightKing}
"
            };

            const string bot_minimax_basic_depth_2_id = "minimax_basic_depth=2";
            Func<Bot> bot_minimax_basic_depth_2 = () => new Bot
            {
                id = bot_minimax_basic_depth_2_id,
                botFactory = _ => new MinimaxBot(bot_minimax_basic_depth_2_id, 2, new BoardEvaluatorBasic(), null),
                description = $@"- {bot_minimax_basic_depth_2_id}:
   - Depth-limited minimax with state evaluation and depth fixed at 2:
      - White man:   {BoardEvaluatorBasic.weightMan}
      - White King:  {BoardEvaluatorBasic.weightKing}
      - Black man:  -{BoardEvaluatorBasic.weightMan}
      - Black King: -{BoardEvaluatorBasic.weightKing}
"
            };

            const string bot_minimax_basic1_id = "minimax_basic1";
            Func<Bot> bot_minimax_basic1 = () => new Bot
            {
                id = bot_minimax_basic1_id,
                botFactory = depth => new MinimaxBot(bot_minimax_basic1_id, depth, new BoardEvaluatorBasic1(), null),
                description = $@"- {bot_minimax_basic1_id}:
   - Depth-limited minimax with state evaluation:
      - White man:   {BoardEvaluatorBasic1.weightMan}
      - White King:  {BoardEvaluatorBasic1.weightKing}
      - Black man:  -{BoardEvaluatorBasic1.weightMan}
      - Black King: -{BoardEvaluatorBasic1.weightKing}
"
            };

            const string bot_minimax_progressive_id = "minimax_progressive";
            Func<Bot> bot_minimax_progressive = () => new Bot
            {
                id = bot_minimax_progressive_id,
                botFactory = depth => new MinimaxBot(bot_minimax_progressive_id, depth, new BoardEvaluatorProgressive(), null),
                description = $@"- {bot_minimax_progressive_id}:
   - Depth-limited minimax with state evaluation:
      - White man:   2 + ( (1 - row / (#rows - 1)) - 1)^3 +1)
      - White King:  5
      - Black man:  -2 - ( (    row / (#rows - 1)) - 1)^3 +1)
      - Black King: -5
"
            };

            var netId0 = $"test02/gen120_net0";
            const string bot_minimax_neural_network0_id = "minimax_neural_network";
            Func<Bot> bot_minimax_neural_network0 = () => new Bot
            {
                id = bot_minimax_neural_network0_id,
                botFactory = depth => new MinimaxBot(bot_minimax_neural_network0_id, depth, new BoardEvaluatorNeuralNetwork(Utils.LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/{netId0}.{Utils.neuralNetworkFileExt}")), null),
                description = $@"- {bot_minimax_neural_network0_id}:
   - Depth-limited minimax with state evaluation:
      - Rated with neural network: {netId0}
"
            };

            var netId1 = $"test02/gen0_net0";
            const string bot_minimax_neural_network1_id = "minimax_neural_network";
            Func<Bot> bot_minimax_neural_network1 = () => new Bot
            {
                id = bot_minimax_neural_network1_id,
                botFactory = depth => new MinimaxBot(bot_minimax_neural_network1_id, depth, new BoardEvaluatorNeuralNetwork(Utils.LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/{netId1}.{Utils.neuralNetworkFileExt}")), null),
                description = $@"- {bot_minimax_neural_network1_id}:
   - Depth-limited minimax with state evaluation:
      - Rated with neural network: {netId1}
"
            };

            // Choose bots
            var bot0 = bot_minimax_basic();
            var bot1 = bot_minimax_progressive();

            // Setup game
            int numberOfGames = 500;

            // Setup log
            string simID = $"{bot0.id}_vs_{bot1.id}";
            string simFilePath = $"{simFolderPath}/{simID}.txt";
            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine("Bots:");
                sw.Write(bot0.description);
                sw.Write(bot1.description);

                sw.WriteLine();
                sw.WriteLine($"Number of games per simulation: {numberOfGames}");
                sw.WriteLine(separator);
            }

            // Run
            for (int i = 1; i < 5; i++)
            {
                run($"run{i}", i);
            }
            void run(string runID, int depth)
            {
                var simOut = SimulateParallel(
                    runID,
                    RulesType.Czech,
                    () => bot0.botFactory(depth),
                    () => bot1.botFactory(depth),
                    numberOfGames
                );
                string message = $"[{runID}] {bot0.id}: {simOut.player0Wins} (w:{simOut.player0WinsWhite} b:{simOut.player0WinsBlack}) | ties: {simOut.ties} | {bot1.id}: {simOut.player1Wins} (w:{simOut.player1WinsWhite} b:{simOut.player1WinsBlack})";
                Console.WriteLine(message);
                using (var sw = new StreamWriter(simFilePath, true))
                {
                    sw.WriteLine($"Run: {runID}, Depth: {depth}");

                    sw.WriteLine($"Score: '{bot0.id}' wins: {simOut.player0Wins} (w: {simOut.player0WinsWhite}, b: {simOut.player0WinsBlack}) | Ties: {simOut.ties} | '{bot1.id}' wins: {simOut.player1Wins} (w: {simOut.player1WinsWhite}, b: {simOut.player1WinsBlack})");

                    sw.WriteLine(separator);
                }
            }

            // Finalise
            using (var sw = new StreamWriter(simFilePath, true))
            {
                sw.WriteLine(separator);
            }
        }

        private struct Bot
        {
            public string id;
            public Func<int, Player> botFactory; // depth, out Player
            public string description;
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
