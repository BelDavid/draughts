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
        public const int MoveCountLimit = 150;

        private Visualiser visualiser;
        private readonly Player[] players;
        private readonly Thread gameThread;

        public readonly GameRules gameRules;
        public readonly List<Move> MoveHistory = new List<Move>();
        public BoardState CurrentBoardState { get; private set; }
        public Player WhitePlayer { get; private set; }
        public Player BlackPlayer { get; private set; }

        public PieceColor WinnerColor { get; private set; } = PieceColor.None;
        public bool IsRunning { get; private set; } = false;
        public bool IsFinished { get; private set; } = false;
        private object signaler = new object();

        public GameControl(RulesType rules, Player whitePlayer, Player blackPlayer)
        {
            WhitePlayer = whitePlayer ?? throw new ArgumentNullException("WhitePlayer can not be null");
            BlackPlayer = blackPlayer ?? throw new ArgumentNullException("BlackPlayer can not be null");

            whitePlayer.Setup(PieceColor.White, rules);
            blackPlayer.Setup(PieceColor.Black, rules);

            gameRules = Utils.GetGameRules(rules);

            players = gameRules.GetStartingColor() == PieceColor.White
               ? new Player[] { whitePlayer, blackPlayer }
               : new Player[] { blackPlayer, whitePlayer };

            CurrentBoardState = gameRules.GetInitialBoardState();

            gameThread = new Thread(() => Run());
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
            // TODO stop properly
            gameThread.Abort();
            Thread.Sleep(1500);
        }

        public PieceColor Run()
        {
            lock (signaler)
            {
                IsRunning = true;
            }

            for (int moveCount = 0; ; moveCount++)
            {
                var playerOnMove = players[moveCount % 2];

                if (moveCount >= MoveCountLimit)
                {
                    WinnerColor = PieceColor.None;
                    break;
                }

                var move = playerOnMove.MakeMove(CurrentBoardState);
                if (move != null)
                {
                    CurrentBoardState = CurrentBoardState.ApplyMove(move);
                    MoveHistory.Add(move);

                    visualiser?.ApplyMove(move, !(playerOnMove is User));
                }
                else
                {
                    WinnerColor = Utils.SwapColor(playerOnMove.Color);
                    break;
                }
            }

            lock (signaler)
            {
                IsRunning = false;
                IsFinished = true;
                Monitor.PulseAll(signaler);
            }
            return WinnerColor;
        }

        public PieceColor Await()
        {
            lock (signaler)
            {
                while (!IsFinished)
                {
                    Monitor.Wait(signaler);
                }
                return WinnerColor;
            }
        }
    }
}