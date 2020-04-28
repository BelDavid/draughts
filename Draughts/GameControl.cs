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
        public readonly string id;

        public BoardState CurrentBoardState { get; private set; }
        public Player WhitePlayer { get; private set; }
        public Player BlackPlayer { get; private set; }

        public Player Winner { get; private set; }
        public bool IsRunning { get; private set; } = false;
        public bool IsTerminated { get; private set; } = false;
        public bool IsFinished { get; private set; } = false;

        private bool disposeVisualiserOnFinish = false;
        private object signaler_run = new object();

        public GameControl(string gameId, RulesType rules, Player whitePlayer, Player blackPlayer)
        {
            this.id = gameId ?? string.Empty;
            WhitePlayer = whitePlayer ?? throw new ArgumentNullException("WhitePlayer can not be null");
            BlackPlayer = blackPlayer ?? throw new ArgumentNullException("BlackPlayer can not be null");

            whitePlayer.Setup(PieceColor.White, rules, this);
            blackPlayer.Setup(PieceColor.Black, rules, this);

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
        public void Terminate(bool abortThread, bool disposeVisualiserOnFinish)
        {
            // TODO stop properly
            IsTerminated = true;
            if (abortThread)
            {
                gameThread?.Abort();
            }

            this.disposeVisualiserOnFinish = disposeVisualiserOnFinish;
            visualiser?.TerminateGame();
        }

        public Player Run()
        {
            IsRunning = true;

            for (int moveCount = 0; moveCount < MoveCountLimit && !IsTerminated; moveCount++)
            {
                var playerOnMove = players[moveCount % 2];

                var move = playerOnMove.MakeMove(CurrentBoardState);
                if (IsTerminated)
                {
                    break;
                }
                else if (move != null)
                {
                    CurrentBoardState = CurrentBoardState.ApplyMove(move);
                    MoveHistory.Add(move);

                    visualiser?.ApplyMove(move, !(playerOnMove is User));
                }
                else if (IsRunning)
                {
                    Winner = players[(moveCount + 1) % 2];
                    break;
                }
            }

            lock (signaler_run)
            {
                IsRunning = false;
                IsFinished = true;
                Monitor.PulseAll(signaler_run);
            }

            if (disposeVisualiserOnFinish && (!visualiser?.IsDisposed ?? false))
            {
                visualiser?.Dispose();
            }

            //System.Diagnostics.Debug.WriteLine($"Game {gameId} Finished");
            return Winner;
        }

        public Player Await()
        {
            if (visualiser != null && visualiser.Dispatcher.Thread == Thread.CurrentThread)
            {
                throw new Exception("This method should not be called from visualiser's Dispatcher thread, since it can block the thread.");
            }

            lock (signaler_run)
            {
                while (!IsFinished)
                {
                    Monitor.Wait(signaler_run);
                }
            }
            return Winner;
        }
    }
}