//#define SIMULATION
#define EVA

using Draughts.Game;
using Draughts.Pieces;
using Draughts.Players;
using Draughts.BoardEvaluators;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using Draughts.GUI;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using Microsoft.SqlServer.Server;
using System.Windows.Threading;

namespace Draughts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml          <see langword="static"/>         
    /// </summary>
    public partial class MainWindow : Window
    {
        private Visualiser visualiser;
        private GameControl gameControl;
        private readonly object signaler_output = new object();

        private Thread[] threads;
        private readonly IFormatter formatter = new BinaryFormatter();


        public MainWindow()
        {
            InitializeComponent();

#if SIMULATION || EVA
            if (!Directory.Exists(Utils.localFolderLocation))
            {
                Directory.CreateDirectory(Utils.localFolderLocation);
            }
#endif

#if SIMULATION
            int numberOfGames = 100;
            threads = new Thread[]
            {
                new Thread(() => Simulate(
                    "game0",
                    RulesType.Czech,
                    () => new MinimaxBot("botBasic5", 5, new BoardEvaluatorBasic(), null, true, true),
                    () => new MinimaxBot("botBasic3", 3, new BoardEvaluatorBasic(), null, true, true),
                    numberOfGames,
                    null,
                    true
                )),
                new Thread(() => Simulate(
                    "game1",
                    RulesType.Czech,
                    () => new MinimaxBot("botBasic5", 5, new BoardEvaluatorBasic(), null, true, true),
                    () => new MinimaxBot("botBasic3", 3, new BoardEvaluatorBasic(), null, true, true),
                    numberOfGames,
                    null,
                    true
                )),
                new Thread(() => Simulate(
                    "game2",
                    RulesType.Czech,
                    () => new MinimaxBot("botBasic5", 5, new BoardEvaluatorBasic(), null, true, true),
                    () => new MinimaxBot("botBasic3", 3, new BoardEvaluatorBasic(), null, true, true),
                    numberOfGames,
                    null,
                    true
                )),
                new Thread(() => Simulate(
                    "game3",
                    RulesType.Czech,
                    () => new MinimaxBot("botBasic5", 5, new BoardEvaluatorBasic(), null, true, true),
                    () => new MinimaxBot("botBasic3", 3, new BoardEvaluatorBasic(), null, true, true),
                    numberOfGames,
                    null,
                    true
                )),
            };
            menu.IsEnabled = false;
#endif

#if EVA
            threads = new Thread[]
            {
                new Thread(TrainNNWithEvAlg),
            };
            menu.IsEnabled = false;
#endif
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (threads != null)
            {
                foreach (var thread in threads)
                {
                    thread?.Start();
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            TerminateGame(true);
            if (threads != null)
            {
                foreach (var thread in threads)
                {
                    thread?.Abort();
                }
            }
        }

        private void Menu_exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        public static (int, int, int) Simulate(string id, RulesType rules, Func<Player> bot0Factory, Func<Player> bot1Factory, int numberOfRuns, MainWindow mainWindow, bool log)
        {
            if (mainWindow != null)
            {
                mainWindow.TerminateGame(false);
            }

            if (log)
            {
                Debug.WriteLine($"[{id}] simulation started");
            }

            int bot0Wins = 0;
            int bot1Wins = 0;
            int tieCount = 0;

            string bot0Id = null;
            string bot1Id = null;

            for (int i = 0; i < numberOfRuns; i++)
            {
                var bot0 = bot0Factory();
                var bot1 = bot1Factory();

                if (bot0Id is null) { bot0Id = bot0.id; }
                if (bot1Id is null) { bot1Id = bot1.id; }

                var firstPlayer = i % 2 == 0 ? bot0 : bot1;
                var secondPlayer = i % 2 == 0 ? bot1 : bot0;

                var gameControl = new GameControl($"{id}", rules, firstPlayer, secondPlayer);
                Visualiser visualiser = null;

                if (mainWindow != null)
                {
                    mainWindow.Dispatcher.Invoke(() =>
                    {
                        visualiser = gameControl.GetVisualiser(mainWindow);
                        visualiser.animationSpeed = 10;
                    });
                }

                var finishReason = gameControl.Run();

                if (visualiser != null)
                {
                    Thread.Sleep(2000);
                }

                visualiser?.Dispose();

                if (finishReason == FinishReason.OnePlayerWon)
                {
                    if (gameControl.Winner == bot0)
                    {
                        bot0Wins += 1;

                        if (log)
                        {
                            Debug.WriteLine($"[{id}] '{bot0Id}' won");
                        }
                    }
                    else if (gameControl.Winner == bot1)
                    {
                        bot1Wins += 1;
                        if (log)
                        {
                            Debug.WriteLine($"[{id}] '{bot1Id}' won");
                        }
                    }
                }
                else if (finishReason == FinishReason.MoveLimitReached)
                {
                    tieCount += 1;
                    if (log)
                    {
                        Debug.WriteLine($"[{id}] tie");
                    }
                }
            }

            if (log)
            {
                lock (mainWindow.signaler_output)
                {
                    Debug.WriteLine($"[{id}] Final score ({numberOfRuns} runs):");
                    Debug.WriteLine($"[{id}] #'{bot0Id}' wins = {bot0Wins}");
                    Debug.WriteLine($"[{id}] #'{bot1Id}' wins = {bot1Wins}");
                    Debug.WriteLine($"[{id}] #ties = {tieCount}");
                    Debug.WriteLine($"[{id}] balance = {(bot0Wins - bot1Wins) / (float)numberOfRuns}");

                    using (var sw = new StreamWriter($"{Utils.localFolderLocation}/simulation_output.txt", true))
                    {
                        sw.WriteLine($"[{id}] Final score ({numberOfRuns} runs):");
                        sw.WriteLine($"[{id}] #'{bot0Id}' wins = {bot0Wins}");
                        sw.WriteLine($"[{id}] #'{bot1Id}' wins = {bot1Wins}");
                        sw.WriteLine($"[{id}] #ties = {tieCount}");
                        sw.WriteLine($"[{id}] balance = {(bot0Wins - bot1Wins) / (float)numberOfRuns}");
                        sw.WriteLine($"---------------------------------------------");
                    }
                }
            }

            return (bot0Wins, bot1Wins, tieCount);
        }

        private void TrainNNWithEvAlg()
        {
            TerminateGame(false);
            var eva = new EvolutionaryAlgorithm("test", () => NeuralNetwork.GetNetworkWithRandomizedWeights(new int[] { 64, 10, 10, 1 }, i => i), RulesType.Czech)
            {
                minimaxDepth = 2,
                populationSize = 10,
            };
            var gen = eva.Run(50);

            Dispatcher.Invoke(() =>
            {
                gameControl = new GameControl("nngame", RulesType.Czech, new User("user"), new MinimaxBot("nn2", 2, new BoardEvaluatorNeuralNetwork(_ => gen.First().neuralNetwork), progressbar_bot));
                visualiser = gameControl.GetVisualiser(this);
                menu.IsEnabled = true;
            });

            gameControl?.Run();
        }


        private void TerminateGame(bool force)
        {
            gameControl?.Terminate(force, !force);
            
            SetEndMessage(null);
            gameControl = null;
            visualiser = null;
        }

        public void SetEndMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(message))
                {
                    label_endMessage.Content = "";
                    label_endMessage.Visibility = Visibility.Hidden;
                }
                else
                {
                    label_endMessage.Content = message;
                    label_endMessage.Visibility = Visibility.Visible;
                }
            });
        }

        
        private void Menu_new_2users_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SelectorWindow(GameType.Local, this);
            selector.ShowDialog();

            if (selector.Sucess)
            {
                TerminateGame(false);

                gameControl = new GameControl("user_vs_user", selector.rules, new User("user0"), new User("user1"));
                visualiser = gameControl.GetVisualiser(this);

                // TODO change window title

                gameControl.Start();
            }
        }        
        private void Menu_new_bot_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SelectorWindow(GameType.AgainstBot, this);
            selector.ShowDialog();

            if (selector.Sucess)
            {
                TerminateGame(false);

                Player user = new User("user");
                Player bot;
                switch (selector.botDifficulty)
                {
                    case BotDifficulty.Randomized:
                        bot = new RandomizedBot("randbot");
                        break;
                    case BotDifficulty.Easy:
                        bot = new MinimaxBot("minimax3", 3, new BoardEvaluatorBasic(), progressbar_bot, true, true);
                        break;
                    case BotDifficulty.Medium:
                        bot = new MinimaxBot("minimax7", 7, new BoardEvaluatorBasic(), progressbar_bot, true, true);
                        break;
#if DEBUG
                    case BotDifficulty.Depth10:
                        bot = new MinimaxBot("minimax10", 10, new BoardEvaluatorBasic(), progressbar_bot, true, true);
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
                
                Player whitePlayer = selector.color == PieceColor.White ? user : bot;
                Player blackPlayer = selector.color == PieceColor.White ? bot : user;

                gameControl = new GameControl("user_vs_bot", selector.rules, whitePlayer, blackPlayer);
                visualiser = gameControl.GetVisualiser(this);

                // TODO change window title

                gameControl.Start();
            }
        }

        private void Menu_new_network_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SelectorWindow(GameType.OverNetwork, this);
            selector.ShowDialog();

            if (selector.Sucess)
            {
                TerminateGame(false);

                //gameControl = new GameControl("user_vs_user", selector.rules, new User("user0"), new User("user1"));
                //visualiser = gameControl.GetVisualiser(canvas_board);

                // TODO change window title

                //gameControl.Start();
            }
        }

        private void Menu_new_replay_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SelectorWindow(GameType.Replay, this);
            selector.ShowDialog();

            if (selector.Sucess)
            {
                TerminateGame(false);

                try
                {
                    using (var fs = new FileStream(selector.filePath, FileMode.Open, FileAccess.Read))
                    {
                        var gameReplay = (GameReplay)formatter.Deserialize(fs);

                        gameControl = new GameControl("replay", gameReplay, selector.animationSpeed, label_pause);
                        visualiser = gameControl.GetVisualiser(this);

                        switch (selector.animationSpeed)
                        {
                            case AnimationSpeed.Manual:
                                break;

                            case AnimationSpeed.Slow:
                                visualiser.animationSpeed = 2;
                                break;

                            case AnimationSpeed.Medium:
                                visualiser.animationSpeed = 5;
                                break;

                            case AnimationSpeed.Fast:
                                visualiser.animationSpeed = 20;
                                break;

                            default:
                                throw new NotImplementedException();
                        }

                        gameControl.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deserializing Replay from file {selector.filePath}\n{ex.Message}");
                }
            }
        }


        private void Menu_saveReplay_Click(object sender, RoutedEventArgs e)
        {
            if (gameControl != null)
            {
                if (gameControl.IsFinished)
                {
                    try
                    {
                        var fd = new SaveFileDialog() {
                            OverwritePrompt = true,
                            ValidateNames = true,
                            DefaultExt = $".{Utils.replayFileExt}",
                            Filter = $"*.{Utils.replayFileExt} files|*.{Utils.replayFileExt}",
                        };

                        if (fd.ShowDialog() ?? false)
                        {
                            var fs = new FileStream(fd.FileName, FileMode.Create, FileAccess.Write);
                            formatter.Serialize(fs, gameControl.GetReplay());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error serializing Replay\n{ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Game not finished yet!");
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            gameControl?.KeyPressed(e.Key);
        }

        private void Menu_help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
@"GamePlay:
 - Use Left mouse button to select piece and then to select place to move on
 - Use Right mouse button to deselect piece

Replay:
 - In manual mode, use Space to make next step, otherwise use Space to pause animation
",
"How to play");
        }
    }
}