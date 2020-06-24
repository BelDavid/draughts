using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class BoardEvaluatorNeuralNetwork : IBoardEvaluator
    {
        private NeuralNetwork neuralNetwork;

        public BoardEvaluatorNeuralNetwork(NeuralNetwork neuralNetwork)
        {
            this.neuralNetwork = neuralNetwork ?? throw new ArgumentNullException("Argument neuralNetwork can not be null");

            if (neuralNetwork.neuronLayout.Last() != 1)
            {
                throw new ArgumentException("Last layer of the network must have exactly 1 neuron");
            }
        }


        public double Evaluate(BoardState state)
        {
            var input = new double[state.NumberOfColumns * state.NumberOfRows / 2];

            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var val = Utils.GetRank(pieceType) == PieceRank.Man ? 1d
                          : Utils.GetRank(pieceType) == PieceRank.King ? 5d
                          : 0d;


                    input[(pos.column + pos.row * state.NumberOfColumns) / 2] = 
                        Utils.GetColor(pieceType) == PieceColor.White ? val
                         : Utils.GetColor(pieceType) == PieceColor.Black ? -val
                         : 0d;
                }
            }
            return neuralNetwork.Evaluate(input)[0];
        }

        public void Validate(GameRules rules)
        {
            if (rules.rulesType != neuralNetwork.rulesType)
            {
                throw new Exception("Network is designed for different rules");
            }
            if (rules.numberOfColumns * rules.numberOfRows / 2 != neuralNetwork.neuronLayout[0])
            {
                throw new Exception("Missmatch between number of neurons on a first layer and number of valid places on the board");
            }
        }
    }
}
