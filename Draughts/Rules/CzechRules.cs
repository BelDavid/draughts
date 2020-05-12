﻿using Draughts.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draughts.Utils;

namespace Draughts.Rules
{
   public class CzechRules : GameRules
   {
      public CzechRules() : base(RulesType.Czech) { }

      public override PieceColor GetStartingColor() => PieceColor.White;
      /* Key rules:
       * Pieces stay on the field until jumping sequence (JS) is finished
       * One piece can not be jumped over more than once
       * Promoting to king ends player's move
       * When only few kings are present and players doesn't seem to be able to catch one another, game ends with tie
       * Jumping is mandatory; JS done with king has priority before JS done with man; JS must be finished
       * King can jump to any place behind a piece, if none of those places lead to more jumping
      */

      public override List<Move> GetAvaiableMoves(BoardState state)
      {
         var takingMovesWithKings = new List<Move>();
         var takingMovesWithMen = new List<Move>();
         var nontakingMoves = new List<Move>();

         for (byte row = 0; row < numberOfRows; row++)
         {
            for (byte column = 0; column < numberOfColumns; column++)
            {
               var pos = new Position(column, row);

               var type = state.GetPieceType(pos);
               if (type == PieceType.None)
               {
                  continue;
               }

               var color = GetColor(type);
               if (color != state.OnMove)
               {
                  continue;
               }

               var rank = GetRank(type);
               // --- MAN ---
               if (rank == PieceRank.Man)
               {
                  if (takingMovesWithKings.Count > 0)
                  {
                     continue;
                  }

                  // Taking moves
                  var path = new List<Position>();
                  var takenPieces = new List<Position>();
                  path.Add(pos);

                  SearchForTakingMovesWithMan(state, color, path, takenPieces, takingMovesWithMen);

                  // little optimization, cause taking is mandatory, hence further searching for non-taking moves might not be necessary
                  if (takingMovesWithMen.Count > 0)
                  {
                     continue;
                  }

                  // Non taking moves
                  int dRow = color == PieceColor.White ? -1 : 1;
                  int dColumn0 = -1, dColumn1 = 1;

                  Position p0 = pos + (dRow, dColumn0);
                  Position p1 = pos + (dRow, dColumn1);

                  if (state.IsInsideBoard(p0) && state.GetPieceType(p0) == PieceType.None)
                  {
                     var m = new Move() { path = new Position[] { pos, p0 } };
                     if (p0.row == 0 || p0.row == numberOfRows - 1)
                     {
                        m.promotion = 1;
                     }
                     nontakingMoves.Add(m);
                  }
                  if (state.IsInsideBoard(p1) && state.GetPieceType(p1) == PieceType.None)
                  {
                     var m = new Move() { path = new Position[] { pos, p1 } };
                     if (p1.row == 0 || p1.row == numberOfRows - 1)
                     {
                        m.promotion = 1;
                     }
                     nontakingMoves.Add(m);
                  }

                  // --- KING ---
               }
               else if (rank == PieceRank.King)
               {
                  // Taking moves
                  var path = new List<Position>();
                  var takenPieces = new List<Position>();
                  path.Add(pos);

                  SearchForTakingMovesWithKing(state, color, path, takenPieces, takingMovesWithKings);


                  if (takingMovesWithMen.Count > 0 || takingMovesWithKings.Count > 0)
                  {
                     continue;
                  }

                  // Non taking moves
                  int dRow0 = -1, dRow1 = 1;
                  int dColumn0 = -1, dColumn1 = 1;

                  Position p00 = pos + (dRow0, dColumn0);
                  Position p01 = pos + (dRow0, dColumn1);
                  Position p10 = pos + (dRow1, dColumn0);
                  Position p11 = pos + (dRow1, dColumn1);

                  while (state.IsInsideBoard(p00) && state.GetPieceType(p00) == PieceType.None)
                  {
                     nontakingMoves.Add(new Move() { path = new Position[] { pos, p00 } });
                     p00 += (dRow0, dColumn0);
                  }
                  while (state.IsInsideBoard(p01) && state.GetPieceType(p01) == PieceType.None)
                  {
                     nontakingMoves.Add(new Move() { path = new Position[] { pos, p01 } });
                     p01 += (dRow0, dColumn1);
                  }
                  while (state.IsInsideBoard(p10) && state.GetPieceType(p10) == PieceType.None)
                  {
                     nontakingMoves.Add(new Move() { path = new Position[] { pos, p10 } });
                     p10 += (dRow1, dColumn0);
                  }
                  while (state.IsInsideBoard(p11) && state.GetPieceType(p11) == PieceType.None)
                  {
                     nontakingMoves.Add(new Move() { path = new Position[] { pos, p11 } });
                     p11 += (dRow1, dColumn1);
                  }

               }
               else
               {
                  throw new InvalidOperationException("Invalid rank");
               }
            }
         }

         return
            takingMovesWithKings.Count > 0 ? takingMovesWithKings :  // taking with a king has priority
            takingMovesWithMen.Count > 0 ? takingMovesWithMen :      // taking is mandatory
            nontakingMoves.Count > 0 ? nontakingMoves :              // any move
            null;                                                    // no move avaiable
      }
      
