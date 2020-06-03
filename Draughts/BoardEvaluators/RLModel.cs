using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Draughts.Pieces;
using Numpy;
using Keras.Models;
using Keras.Layers;

namespace Draughts.BoardEvaluators
{
    [Serializable]
    public class RLModel
    {
        private BaseModel _model;

        public RLModel()
        {
            var sModel = new Sequential();
            sModel.Add(new Dense(512, activation: "relu", input_shape: new Keras.Shape(new int[] { 32 })));
            sModel.Add(new Dense(512, activation: "relu"));
            sModel.Add(new Dense(1, activation: "linear"));

            _model = sModel;

            _model.Compile(optimizer: new Keras.Optimizers.Adam(), loss: "binary_crossentropy", metrics: new string[] { "accuracy" });
        }

        public RLModel(string path)
        {
            _model = BaseModel.LoadModel(path);
        }

        public void TrainOnBatch(NDarray x, NDarray y, NDarray weights)
        {
            _model.TrainOnBatch(x, y, weights);
        }

        public float Predict(NDarray x)
        {
            NDarray x_reshaped = x.reshape(new int[] { 1, -1 });
            return _model.Predict(x_reshaped, verbose: 0).item<float>(0);
        }

        public void Save(string path)
        {
            _model.Save(path);
        }

        public static NDarray ConvertBoardStateToModelInput(BoardState state)
        {
            NDarray boardState = np.zeros(new int[] { 32 });
            int i = 0;
            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if ((pos.column + pos.row) % 2 == 0)
                {
                    continue;
                }

                float f = Utils.GetRank(pieceType) == PieceRank.Man ? 1f
                        : Utils.GetRank(pieceType) == PieceRank.King ? 2f
                        : 0f;

                boardState.itemset(new object[] { i++, Utils.GetColor(pieceType) == PieceColor.White ? f
                     : Utils.GetColor(pieceType) == PieceColor.Black ? -f
                     : 0f });
            }

            return boardState;
        }
    }
}
