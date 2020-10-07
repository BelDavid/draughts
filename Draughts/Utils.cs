using Draughts.BoardEvaluators;
using Draughts.Pieces;
using Draughts.Rules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Draughts
{
    public static class Utils
    {
        private static readonly Random randOverlord = new Random();
        private static readonly ThreadLocal<Random> _rand = new ThreadLocal<Random>(() => { lock (randOverlord) return new Random(randOverlord.Next()); });
        public static Random rand => _rand.Value;

        public static readonly IFormatter binaryFormatter = new BinaryFormatter();



        public const string replayFileExt = "drep";
        public const string neuralNetworkFileExt = "nn";

        public static bool AnyPiece(PieceType pieceType) => (((int)pieceType >> 2) & 0b_1) == 0b_1;
        public static PieceColor GetColor(PieceType pieceType) => pieceType != PieceType.None ? (PieceColor)((int)pieceType & 0b_101) : PieceColor.None;
        public static PieceRank GetRank(PieceType pieceType) => pieceType != PieceType.None ? (PieceRank)((int)pieceType & 0b_110) : PieceRank.None;
        public static PieceColor SwapColor(PieceColor color) => color == PieceColor.None ? PieceColor.None : (PieceColor)(0b_100 | (~(uint)color & 0b_001));
        public static PieceType PromoteToKing(PieceType pieceType) => pieceType != PieceType.None ? (PieceType)((int)pieceType | (int)PieceRank.King) : throw new ArgumentException("can not promote PieceType.None");



        public static NeuralNetwork LoadNetwork(string path)
        {
            if (!File.Exists(path) || !path.EndsWith($".{neuralNetworkFileExt}"))
            {
                return null;
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    return (NeuralNetwork)binaryFormatter.Deserialize(fs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading from {path}\n{ex.Message}");
                return null;
            }
        }

    }
}