        /// <summary>
      /// 
      /// </summary>
      /// <param name="path"></param>
      /// <param name="moves">All found moves are added to this list</param>
      private void SearchForTakingMovesWithMan(BoardState state, PieceColor color, List<Position> path, List<Position> takenPieces, List<Move> moves)
      {
         var pos = path.Last();
         int dRow = color == PieceColor.White ? -1 : 1;

         bool canJump = false;
         foreach (var dColumn in deltas)
         {
            var delta = (dRow, dColumn);

            var posOfTakenPiece = pos + delta; // position of a potential piece to be taken
            var posLanding = posOfTakenPiece + delta; // landing position

            if (state.IsInsideBoard(posLanding)
               && state.GetPieceType(posLanding) == PieceType.None
               && !takenPieces.Contains(posOfTakenPiece))
            {

               var pt = state.GetPieceType(posOfTakenPiece);
               if (pt != PieceType.None && GetColor(pt) == SwapColor(color))
               {
                  canJump = true;

                  takenPieces.Add(posOfTakenPiece);
                  path.Add(posLanding);

                  SearchForTakingMovesWithMan(state, color, path, takenPieces, moves);

                  takenPieces.RemoveAt(takenPieces.Count - 1);
                  path.RemoveAt(path.Count - 1);
               }
            }
         }

         if (!canJump && path.Count > 1)
         {
            moves.Add(new Move()
            {
               path = path.ToArray(),
               positionsOfTakenPieces = takenPieces.ToArray(),
               promotion = pos.row == 0 || pos.row == numberOfRows - 1 ? path.Count() - 1 : -1,
            });
         }
      }
      
        private bool SearchForTakingMovesWithKing(BoardState state, PieceColor color, List<Position> path, List<Position> takenPieces, List<Move> moves)
      {
         var pos = path.Last();
         var potentionalLandingPlaces = new List<Position>();

         bool canJump = false;
         foreach (var dRow in deltas)
         {
            foreach (var dColumn in deltas)
            {
               var delta = (dRow, dColumn);

               var pos0 = pos + delta;
               bool isInside;
               PieceType pt = PieceType.None;

               while ((isInside = state.IsInsideBoard(pos0)) && (pt = state.GetPieceType(pos0)) == PieceType.None)
               {
                  pos0 += delta;
               }

               if (isInside
                  && GetColor(pt) == SwapColor(color)
                  && !takenPieces.Contains(pos0))
               {
                  var posPieceToBeTaken = pos0;
                  bool canContinueJumping = false;
                  pos0 += delta;
                  takenPieces.Add(posPieceToBeTaken);

                  while (state.IsInsideBoard(pos0) && state.GetPieceType(pos0) == PieceType.None)
                  {
                     canJump = true;
                     potentionalLandingPlaces.Add(pos0);

                     path.Add(pos0);

                     canContinueJumping |= SearchForTakingMovesWithKing(state, color, path, takenPieces, moves);

                     path.RemoveAt(path.Count - 1);

                     pos0 += delta;
                  }

                  if (!canContinueJumping && potentionalLandingPlaces.Count > 0)
                  {
                     foreach (var p in potentionalLandingPlaces)
                     {
                        path.Add(p);
                        moves.Add(new Move()
                        {
                           path = path.ToArray(),
                           positionsOfTakenPieces = takenPieces.ToArray(),
                        });
                        path.RemoveAt(path.Count - 1);
                     }
                  }

                  takenPieces.RemoveAt(takenPieces.Count - 1);
                  potentionalLandingPlaces.Clear();
               }
            }
         }
         return canJump;
      }

      private readonly int[] deltas = new int[2] { -1, 1 };

      public override BoardState GetInitialBoardState() => new BoardState(this, initialOccupiedPlaces, initialPieceRanks, initialPieceColors, GetStartingColor());

      // Initial board state acording to Czech rules
      public const ulong
         initialOccupiedPlaces = 0b_1111_1111_1111_0000_0000_1111_1111_1111,
         initialPieceRanks     = 0b_0000_0000_0000_0000_0000_0000_0000_0000,
         initialPieceColors    = 0b_0000_0000_0000_0000_0000_1111_1111_1111;
    }
}