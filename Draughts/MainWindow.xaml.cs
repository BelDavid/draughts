//#define SIMULATION
//#define EVA

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

        private Thread[] threads;

        public string defaultTitle = "Draughts";


        public MainWindow()
        {
            InitializeComponent();

            Title = defaultTitle;

#if SIMULATION
            var net0 = "test2/gen9_net0";
            var net1 = "test2/gen0_net0";
            var nn0 = LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/run_{net0}.nn");
            var nn1 = LoadNetwork($"{EvolutionaryAlgorithm.folderPath_eva}/run_{net1}.nn");
            int numberOfGames = 1000;
            int depth = 1;

            void run(string simulationID)
            {
                string bot0Id = null;
                string bot1Id = null;

                var (networkWins, ties, basicWins) = Simulate(
                    simulationID,
                    RulesType.Czech,
                    () => new MinimaxBot(bot0Id = net0, depth, new BoardEvaluatorNeuralNetwork(nn0), null),
                    () => new MinimaxBot(bot1Id = net1, depth, new BoardEvaluatorNeuralNetwork(nn1), null),
                    //() => new MinimaxBot(bot1Id = "basic", depth, new BoardEvaluatorBasic(), null),
                    numberOfGames,
                    null
                );
                Debug.WriteLine($"[{simulationID}] {bot0Id}: {networkWins} | ties: {ties} | {bot1Id}: {basicWins}");
            }

            threads = new Thread[]
            {
                new Thread(() => run("sim0")),
                new Thread(() => run("sim1")),
                new Thread(() => run("sim2")),
                new Thread(() => run("sim3")),
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


        public static (int bot0Wins, int ties, int bot1Wins) Simulate(string simulationID, RulesType rules, Func<Player> bot0Factory, Func<Player> bot1Factory, int numberOfRuns, MainWindow mainWindow)
        {
            if (mainWindow != null)
            {
                mainWindow.TerminateGame(false);
            }

            int bot0Wins = 0;
            int tieCount = 0;
            int bot1Wins = 0;

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

                var gameControl = new GameControl($"{simulationID}", rules, firstPlayer, secondPlayer);
                Visualiser visualiser = null;

                if (mainWindow != null)
                {
                    var startingColor = gameControl.gameRules.GetStartingColor();

                    mainWindow.Dispatcher.Invoke(() =>
                    {
                        mainWindow.Title = $"{mainWindow.defaultTitle} | Simulation [{simulationID}]: {i+1}/{numberOfRuns} | {startingColor}: {firstPlayer.id} | {Utils.SwapColor(startingColor)}: {secondPlayer.id}";
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
                    }
                    else if (gameControl.Winner == bot1)
                    {
                        bot1Wins += 1;
                    }
                }
                else if (finishReason == FinishReason.MoveLimitReached)
                {
                    tieCount += 1;
                }
            }

            if (mainWindow != null)
            {
                mainWindow.Dispatcher.Invoke(() => { mainWindow.Title = mainWindow.defaultTitle; });
            }

            return (bot0Wins, tieCount, bot1Wins);
        }

        private void TrainNNWithEvAlg()
        {
            var id = "test3";

            TerminateGame(false);
            var eva = new EvolutionaryAlgorithm(id, new int[] { 10, 10, 10, 10, }, RulesType.Czech)
            {
                paralelisedMatches = true,
                minimaxDepth = 2,
            };
            var gen = eva.Run();

            var nn = gen.First().neuralNetwork;
            var depth = 1;
            var (network_wins, ties, basicEval_Wins) = Simulate(
                id,
                RulesType.Czech,
                () => new MinimaxBot($"network", depth, new BoardEvaluatorNeuralNetwork(nn), null),
                () => new MinimaxBot($"basic", depth, new BoardEvaluatorBasic(), null),
                1000,
                null
            );

            Debug.WriteLine($"[{id}] network: {network_wins} | ties: {ties} | basic: {basicEval_Wins}");

            gameControl?.Run();
        }


        public NeuralNetwork LoadNetwork(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    return (NeuralNetwork)Utils.binaryFormatter.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading from {path}\n{ex.Message}");
                return null;
            }
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

        
        private void Menu_new_local_Click(object sender, RoutedEventArgs e)
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

                IBoardEvaluator evaluator;
                switch (selector.boardEvaluator)
                {
                    case BoardEvaluatorType.Basic:
                        evaluator = new BoardEvaluatorBasic();
                        break;

                    case BoardEvaluatorType.NeuralNetwork:
                        var nn = LoadNetwork(selector.neuralNetworkFilePath);
                        if (nn != null)
                        {
                            evaluator = new BoardEvaluatorNeuralNetwork(nn);
                        }
                        else
                        {
                            return;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }

                switch (selector.botDifficulty)
                {
                    case BotDifficulty.Randomized:
                        bot = new RandomizedBot("randbot");
                        break;

                    case BotDifficulty.Easy:
                        bot = new MinimaxBot("minimax3", 3, evaluator, progressbar_bot, true, true);
                        break;

                    case BotDifficulty.Medium:
                        bot = new MinimaxBot("minimax7", 7, evaluator, progressbar_bot, true, true);
                        break;
#if DEBUG
                    case BotDifficulty.Depth10:
                        bot = new MinimaxBot("minimax10", 10, evaluator, progressbar_bot, true, true);
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

        private void Menu_new_online_Click(object sender, RoutedEventArgs e)
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
                    using (var fs = new FileStream(selector.replayFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var gameReplay = (GameReplay)Utils.binaryFormatter.Deserialize(fs);

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
                    MessageBox.Show($"Error deserializing Replay from file {selector.replayFilePath}\n{ex.Message}");
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
                            Utils.binaryFormatter.Serialize(fs, gameControl.GetReplay());
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