using Draughts.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Rules
{
    public abstract class GameRules
    {
        public readonly byte numberOfRows, numberOfColumns;
        public readonly RulesType rulesType;

        public GameRules(RulesType rulesType)
        {
            (numberOfColumns, numberOfRows) = GetBoardDimensions(rulesType);

            if (numberOfColumns * numberOfRows > 128)
            {
                throw new ArgumentException("Number of places on the board can not exceed 128");
            }
            if (numberOfRows % 2 != 0 || numberOfColumns % 2 != 0)
            {
                throw new ArgumentException("Number of rows and columns must be even");
            }

            this.rulesType = rulesType;
        }

        public abstract List<Move> GetAvaiableMoves(BoardState state);
        public abstract BoardState GetInitialBoardState();
        public abstract Pieces.PieceColor GetStartingColor();

        public static (byte numberOfColumns, byte numberOfRows) GetBoardDimensions(RulesType rules)
        {
            switch (rules)
            {
                case RulesType.Czech:
                    return (8, 8);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}