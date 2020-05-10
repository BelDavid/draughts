using Draughts.BoardEvaluators;
using Draughts.Players;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Draughts
{
    public class EvolutionaryAlgorithm
    {
        public static readonly string folderPath_runs = $"{Utils.localFolderLocation}/runs";

        static EvolutionaryAlgorithm()
        {
            if (!Directory.Exists(folderPath_runs))
            {
                Directory.CreateDirectory(folderPath_runs);
            }
        }


        public double mutationRate = 1d;
        public double mutationBitRate = 0.01d;
        public double crossoverRate = 0.2d;
        public int populationSize = 30;
        public int numberOfElites = 0;
        public int numberOfGameRounds = 50;

        public int minimaxDepth = 3;
        public int numberOfCompetetiveMatches = 50;

        private readonly string id;
        private readonly Func<NeuralNetwork> networkFactory;
        private readonly RulesType rulesType;

        public EvolutionaryAlgorithm(string id, Func<NeuralNetwork> networkFactory, RulesType rulesType)
        {
            this.id = id;
            this.networkFactory = networkFactory;
            this.rulesType = rulesType;
        }


        public List<NNFit> Run(int numberOfGenerations)
        {
            var startingPopulation = new List<NeuralNetwork>(populationSize + numberOfElites);
            for (int i = 0; i < populationSize; i++)
            {
                startingPopulation.Add(networkFactory());
            }
            var generation = CalculateFitnesses(startingPopulation);
            Report(generation, 0);

            for (int i = 1; i < numberOfGenerations; i++)
            {
                generation = GetNextGen(generation);

                Report(generation, i);
            }

            return generation;
        }

        public List<NNFit> GetNextGen(List<NNFit> currGen)
        {
            // SELECT
            var matingPool = SelectRulete(currGen);

            // CROSSOVER
            for (int j = 0; j + 1 < matingPool.Count; j += 2)
                if (Utils.rand.NextDouble() < crossoverRate)
                    (matingPool[j], matingPool[j + 1]) = CrossOver(matingPool[j], matingPool[j + 1]);

            // MUTATION
            for (int j = 0; j < matingPool.Count; j++)
                if (Utils.rand.NextDouble() < mutationRate)
                    matingPool[j] = Mutate(matingPool[j]);

            // Calculate fitnesses
            var nextGen = CalculateFitnesses(matingPool);

            // ELITISM | Fittest entity survives to next generation
            var elites = Math.Min(numberOfElites, populationSize);
            for (int i = 0; i < elites; i++)
            {
                nextGen.Add(currGen[i]);
            }

            Sort(nextGen);

            nextGen.RemoveRange(currGen.Count, elites);

            return nextGen;
        }

        private List<NeuralNetwork> SelectRulete(List<NNFit> population)
        {
            var wheelSums = new double[population.Count];
            double sum = 0;
            for (int i = 0; i < population.Count; i++)
                wheelSums[i] = sum += population[i].fitness;

            var matingPool = new NeuralNetwork[population.Count];
            Parallel.For(0, population.Count, (i) => {
                double r = Utils.rand.NextDouble() * sum;
                int j = 0;
                while (wheelSums[j] < r)
                {
                    j++;
                }
                matingPool[i] = population[j].neuralNetwork;
            });

            return matingPool.ToList();
        }

        public (NeuralNetwork, NeuralNetwork) CrossOver(NeuralNetwork a0, NeuralNetwork b0)
        {
            var neuronLayout = a0.neuronLayout;
            var a1 = new NeuralNetwork(neuronLayout, a0.activationFunction);
            var b1 = new NeuralNetwork(neuronLayout, b0.activationFunction);

            for (int i = 0; i < neuronLayout.Length - 1; i++)
            {
                for (int k = 0; k < neuronLayout[i + 1]; k++)
                {
                    if (Utils.rand.Next(2) == 0)
                    {
                        for (int j = 0; j < neuronLayout[i] + 1; j++)
                        {
                            a1.weights[i][j, k] = a0.weights[i][j, k];
                            b1.weights[i][j, k] = b0.weights[i][j, k];
                        }
                    }
                    else
                    {
                        for (int j = 0; j < neuronLayout[i] + 1; j++)
                        {
                            a1.weights[i][j, k] = b0.weights[i][j, k];
                            b1.weights[i][j, k] = a0.weights[i][j, k];
                        }
                    }
                }
            }

            return (a1, b1);
        }

        public NeuralNetwork Mutate(NeuralNetwork a)
        {
            for (int i = 0; i < a.weights.Length; i++)
            {
                for (int j = 0; j < a.weights[i].GetLength(0); j++)
                {
                    for (int k = 0; k < a.weights[i].GetLength(1); k++)
                    {
                        if (Utils.rand.NextDouble() < mutationBitRate)
                            a.weights[i][j, k] += Utils.rand.NextGaussian();
                    }
                }
            }

            return a;
        }

        public List<NNFit> Sort(List<NNFit> population)
        {
            population.Sort((e0, e1) => e1.fitness.CompareTo(e0.fitness));
            return population;
        }

        public List<NNFit> CalculateFitnesses(List<NeuralNetwork> networks)
        {
            var nf = new NNFit[networks.Count];

            // Parallel.For(0, networks.Count, i =>
            for (int i = 0; i < networks.Count; i++)
            {
                (int wins, _, _) = MainWindow.Simulate(
                    $"{id}_sim{i}",
                    rulesType,
                    () => new MinimaxBot($"nn_bot{minimaxDepth}", minimaxDepth, new BoardEvaluatorNeuralNetwork(_ => networks[i]), null, true, true),
                    () => new MinimaxBot($"mx_bot{minimaxDepth}", minimaxDepth, new BoardEvaluatorBasic(), null, true, true),
                    numberOfCompetetiveMatches,
                    null,
                    false
                );

                nf[i] = new NNFit(networks[i], wins);
            }
            //);

            return nf.ToList();
        }

        private void Report(List<NNFit> generation, int genNum)
        {
            Debug.WriteLine($"[{id}] gen{genNum} | best: {generation.First().fitness}/{numberOfCompetetiveMatches}");
        }
    }

    public class NNFit
    {
        public NeuralNetwork neuralNetwork;
        public double fitness;

        public NNFit(NeuralNetwork neuralNetwork, double fitness)
        {
            this.neuralNetwork = neuralNetwork;
            this.fitness = fitness;
        }
    }
}
