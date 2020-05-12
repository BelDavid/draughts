using Draughts.Rules;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.BoardEvaluators
{
    [Serializable]
    public class NeuralNetwork
    {
        public readonly int[] neuronLayout;
        public readonly double[][,] weights;
        public readonly Func<double, double> activationFunction;
        public readonly RulesType rulesType;

        public NeuralNetwork(int[] neuronLayout, RulesType rulesType, Func<double, double> activationFunction)
        {
            this.rulesType = rulesType;
            this.neuronLayout = neuronLayout ?? throw new ArgumentNullException("Argument neurons can not be null");
            this.activationFunction = activationFunction;

            if (neuronLayout.Length < 2)
            {
                throw new ArgumentException("Network has to have at least 2 layers");
            }

            weights = new double[neuronLayout.Length - 1][,];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = new double[neuronLayout[i] + 1, neuronLayout[i + 1]];
            }
        }
        public NeuralNetwork(double[][,] weights, RulesType rulesType, Func<double, double> activationFunction)
        {
            this.rulesType = rulesType;
            this.weights = weights ?? throw new ArgumentNullException("Argument weights can not be null");
            this.activationFunction = activationFunction;

            if (weights.Length < 1)
            {
                throw new ArgumentException("Network has to have at least 2 layers");
            }

            neuronLayout = new int[weights.Length + 1];
            neuronLayout[0] = weights[0].GetLength(0) - 1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] == null)
                {
                    throw new ArgumentException("No element can have null value");
                }
                else if (i != 0)
                {
                    if (weights[i].GetLength(0) != weights[i - 1].GetLength(1) + 1)
                    {
                        throw new ArgumentException($"Missmatch at layer {i}");
                    }
                }
                neuronLayout[i + 1] = weights[i].GetLength(1);
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
