﻿using Draughts.Pieces;
using Draughts.Rules;
using System;
using Draughts.Visualisation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Players
{
   public class User : Player
   {
      public User() : base()
      {
      }
      public Visualiser visualiser { private get; set; }


      public override Move MakeMove(BoardState boardState)
      {
         var moves = boardState.GetAvaiableMoves(Color);

         if (visualiser is null)
         {
            throw new InvalidOperationException("User needs visualiser to be able to make a move");
         }

         return visualiser.LetUserDoTheMove(moves);
      }
   }
}