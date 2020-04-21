using Draughts.Pieces;
using Draughts.Players;
using Draughts.Rules;
using Draughts.Visualisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Draughts
{
   public class GameControl
   {
      private Visualiser visualiser;
      private readonly Player[] players;
      private readonly Thread gameThread;

      public readonly GameRules gameRules;
      public readonly List<Move> MoveHistory = new List<Move>();
      public BoardState CurrentBoardState { get; private set; }
      public Player WhitePlayer { get; private set; }
      public Player BlackPlayer { get; private set; }



      public GameControl(RulesType rules, Player whitePlayer, Player blackPlayer)
      {

         WhitePlayer = whitePlayer ?? throw new ArgumentNullException("Argument whitePlayer can not be null");
         BlackPlayer = blackPlayer ?? throw new ArgumentNullException("Argument blackPlayer can not be null");

         whitePlayer.Color = PieceColor.White;
         blackPlayer.Color = PieceColor.Black;

         gameRules = Utils.GetGameRules(rules);


         players = gameRules.GetStartingColor() == PieceColor.White ?
            new Player[] { whitePlayer, blackPlayer } :
            new Player[] { blackPlayer, whitePlayer };

         CurrentBoardState = gameRules.GetInitialBoardState();

         gameThread = new Thread(Run);
      }

      public Visualiser GetVisualiser(Canvas canvasBoard)
      {
         if (visualiser == null)
         {
            visualiser = new Visualiser(canvasBoard, this);
            visualiser.Refresh();
         }
         return visualiser;
      }

      public void Start()
      {
         gameThread.Start();
      }
      public void Stop()
      {
         gameThread.Abort();
      }

      public void Run()
      {
         for (int moveCount = 0; ; moveCount++)
         {
            var playerOnMove = players[moveCount % 2];

            var move = playerOnMove.MakeMove(CurrentBoardState);
            if (move != null)
            {
               CurrentBoardState = CurrentBoardState.ApplyMove(move);
               MoveHistory.Add(move);

               visualiser?.ApplyMove(move, !(playerOnMove is User));
            }
            else
            {
               // TODO game is over
               break;
            }
         }
      }
   }
}