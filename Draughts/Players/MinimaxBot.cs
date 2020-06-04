﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Draughts.Pieces;
using Draughts.BoardEvaluators;
using System.Windows.Threading;
using Keras.Layers;
using System.Reflection;
using System.Windows.Media;

namespace Draughts.Players
{
    public class MinimaxBot : Player
    {
        public MinimaxBot(string id, int maxDepth, IBoardEvaluator evaluator, ProgressBar progressBar) : this(id, maxDepth, evaluator, progressBar, true, true) { }
        public MinimaxBot(string id, int maxDepth, IBoardEvaluator evaluator, ProgressBar progressBar, bool allowCaching, bool allowAlphaBetaCutting) : base(id)
        {
            this.maxDepth = maxDepth;
            this.progressBar = progressBar;
            this.allowCaching = allowCaching;
            this.allowAlphaBetaCutting = allowAlphaBetaCutting;
            this.evaluator = evaluator;
        }

        private readonly int maxDepth;
        private readonly int reportDepth = 3;
        private readonly ProgressBar progressBar;
        private readonly bool allowCaching;
        private readonly bool allowAlphaBetaCutting;
        private readonly IBoardEvaluator evaluator;
        public bool ruletteMoveSelection = false;

        protected override void AfterSetup()
        {
            base.AfterSetup();

            evaluator.Validate(game.gameRules);
        }

        public override Move MakeMove(BoardState boardState)
        {
            double progress = 0d;

            var expandedStates = new Stack<BoardState>();
            expandedStates.Push(boardState);

            var fitmove = Search(boardState, 0, ref progress, 100d, new Dictionary<BoardState, FitMove>(), expandedStates, null);
            //System.Diagnostics.Debug.WriteLine($"progressBar = {progress}");
            //System.Diagnostics.Debug.WriteLine($"fit = {fitmove.fit}");

            return fitmove.move;
        }

        private void ReportProgress(double progress)
        {
            if (progressBar != null)
            {
                progressBar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    progressBar.Value = progress;
                }));
            }
        }

        private FitMove Search(BoardState state, int depth, ref double progress, double progressDelta, Dictionary<BoardState, FitMove> cache, Stack<BoardState> expandedStates, double? bestFitOneUp)
        {
            // Checking for cached values
            if (allowCaching && cache.ContainsKey(state))
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

                return new FitMove(evaluator.Evaluate(state), null);
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
                var bestFit = state.OnMove == PieceColor.White ? double.MinValue : double.MaxValue;

                int i = 0;
                foreach (var move in moves)
                {
                    if (game.IsTerminated)
                    {
                        return new FitMove(0, null);
                    }

                    if (move != null)
                    {
                        var s = state.ApplyMove(move);

                        // Cycle
                        if (expandedStates.Contains(s))
                        {
                            continue;
                        }

                        expandedStates.Push(s);

                        double fit = Search(s, depth + 1, ref progress, progressDelta / moves.Count, cache, expandedStates, bestFit).fit;


                        expandedStates.Pop();
                        var fm = new FitMove(fit, move);

                        fitmoves.Add(fm);


                        // Alpha beta cutting
                        if (allowAlphaBetaCutting)
                        {
                            if (state.OnMove == PieceColor.White && fit > bestFit)
                            {
                                bestFit = fit;
                                if (bestFitOneUp != null && bestFit > bestFitOneUp)
                                {
                                    progress += (moves.Count - i - 1) * progressDelta / moves.Count;
                                    break;
                                }
                            }
                            else if (state.OnMove == PieceColor.Black && fit < bestFit)
                            {
                                bestFit = fit;
                                if (bestFitOneUp != null && bestFit < bestFitOneUp)
                                {
                                    progress += (moves.Count - i - 1) * progressDelta / moves.Count;
                                    break;
                                }
                            }
                        }
                    }

                    i++;
                }

                if (depth == reportDepth)
                {
                    ReportProgress(progress);
                }

                if (fitmoves.Count == 0)
                {
                    return new FitMove(0, null);
                }

                FitMove fitmove = default;
                if (ruletteMoveSelection)
                {
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    for (int j = 0; j < fitmoves.Count; j++)
                    {
                        var fm = fitmoves[j];
                        if (state.OnMove  ==  PieceColor.Black)
                        {
                            fm.fit *= -1;
                            fitmoves[j] = fm;
                        }

                        if (fm.fit < min)
                        {
                            min = fm.fit;
                        }
                        if (fm.fit > max)
                        {
                            max = fm.fit;
                        }
                    }

                    double delta = max - min;
                    double shift = - min + delta / 10;
                    min += shift;
                    max += shift;
                    for (int j = 0; j < fitmoves.Count; j++)
                    {
                        var fm = fitmoves[j];
                        
                        fm.fit += shift;

                        fitmoves[j] = fm;
                    }


                    var wheelSums = new double[fitmoves.Count];
                    double sum = 0;
                    for (int j = 0; j < fitmoves.Count; j++)
                        wheelSums[j] = sum += fitmoves[j].fit;

                    double r = Utils.rand.NextDouble() * sum;
                    int k = 0;
                    while (wheelSums[k] < r)
                    {
                        k++;
                    }
                    fitmove = fitmoves[k];
                }
                else
                {
                    var minmax =
                        state.OnMove == PieceColor.White
                        ? fitmoves.Max(fm => fm.fit)
                        : fitmoves.Min(fm => fm.fit);

                    var candidates = from fm in fitmoves where fm.fit >= minmax - .001d && fm.fit <= minmax + .001d select fm;

                    // Select random fitmove from candidates
                    fitmove = candidates.ElementAt(Utils.rand.Next(candidates.Count()));
                }

                // Caching
                if (allowCaching)
                {
                    cache.Add(state, fitmove);
                }

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
