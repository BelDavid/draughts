using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Draughts;
using Draughts.BoardEvaluators;
using Draughts.Players;
using Draughts.Rules;

using Keras;
using Numpy;

namespace Controller
{
    public class ReinforcementLearning
    {
        public RLModel TrainNewModel(int numberOfBatches, int gamesPerBatch, int depth)
        {
            RLModel model = new RLModel();
            BoardEvaluatorRL evaluator = new BoardEvaluatorRL(model);

            for (int i = 0; i < numberOfBatches; i++)
            {
                SimulationOutput output = Program.SimulateSerial(
                    "test",
                    RulesType.Czech,
                    () => {
                        MinimaxBot bot = new MinimaxBot("train", depth, evaluator, null);
                        bot.ruletteMoveSelection = true;
                        return bot;
                    },
                    () => {
                        MinimaxBot bot = new MinimaxBot("train2", depth, evaluator, null);
                        bot.ruletteMoveSelection = true;
                        return bot;
                    },
                    gamesPerBatch
                ) ;

                List<Numpy.NDarray> x = new List<Numpy.NDarray>();
                List<float> y = new List<float>();
                List<float> weights = new List<float>();

                foreach(List<BoardState> game in output.histories)
                {
                    int positiveOffset = game.Count % 2;
                    int curIndex = 0;
                    foreach(BoardState state in game)
                    {
                        x.Add(RLModel.ConvertBoardStateToModelInput(state));
                        
                        if (curIndex % 2 == positiveOffset)
                        {
                            y.Add(10f);
                        } else
                        {
                            y.Add(-10f);
                        }
                        weights.Add(game.Count - curIndex);

                        ++curIndex;
                    }
                }
                Numpy.NDarray npx = np.stack(x.ToArray());
                Numpy.NDarray npy = (Numpy.NDarray)y.ToArray();
                Numpy.NDarray npweights = (Numpy.NDarray)weights.ToArray();
                
                model.TrainOnBatch(npx, npy, npweights);
                Console.WriteLine($"Batch number {i} finished");
            }

            return model;
        }
    }

}
