using Draughts.Rules;
using Draughts.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class BoardEvaluatorRL : IBoardEvaluator
    {
        private RLModel _model;

        public BoardEvaluatorRL(RLModel model)
        {
            this._model = model;
        }

        public double Evaluate(BoardState state)
        {
            var model_input = RLModel.ConvertBoardStateToModelInput(state);
            return _model.Predict(model_input);
        }

        public void Validate(GameRules gameRules)
        {
            //throw new NotImplementedException();
        }
    }
}
