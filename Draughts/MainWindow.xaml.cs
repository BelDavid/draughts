using Draughts.Pieces;
using Draughts.Players;
using Draughts.BoardEvaluators;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using Draughts.Visualisation;
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

namespace Draughts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml          <see langword="static"/>         
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //int numberOfGames = 100;
            //threads = new Thread[]
            //{
            //    new Thread(() => Simulate(
            //        "game0",
            //        RulesType.Czech,
            //        PlayerFactories.MinimaxBotFactory("minimax5", 5, new BoardEvaluatorBasic(), null, true, true),
            //        PlayerFactories.MinimaxBotFactory("minimax4", 4, new BoardEvaluatorBasic(), null, true, true),
            //        numberOfGames,
            //        null
            //    )),
            //};
        }

        // Testing
        private Visualiser visualiser;
        private GameControl gameControl;
        private object signaler_output = new object();

        private Thread[] threads;


        private void TerminateGame(bool force)
        {
            if (gameControl != null)
            {
                gameControl.Terminate(force, !force);
            }
            
            gameControl = null;
            visualiser = null;
        }

        private void Simulate(string id, RulesType rules, PlayerFactory firstPlayerFactory, PlayerFactory secondPlayerFactory, int numberOfRuns, Canvas canvas)
        {
            //TerminateGame(false);
            Debug.WriteLine($"[{id}] simulation started");

            int firstPlayerWins = 0;
            int secondPlayerWins = 0;
            int tieCount = 0;

            string firstPlayerId = null;
            string secondPlayerId = null;

            for (int i = 0; i < numberOfRuns; i++)
            {
                var firstPlayer = firstPlayerFactory();
                var secondPlayer = secondPlayerFactory();

                if (firstPlayerId is null)
                {
                    firstPlayerId = firstPlayer.id;
                }
                if (secondPlayerId is null)
                {
                    secondPlayerId = secondPlayer.id;
                }

                var whitePlayer = i % 2 == 0 ? firstPlayer : secondPlayer;
                var blackPlayer = i % 2 == 0 ? secondPlayer : firstPlayer;

                GameControl gameControl = new GameControl($"{id}", rules, whitePlayer, blackPlayer);
                Visualiser visualiser = null;

                if (canvas != null)
                {
                    canvas_board.Dispatcher.Invoke(() =>
                    {
                        visualiser = gameControl.GetVisualiser(canvas);
                        visualiser.animationSpeed = 10;
                    });
                }

                var winner = gameControl.Run();

                if (visualiser != null)
                {
                    Thread.Sleep(2000);
                }

                visualiser?.Dispose();

                if (winner == firstPlayer)
                {
                    firstPlayerWins += 1;
                    Debug.WriteLine($"[{id}] '{firstPlayerId}' player won");
                }
                else if (winner == secondPlayer)
                {
                    secondPlayerWins += 1;
                    Debug.WriteLine($"[{id}] '{secondPlayerId}' player won");
                }
                else
                {
                    tieCount += 1;
                    Debug.WriteLine($"[{id}] tie");
                }
            }

            lock (signaler_output)
            {
                Debug.WriteLine($"Final score ({id}):");
                Debug.WriteLine($"#'{firstPlayerId}' player wins = {firstPlayerWins}");
                Debug.WriteLine($"#'{secondPlayerId}' player wins = {secondPlayerWins}");
                Debug.WriteLine($"#ties = {tieCount}");
                Debug.WriteLine($"balance = {(firstPlayerWins - secondPlayerWins) / (float)numberOfRuns}");
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            gameControl?.Start();
            if (threads != null)
            {
                foreach (var thread in threads)
                {
                    thread?.Start();
                }
            }
        }

        private void Menu_exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        
        private void Menu_new_2users_Click(object sender, RoutedEventArgs e)
        {
            TerminateGame(false);

            // TODO rules selector
            gameControl = new GameControl("2users", RulesType.Czech, new User("user0"), new User("user1"));
            visualiser = gameControl.GetVisualiser(canvas_board);
            
            // TODO change window title

            gameControl.Start();
        }        
        private void Menu_new_bot_Click(object sender, RoutedEventArgs e)
        {
            TerminateGame(false);

            // TODO rules, side, difficulty selector
            TerminateGame(false);

            const int minimaxDepth = 7;
            gameControl =
                Utils.rand.Next(2) == 0
                ? new GameControl(null, RulesType.Czech, new User("user"), new MinimaxBot($"minmax{minimaxDepth}", minimaxDepth, new BoardEvaluatorBasic(), progressbar_bot, true, true))
                : new GameControl(null, RulesType.Czech, new MinimaxBot($"minmax{minimaxDepth}", minimaxDepth, new BoardEvaluatorBasic(), progressbar_bot, true, true), new User("user"));

            visualiser = gameControl.GetVisualiser(canvas_board);

            // TODO change window title

            gameControl.Start();
        }
    }
}