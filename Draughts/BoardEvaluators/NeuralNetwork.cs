using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    public class NeuralNetwork
    {
        public readonly int[] neuronLayout;
        public readonly double[][,] weights;
        public readonly Func<double, double> activationFunction;

        public NeuralNetwork(int[] neuronLayout, double[][,] weights, Func<double, double> activationFunction)
        {
            this.neuronLayout = neuronLayout ?? throw new ArgumentNullException("Argument neurons can not be null");
            this.weights = weights ?? throw new ArgumentNullException("Argument weights can not be null");
            this.activationFunction = activationFunction;

            if (neuronLayout.Length < 2)
            {
                throw new ArgumentException("Network has to have at least 2 layers");
            }

            if (weights.Length != neuronLayout.Length - 1)
            {
                throw new ArgumentException();
            }
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] is null)
                {
                    throw new ArgumentException($"No element of argument weights can be null");
                }
                if (weights[i].GetLength(0) != neuronLayout[i] + 1 || weights[i].GetLength(1) != neuronLayout[i + 1])
                {
                    throw new ArgumentException($"Weights on {i}-th layer do not match layout given by neurons");
                }
            }
        }

        public double[] Evaluate(double[] input)
        {
            if (input.Length != neuronLayout.First())
            {
                throw new ArgumentException("Input must match first layer of the network");
            }

            var layoutIn = input;

            // Foreach layer
            for (int i = 0; i < neuronLayout.Length - 1; i++)
            {
                var layoutOut = new double[neuronLayout[i + 1]];

                // Foreach neuron
                for (int j = 0; j < neuronLayout[i + 1]; j++)
                {
                    double val = 0;

                    // Foreach weight to previous neuron
                    for (int k = 0; k < neuronLayout[i]; k++)
                    {
                        val += weights[i][k, j] * layoutIn[k];
                    }
                    val += weights[i][neuronLayout[i], j]; // Treshold

                    layoutOut[j] = activationFunction(val);
                }

                layoutIn = layoutOut;
            }

            return layoutIn;
        }
    }
}
