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

            //thread = new Thread(() => Simulate(
            //    RulesType.Czech,
            //    PlayerFactories.MinimaxBotFactory(7, new BoardEvaluatorBasic(), progressbar_bot),
            //    PlayerFactories.MinimaxBotFactory(5, new BoardEvaluatorBasic(), progressbar_bot),
            //    10
            //));

            InitGame();
        }

        // Testing
        private Visualiser visualiser;
        private List<GameControl> gameControls;

        private Thread thread;

        private void InitGame()
        {
            TerminateGame(false);
            gameControls = new List<GameControl>();

            const int minimaxDepth = 10;
            for (int i = 0; i < 32; i++)
            {
                var gameControl = new GameControl(null, RulesType.Czech, new MinimaxBot(minimaxDepth, new BoardEvaluatorBasic(), progressbar_bot), new MinimaxBot(minimaxDepth, new BoardEvaluatorBasic(), progressbar_bot));
                gameControls.Add(gameControl);
            }
            visualiser = null; //gameControl.GetVisualiser(canvas_board);
        }

        private void TerminateGame(bool force)
        {
            if (gameControls == null)
            {
                return;
            }
            foreach (var gameControl in gameControls)
            {
                gameControl.Terminate(force, !force);
            }
            
            gameControls = null;
            visualiser = null;
        }
        /*
        private void Simulate(RulesType rules, PlayerFactory whitePlayerFactory, PlayerFactory blackPlayerFactory, int numberOfRuns)
        {
            TerminateGame(false);
            Debug.WriteLine("Simulation started");

            int whiteWins = 0;
            int blackWins = 0;
            int tieCount = 0;

            for (int i = 0; i < numberOfRuns; i++)
            {
                var whitePlayer = whitePlayerFactory();
                var blackPlayer = blackPlayerFactory();

                gameControl = new GameControl($"simulation{i}", rules, whitePlayer, blackPlayer);

                canvas_board.Dispatcher.Invoke(() =>
                {
                    visualiser = gameControl.GetVisualiser(canvas_board);
                    visualiser.animationSpeed = 10;
                });

                var winner = gameControl.Run();

                Thread.Sleep(2000);

                visualiser?.Dispose();

                if (winner == PieceColor.White)
                {
                    Debug.WriteLine("White player won");
                    whiteWins += 1;
                }
                else if (winner == PieceColor.Black)
                {
                    Debug.WriteLine("Black player won");
                    blackWins += 1;
                }
                else
                {
                    Debug.WriteLine("Tie");
                    tieCount += 1;
                }
            }

            Debug.WriteLine("Final score:");
            Debug.WriteLine($"#white wins = {whiteWins}");
            Debug.WriteLine($"#black wins = {blackWins}");
            Debug.WriteLine($"#ties = {tieCount}");
        }
        */
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            TerminateGame(true);
            thread?.Abort();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var gc in gameControls)
            {
                gc.Start();
            }
            thread?.Start();
        }

        private void Menu_exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Menu_new_Click(object sender, RoutedEventArgs e)
        {
            InitGame();
            foreach (var gc in gameControls)
            {
                gc.Start();
            }
        }

        private void Menu_log_Click(object sender, RoutedEventArgs e)
        {
            visualiser.Dispose();
        }
    }
}