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
        static void Main(string[] args)
        {
            var id = "train01";

#if EVA
            var eva = new EvolutionaryAlgorithm(id, new int[] { 10, 10, 10, }, RulesType.Czech)
            {
                paralelisedMatches = true,
                minimaxDepth = 3,
                numberOfGenerations = 50,
                populationSize = 30,
                mutationBitRate = 1d,
                crossoverRate = .5d,
                numberOfGameRounds = 100,
            };
            var gen = eva.Run();
#endif
#if RL
            var rl = new ReinforcementLearning();
            RLModel model = rl.TrainNewModel(500, 20, 2);
            model.Save("testmodelbig.h5");
#endif
#if SIM
            var netId1 = $"{id}/gen29_net0";
            var nn1 = Utils.LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/run_{netId1}.{Utils.neuralNetworkFileExt}");
            RLModel model = Utils.LoadRLModel("testmodelbig.h5");
            int numberOfGames = 100;

            void run(string simID, int depth)
            {
                string bot0Id = null;
                string bot1Id = null;


                var simOut = SimulateSerial(
                    simID,
                    RulesType.Czech,
                    () => new MinimaxBot(bot0Id = "rl", depth, new BoardEvaluatorRL(model), null),
                    () => new MinimaxBot(bot1Id = "basic", depth, new BoardEvaluatorNeuralNetwork(nn1), null),
                    numberOfGames
                );
                Console.WriteLine($"[{simID}] {bot0Id}: (w:{simOut.player0WinsWhite} b:{simOut.player0WinsBlack}) | ties: {simOut.ties} | {bot1Id}: (w:{simOut.player1WinsWhite} b:{simOut.player1WinsBlack})");
            }

            //run($"sim1", 1);
            //run($"sim2", 2);
            //run($"sim3", 3);
            //run($"sim4", 4);
            //run($"sim5", 5);

            for (int i = 0; i < 5; i++)
            {
                run($"sim7.{i}", i);
            }
#endif

            Console.Write("Press Enter to exit...");
            Console.ReadLine();
        }

        public static SimulationOutput SimulateSerial(string simulationID, RulesType rules, Func<Player> bot0Factory, Func<Player> bot1Factory, int numberOfRuns)
        {
            var output = new SimulationOutput() { total = numberOfRuns, histories = new List<List<BoardState>>() };

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

                    output.histories.Add(gameControl.StateHistory);
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
            Console.Write($"0/{numberOfRuns}");

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
                        output.histories.Add(gameControl.StateHistory);
                    }
                    else if (finishReason == FinishReason.MoveLimitReached)
                    {
                        output.ties += 1;
                    }

                    done += 1;
                    Console.CursorLeft = 0;
                    Console.Write($"{done}/{numberOfRuns}".PadRight(20));
                }
            });

            Console.CursorLeft = 0;
            Console.Write(new string(' ', 20));
            Console.CursorLeft = 0;


            return output;
        }
    }
}
