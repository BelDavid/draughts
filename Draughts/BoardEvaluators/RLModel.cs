using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Draughts.Pieces;
using Numpy;

namespace Draughts.BoardEvaluators
{
    [Serializable]
    public class RLModel
    {
        private Keras.Models.Sequential _model;

        public RLModel()
        {
            _model = new Keras.Models.Sequential();
            _model.Add(new Keras.Layers.Dense(512, activation: "relu", input_shape: new Keras.Shape(new int[] { 32 })));
            _model.Add(new Keras.Layers.Dense(512, activation: "relu"));
            _model.Add(new Keras.Layers.Dense(1, activation: "linear"));

            _model.Compile(optimizer: new Keras.Optimizers.Adam(), loss: "binary_crossentropy", metrics: new string[] { "accuracy" });
        }

        public RLModel(string path)
        {
            _model = (Keras.Models.Sequential)Keras.Models.Sequential.LoadModel(path);
        }

        public void TrainOnBatch(NDarray x, NDarray y, NDarray weights)
        {
            _model.TrainOnBatch(x, y, weights);
        }

        public float Predict(NDarray<float> x)
        {
            NDarray x_reshaped = x.reshape(new int[] { 1, -1 });
            return _model.Predict(x_reshaped, verbose: 0).item<float>(0);
        }

        public void Save(string filepath)
        {
            _model.Save(filepath);
        }

        public static NDarray<float> ConvertBoardStateToModelInput(BoardState state)
        {
            List<float> boardState = new List<float>();
            foreach (var (pos, pieceType) in state.IterateBoard())
            {
                if ((pos.column + pos.row) % 2 == 0)
                {
                    continue;
                }

                float f = Utils.GetRank(pieceType) == PieceRank.Man ? 1f
                        : Utils.GetRank(pieceType) == PieceRank.King ? 2f
                        : 0f;

                boardState.Add(Utils.GetColor(pieceType) == PieceColor.White ? f
                     : Utils.GetColor(pieceType) == PieceColor.Black ? -f
                     : 0f);
            }

            return (NDarray<float>)boardState.ToArray();
        }
    }
}
