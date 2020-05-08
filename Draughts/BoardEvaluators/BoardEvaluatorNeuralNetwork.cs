using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    class BoardEvaluatorNeuralNetwork : IBoardEvaluator
    {
        private readonly Func<RulesType, NeuralNetwork> neuralNetworkFactory;
        private NeuralNetwork neuralNetwork;

        public BoardEvaluatorNeuralNetwork(Func<RulesType, NeuralNetwork> neuralNetworkFactory)
        {
            this.neuralNetworkFactory = neuralNetworkFactory ?? throw new ArgumentNullException("Argument neuralNetworkFactory can not be null");
        }


        public double Evaluate(BoardState state)
        {
            var input = new double[state.NumberOfColumns * state.NumberOfRows];

            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if (pieceType != PieceType.None)
                {
                    var val = Utils.GetRank(pieceType) == PieceRank.Man ? 1d
                          : Utils.GetRank(pieceType) == PieceRank.King ? 5d
                          : 0d;


                    input[pos.column + pos.row * state.NumberOfColumns]
                         = Utils.GetColor(pieceType) == PieceColor.White ? val
                         : Utils.GetColor(pieceType) == PieceColor.Black ? -val
                         : 0d;
                }
            }
            return neuralNetwork.Evaluate(input)[0];
        }

        public void Setup(GameRules rules)
        {
            neuralNetwork = neuralNetworkFactory(rules.rulesType) ?? throw new Exception("neuralNetwork can not be null");

            if (neuralNetwork.neuronLayout.Last() != 1)
            {
                throw new ArgumentException("Last layer of the network must have exactly 1 neuron");
            }
            if (rules.numberOfColumns * rules.numberOfRows != neuralNetwork.neuronLayout[0])
            {
                throw new Exception("Missmatch between number of neurons on a first layer and number of places on the board");
            }
        }
    }
}
