using Draughts.Pieces;
using Draughts.Players;
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

         gameControl = new GameControl(RulesType.Czech, new User(), new MinimaxBot(3, progressbar_bot));
         visualiser = gameControl.GetVisualiser(canvas_board);
      }

      // Testing
      private Visualiser visualiser;
      private GameControl gameControl;


      private void MainWindow_Closed(object sender, EventArgs e)
      {
         gameControl?.Stop();
      }

      private void MainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         gameControl?.Start();
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
         gameControl?.Stop();
         gameControl = null;

         if (!visualiser?.IsDisposed ?? false)
         {
            visualiser.Dispose();
            visualiser = null;
         }

         gameControl = new GameControl(RulesType.Czech, new RandomizedBot(), new RandomizedBot());
         visualiser = gameControl.GetVisualiser(canvas_board);

         gameControl.Start();
      }
   }
}