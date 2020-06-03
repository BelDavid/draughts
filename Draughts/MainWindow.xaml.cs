
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

        public string defaultTitle = "Draughts";

        public ProgressBar ProgressBar_Bot => progressbar_bot;

        public MainWindow()
        {
            InitializeComponent();

            Title = defaultTitle;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            gameControl?.Start();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            TerminateGame(true);
        }

        private void Menu_exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        public void TerminateGame(bool force)
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
                        var nn = Utils.LoadNetwork(selector.neuralNetworkFilePath);
                        if (nn != null)
                        {
                            evaluator = new BoardEvaluatorNeuralNetwork(nn);
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case BoardEvaluatorType.RLModel:
                        RLModel model = Utils.LoadRLModel(selector.neuralNetworkFilePath);
                        if (model != null)
                        {
                            evaluator = new BoardEvaluatorRL(model);
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