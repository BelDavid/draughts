using Draughts.Rules;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Draughts.BoardEvaluators
{
    [Serializable]
    public class NeuralNetwork
    {
        public readonly int[] neuronLayout;
        public readonly double[][,] weights;
        public readonly ActivationFunctionType[] activationFunctionTypes;
        public readonly ActivationFunctionDelegate[] activationFunctions;
        public readonly RulesType rulesType;

        public NeuralNetwork(int[] neuronLayout, RulesType rulesType, ActivationFunctionType[] activationFunctionTypes)
        {
            this.rulesType = rulesType;
            this.neuronLayout = neuronLayout ?? throw new ArgumentNullException("Argument neurons can not be null");

            this.activationFunctionTypes = activationFunctionTypes ?? throw new ArgumentNullException("Argument activationFunctionTypes can not be null");
            if (activationFunctionTypes.Length != neuronLayout.Length - 1)
            {
                throw new ArgumentException(""); // TODO
            }
            activationFunctions = new ActivationFunctionDelegate[activationFunctionTypes.Length];
            for (int i = 0; i < activationFunctionTypes.Length; i++)
            {
                switch (activationFunctionTypes[i])
                {
                    case ActivationFunctionType.Sigmoid:
                        activationFunctions[i] = Sigmoid;
                        break;
                    case ActivationFunctionType.Tanh:
                        activationFunctions[i] = Tanh;
                        break;
                    case ActivationFunctionType.ReLu:
                        activationFunctions[i] = ReLu;
                        break;
                    case ActivationFunctionType.Linear:
                        activationFunctions[i] = Linear;
                        break;
                    default:
                        throw new NotImplementedException("Invalid Activation function");
                }
            }

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
        public NeuralNetwork(double[][,] weights, RulesType rulesType, ActivationFunctionType[] activationFunctionTypes)
        {
            this.rulesType = rulesType;
            this.weights = weights ?? throw new ArgumentNullException("Argument weights can not be null");
            
            this.activationFunctionTypes = activationFunctionTypes ?? throw new ArgumentNullException("Argument activationFunctionTypes can not be null");
            activationFunctions = new ActivationFunctionDelegate[activationFunctionTypes.Length];
            for (int i = 0; i < activationFunctionTypes.Length; i++)
            {
                switch (activationFunctionTypes[i])
                {
                    case ActivationFunctionType.Sigmoid:
                        activationFunctions[i] = Sigmoid;
                        break;
                    case ActivationFunctionType.Tanh:
                        activationFunctions[i] = Tanh;
                        break;
                    case ActivationFunctionType.ReLu:
                        activationFunctions[i] = ReLu;
                        break;
                    case ActivationFunctionType.Linear:
                        activationFunctions[i] = Linear;
                        break;
                    default:
                        throw new NotImplementedException("Invalid Activation function");
                }
            }

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
        private NeuralNetwork(int[] neuronLayout, double[][,] weights, RulesType rulesType, ActivationFunctionType[] activationFunctionTypes, ActivationFunctionDelegate[] activationFunctions)
        {
            this.neuronLayout = neuronLayout;
            this.weights = weights;
            this.rulesType = rulesType;
            this.activationFunctionTypes = activationFunctionTypes;
            this.activationFunctions = activationFunctions;
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

                    layoutOut[j] = activationFunctions[i](val);
                }

                layoutIn = layoutOut;
            }

            return layoutIn;
        }

        public NeuralNetwork Clone()
        {
            var nl = new int[neuronLayout.Length];
            neuronLayout.CopyTo(nl, 0);

            var w = new double[weights.Length][,];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = weights[i].Clone() as double[,];
            }

            var aft = new ActivationFunctionType[activationFunctionTypes.Length];
            activationFunctionTypes.CopyTo(aft, 0);

            var af = new ActivationFunctionDelegate[activationFunctions.Length];
            activationFunctions.CopyTo(af, 0);

            var rt = rulesType;

            return new NeuralNetwork(nl, w, rt, aft, af);
        }


        public delegate double ActivationFunctionDelegate(double val);
        public static double Sigmoid(double val) => 1.0d / (1.0d + Math.Exp(-val));
        public static double Tanh(double val) => throw new NotImplementedException();
        public static double ReLu(double val) => throw new NotImplementedException();
        public static double Linear(double val) => val;
    }

    [Serializable]
    public enum ActivationFunctionType
    {
        Sigmoid,
        Tanh,
        ReLu,
        Linear,
    }
}
