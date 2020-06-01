using Draughts.BoardEvaluators;
using Draughts.Game;
using Draughts.Pieces;
using Draughts.Players;
using Draughts.Rules;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Draughts.GUI
{
    /// <summary>
    /// Interaction logic for SelectorWindow.xaml
    /// </summary>
    public partial class SelectorWindow : Window
    {
        public SelectorWindow(GameType windowType, Window owner = null)
        {
            if (owner != null)
            {
                Owner = owner;
            }

            InitializeComponent();

            combobox_rules.ItemsSource = Enum.GetValues(typeof(RulesType)).Cast<RulesType>();
            combobox_rules.SelectedIndex = 0;


            combobox_userColor.ItemsSource = Enum.GetValues(typeof(PieceColor)).Cast<PieceColor>().Where(color => color != PieceColor.None);
            combobox_userColor.SelectedIndex = 0;

            this.windowType = windowType;
            switch (windowType)
            {
                case GameType.Local:
                    Title = "Local";

                    Height = 200;
                    Grid.SetRow(button_ok, 4);
                    break;

                case GameType.AgainstBot:
                    Title = "Against bot";

                    label_difficulty.Visibility = Visibility.Visible;
                    combobox_difficulty.Visibility = Visibility.Visible;
                    combobox_difficulty.ItemsSource = Enum.GetValues(typeof(BotDifficulty)).Cast<BotDifficulty>();
                    combobox_difficulty.SelectedIndex = 0;
                    
                    label_evaluator.Visibility = Visibility.Visible;
                    combobox_evaluator.Visibility = Visibility.Visible;
                    combobox_evaluator.ItemsSource = Enum.GetValues(typeof(BoardEvaluatorType)).Cast<BoardEvaluatorType>();
                    combobox_evaluator.SelectedIndex = 0;

                    label_networkFilePath.Visibility = Visibility.Visible;
                    textbox_networkFilePath.Visibility = Visibility.Visible;
                    button_selectNetworkFilePath.Visibility = Visibility.Visible;

                    label_userColor.Visibility = Visibility.Visible;
                    combobox_userColor.Visibility = Visibility.Visible;

                    Height = 285;
                    Grid.SetRow(button_ok, 8);
                    break;

                case GameType.OverNetwork:
                    Title = "Over Network";

                    label_userColor.Visibility = Visibility.Visible;
                    combobox_userColor.Visibility = Visibility.Visible;

                    label_networkType.Visibility = Visibility.Visible;
                    combobox_networkType.Visibility = Visibility.Visible;
                    combobox_networkType.ItemsSource = Enum.GetValues(typeof(NetworkType)).Cast<NetworkType>();
                    combobox_networkType.SelectedIndex = 0;

                    label_serverIP.Visibility = Visibility.Visible;
                    textbox_serverIP.Visibility = Visibility.Visible;

                    Height = 260;
                    Grid.SetRow(button_ok, 7);
                    break;

                case GameType.Replay:
                    Title = "Replay";

                    label_rules.Visibility = Visibility.Hidden;
                    combobox_rules.Visibility = Visibility.Hidden;

                    label_replayFilePath.Visibility = Visibility.Visible;
                    textbox_replayFilePath.Visibility = Visibility.Visible;
                    button_selectReplayFilePath.Visibility = Visibility.Visible;

                    label_animationSpeed.Visibility = Visibility.Visible;
                    combobox_animationSpeed.Visibility = Visibility.Visible;
                    combobox_animationSpeed.ItemsSource = Enum.GetValues(typeof(AnimationSpeed)).Cast<AnimationSpeed>();
                    combobox_animationSpeed.SelectedIndex = 0;

                    Height = 230;
                    Grid.SetRow(button_ok, 5);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // Pattern that matches valid ipv4 address
            regexIP = new Regex("^(?:(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])(\\.(?!$)|$)){4}$");
        }


        private Regex regexIP;
        private GameType windowType;

        public bool Sucess { get; private set; }

        public RulesType rules { get; private set; }
        public BotDifficulty botDifficulty { get; private set; }
        public BoardEvaluatorType boardEvaluator { get; private set; }
        public PieceColor color { get; private set; }
        public NetworkType networkType { get; private set; }
        public string serverIPaddress { get; private set; }
        public string replayFilePath { get; private set; }
        public string neuralNetworkFilePath { get; private set; }
        public AnimationSpeed animationSpeed { get; private set; }


        private bool IsEvaluatorNeeded => combobox_difficulty.SelectedItem != null ? (BotDifficulty)(combobox_difficulty.SelectedItem) != BotDifficulty.Randomized : false;
        private bool IsFilePathSelectionNeeded => combobox_evaluator.SelectedItem != null
            ? ((BoardEvaluatorType)combobox_evaluator.SelectedItem) == BoardEvaluatorType.NeuralNetwork
               || ((BoardEvaluatorType)combobox_evaluator.SelectedItem) == BoardEvaluatorType.RLModel
            : false;


        private void Button_ok_Click(object sender, RoutedEventArgs e)
        {
            rules = (RulesType)combobox_rules.SelectedItem;

            switch (windowType)
            {
                case GameType.Local:
                    break;

                case GameType.AgainstBot:
                    color = combobox_userColor.SelectedIndex == 0 ? PieceColor.White : PieceColor.Black;
                    botDifficulty = (BotDifficulty)combobox_difficulty.SelectedItem;
                    boardEvaluator = (BoardEvaluatorType)combobox_evaluator.SelectedItem;
                    if (IsFilePathSelectionNeeded)
                    {
                        if (!File.Exists(textbox_networkFilePath.Text))
                        {
                            MessageBox.Show("Invalid file path");
                            return;
                        }
                        neuralNetworkFilePath = textbox_networkFilePath.Text;
                    }
                    break;

                case GameType.OverNetwork:
                    networkType = (NetworkType)combobox_networkType.SelectedItem;
                    if (networkType == NetworkType.Client && !regexIP.IsMatch(textbox_serverIP.Text))
                    {
                        MessageBox.Show("Invalid ipv4 address format");
                        return;
                    }
                    color = combobox_userColor.SelectedIndex == 0 ? PieceColor.White : PieceColor.Black;
                    serverIPaddress = textbox_serverIP.Text;
                    break;

                case GameType.Replay:
                    replayFilePath = textbox_replayFilePath.Text;
                    if (!File.Exists(textbox_replayFilePath.Text))
                    {
                        MessageBox.Show("Invalid file path");
                        return;
                    }
                    animationSpeed = (AnimationSpeed)combobox_animationSpeed.SelectedItem;
                    break;

                default:
                    throw new NotImplementedException();
            }

            Sucess = true;
            Close();
        }

        private void Textbox_serverIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            var brush = regexIP?.IsMatch(textbox_serverIP.Text) ?? true
                ? Brushes.Green
                : Brushes.Red;

            textbox_serverIP.BorderBrush = brush;
            textbox_serverIP.Foreground = brush;
        }

        private void Combobox_networkType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textbox_serverIP.IsEnabled = (NetworkType)combobox_networkType.SelectedItem == NetworkType.Client;
        }

        private void Textbox_filePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            var brush = File.Exists(textbox_replayFilePath.Text)
                ? Brushes.Green
                : Brushes.Red;

            textbox_replayFilePath.BorderBrush = brush;
            textbox_replayFilePath.Foreground = brush;
        }

        private void Button_selectReplayFilePath_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFileDialog() {
                Multiselect = false,
                CheckFileExists = true,
                DefaultExt = $".{Utils.replayFileExt}",
                Filter = $"*.{Utils.replayFileExt} files|*.{Utils.replayFileExt}",
            };

            if (f.ShowDialog() ?? false)
            {
                textbox_replayFilePath.Text = f.FileName;
            }
        }
        private void Button_selectNetworkFilePath_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFileDialog() {
                Multiselect = false,
                CheckFileExists = true,
                DefaultExt = $".{Utils.neuralNetworkFileExt}",
                Filter = $"*.{Utils.neuralNetworkFileExt} files|*.{Utils.neuralNetworkFileExt}",
            };

            if (f.ShowDialog() ?? false)
            {
                textbox_networkFilePath.Text = f.FileName;
            }
        }

        private void Combobox_evaluator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isFilePathSelectionNeeded = IsFilePathSelectionNeeded;

            label_networkFilePath.IsEnabled = isFilePathSelectionNeeded;
            textbox_networkFilePath.IsEnabled = isFilePathSelectionNeeded;
            button_selectNetworkFilePath.IsEnabled = isFilePathSelectionNeeded;
        }

        private void Combobox_difficulty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isEvaluatorNeeded = IsEvaluatorNeeded;
            bool isFilePathSelectionNeeded = IsFilePathSelectionNeeded;

            label_evaluator.IsEnabled = isEvaluatorNeeded;
            combobox_evaluator.IsEnabled = isEvaluatorNeeded;

            label_networkFilePath.IsEnabled = isEvaluatorNeeded && isFilePathSelectionNeeded;
            textbox_networkFilePath.IsEnabled = isEvaluatorNeeded && isFilePathSelectionNeeded;
            button_selectNetworkFilePath.IsEnabled = isEvaluatorNeeded && isFilePathSelectionNeeded;
        }
    }
}
