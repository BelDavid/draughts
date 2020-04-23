using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Draughts.Pieces;
using Draughts.Players.AI;

namespace Draughts.Players
{
    class MinimaxBot : Player
    {
        public MinimaxBot(int maxDepth, BoardEvaluatorType evaluator, ProgressBar progressBar)
        {
            this.maxDepth = maxDepth;
            this.progressBar = progressBar;
            this.evaluator = evaluator;
        }

        private readonly int maxDepth;
        private readonly int reportDepth = 3;
        private readonly ProgressBar progressBar;
        private readonly BoardEvaluatorType evaluator;

        public override Move MakeMove(BoardState boardState)
        {
            double progress = 0d;

            var move = Search(boardState, 0, ref progress, 100d).move;

            return move;
        }

        private void ReportProgress(double progress)
        {
            if (progressBar != null)
            {
                progressBar.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = progress;
                });
            }
        }

        private (double fit, Move move) Search(BoardState state, int depth, ref double progress, double progressChunk)
        {
            if (depth == maxDepth)
            {
                progress += progressChunk;
                if (depth <= reportDepth)
                {
                    ReportProgress(progress);
                }

                return (BoardEvaluator.Evaluate(state, evaluator, rules), null);
            }
            else
            {
                var moves = state.GetAvaiableMoves();

                if (moves == null || moves.Count == 0)
                {
                    return (state.OnMove == PieceColor.White ? double.MinValue : double.MaxValue, null);
                }

                if (depth == 0 && moves.Count == 1)
                {
                    return (0, moves[0]);
                }

                var fmoves = new List<(double fit, Move move)>();
                foreach (var move in moves)
                {
                    if (move != null)
                    {
                        var s = state.ApplyMove(move);
                        (double f, Move m) = Search(s, depth + 1, ref progress, progressChunk / moves.Count);
                        fmoves.Add((f, move));
                    }
                }

                if (depth == reportDepth)
                {
                    ReportProgress(progress);
                }

                var minmax =
                    state.OnMove == PieceColor.White
                    ? fmoves.Max(fm => fm.fit)
                    : fmoves.Min(fm => fm.fit);

                var candidates = from fm in fmoves where fm.fit >= minmax - .001d && fm.fit <= minmax + .001d select fm;

                // return random move from candidates
                return candidates.ElementAt(Utils.rand.Next(candidates.Count()));
            }
        }
    }
}
