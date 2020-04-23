using Draughts.Pieces;
using Draughts.Players;
using Draughts.Players.AI;
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

            //gameControl = new GameControl(RulesType.Czech, PlayerFactories.UserFactory(), PlayerFactories.MinimaxBotFactory(6, BoardEvaluatorType.Basic, progressbar_bot));
            //visualiser = gameControl.GetVisualiser(canvas_board);

            thread = new Thread(() => Simulate(
                (wpf, bpf) => new GameControl(RulesType.Czech, wpf, bpf),
                PlayerFactories.MinimaxBotFactory(1, BoardEvaluatorType.Basic, progressbar_bot),
                PlayerFactories.MinimaxBotFactory(3, BoardEvaluatorType.Basic, progressbar_bot),
                10
            ));
        }

        // Testing
        private Visualiser visualiser;
        private GameControl gameControl;

        private Thread thread;


        private void Simulate(GameControlFactory gameControlFactory, PlayerFactory whitePlayerFactory, PlayerFactory blackPlayerFactory, int numberOfRuns)
        {
            Debug.WriteLine("Simulation started");

            int whiteWins = 0;
            int blackWins = 0;
            int tieCount = 0;

            for (int i = 0; i < numberOfRuns; i++)
            {
                var gameControl = gameControlFactory(whitePlayerFactory, blackPlayerFactory);

                canvas_board.Dispatcher.Invoke(() =>
                {
                    visualiser = gameControl.GetVisualiser(canvas_board);
                    visualiser.animationSpeed = 10;
                });

                var winner = gameControl.Run();

                canvas_board.Dispatcher.Invoke(visualiser.Dispose);

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

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            gameControl?.Stop();
            thread?.Abort();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            gameControl?.Start();
            thread?.Start();
        }

        private void Menu_exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Menu_test_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_new_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}