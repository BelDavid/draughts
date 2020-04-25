﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Draughts.Pieces;
using Draughts.BoardEvaluators;

namespace Draughts.Players
{
    class MinimaxBot : Player
    {
        public MinimaxBot(int maxDepth, IBoardEvaluator evaluator, ProgressBar progressBar)
        {
            this.maxDepth = maxDepth;
            this.progressBar = progressBar;
            this.evaluator = evaluator;
        }

        private readonly int maxDepth;
        private readonly int reportDepth = 3;
        private readonly ProgressBar progressBar;
        private readonly IBoardEvaluator evaluator;

        public override Move MakeMove(BoardState boardState)
        {
            double progress = 0d;

            var expandedStates = new Stack<BoardState>();
            expandedStates.Push(boardState);

            var move = Search(boardState, 0, ref progress, 100d, new Dictionary<BoardState, FitMove>(), expandedStates).move;

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

        private FitMove Search(BoardState state, int depth, ref double progress, double progressDelta, Dictionary<BoardState, FitMove> cache, Stack<BoardState> expandedStates)
        {
            if (cache.ContainsKey(state))
            {
                progress += progressDelta;

                if (depth <= reportDepth)
                {
                    ReportProgress(progress);
                }

                return cache[state];
            }

            if (depth == maxDepth)
            {
                progress += progressDelta;
                if (depth <= reportDepth)
                {
                    ReportProgress(progress);
                }

                return new FitMove(evaluator.Evaluate(state, rules), null);
            }
            else
            {

                var moves = state.GetAvaiableMoves();

                if (moves == null || moves.Count == 0)
                {
                    progress += progressDelta;
                    return new FitMove(state.OnMove == PieceColor.White ? double.MinValue : double.MaxValue, null);
                }

                if (depth == 0 && moves.Count == 1)
                {
                    progress += progressDelta;
                    return new FitMove(0, moves[0]);
                }

                var fitmoves = new List<FitMove>();

                foreach (var move in moves)
                {
                    if (move != null)
                    {
                        var s = state.ApplyMove(move);

                        if (expandedStates.Contains(s))
                        {
                            continue;
                        }

                        expandedStates.Push(s);

                        double fit = Search(s, depth + 1, ref progress, progressDelta / moves.Count, cache, expandedStates).fit;

                        expandedStates.Pop();
                        var fm = new FitMove(fit, move);

                        fitmoves.Add(fm);
                    }
                }

                if (depth == reportDepth)
                {
                    ReportProgress(progress);
                }

                var minmax =
                    state.OnMove == PieceColor.White
                    ? fitmoves.Max(fm => fm.fit)
                    : fitmoves.Min(fm => fm.fit);

                var candidates = from fm in fitmoves where fm.fit >= minmax - .001d && fm.fit <= minmax + .001d select fm;

                // random fitmove from candidates
                var fitmove = candidates.ElementAt(Utils.rand.Next(candidates.Count()));
                
                cache.Add(state, fitmove);

                return fitmove;
            }
        }

        private struct FitMove
        {
            public double fit;
            public Move move;

            public FitMove(double fit, Move move)
            {
                this.fit = fit;
                this.move = move;
            }
        }
    }
}
