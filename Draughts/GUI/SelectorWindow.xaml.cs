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
        public SelectorWindow(SelectorWindowType windowType, Window owner = null)
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
                case SelectorWindowType.TwoUsers:
                    Title = "2 users";
                    break;

                case SelectorWindowType.AgainstBot:
                    Title = "Against bot";

                    label_difficulty.Visibility = Visibility.Visible;
                    combobox_difficulty.ItemsSource = Enum.GetValues(typeof(BotDifficulty)).Cast<BotDifficulty>();
                    combobox_difficulty.SelectedIndex = 0;
                    combobox_difficulty.Visibility = Visibility.Visible;

                    label_userColor.Visibility = Visibility.Visible;
                    combobox_userColor.Visibility = Visibility.Visible;
                    break;

                case SelectorWindowType.OverNetwork:
                    Title = "Over Network";

                    label_userColor.Visibility = Visibility.Visible;
                    combobox_userColor.Visibility = Visibility.Visible;

                    label_networkType.Visibility = Visibility.Visible;
                    combobox_networkType.Visibility = Visibility.Visible;
                    combobox_networkType.ItemsSource = Enum.GetValues(typeof(NetworkType)).Cast<NetworkType>();
                    combobox_networkType.SelectedIndex = 0;

                    label_serverIP.Visibility = Visibility.Visible;
                    textbox_serverIP.Visibility = Visibility.Visible;

                    Grid.SetRow(button_ok, 7);
                    break;

                case SelectorWindowType.Replay:
                    Title = "Replay";

                    label_rules.Visibility = Visibility.Hidden;
                    combobox_rules.Visibility = Visibility.Hidden;

                    label_filePath.Visibility = Visibility.Visible;
                    textbox_filePath.Visibility = Visibility.Visible;
                    button_selectFilePath.Visibility = Visibility.Visible;

                    label_animationSpeed.Visibility = Visibility.Visible;
                    combobox_animationSpeed.Visibility = Visibility.Visible;
                    combobox_animationSpeed.ItemsSource = Enum.GetValues(typeof(AnimationSpeed)).Cast<AnimationSpeed>();
                    combobox_animationSpeed.SelectedIndex = 0;
                    break;

                default:
                    throw new NotImplementedException();
            }

            // Pattern that matches valid ipv4 address
            regexIP = new Regex("^(?:(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])(\\.(?!$)|$)){4}$");
        }


        private Regex regexIP;
        private SelectorWindowType windowType;

        public bool Sucess { get; private set; }

        public RulesType rules { get; private set; }
        public BotDifficulty botDifficulty { get; private set; }
        public PieceColor color { get; private set; }
        public NetworkType networkType { get; private set; }
        public string serverIPaddress { get; private set; }
        public string filePath { get; private set; }
        public AnimationSpeed animationSpeed { get; private set; }


        private bool ValidateFilePath(string filePath)
        {
            return File.Exists(filePath) && filePath.EndsWith($".{Utils.replayFileExt}");
        }

        private void Button_ok_Click(object sender, RoutedEventArgs e)
        {
            rules = (RulesType)combobox_rules.SelectedItem;

            switch (windowType)
            {
                case SelectorWindowType.TwoUsers:
                    break;

                case SelectorWindowType.AgainstBot:
                    color = combobox_userColor.SelectedIndex == 0 ? PieceColor.White : PieceColor.Black;
                    botDifficulty = (BotDifficulty)combobox_difficulty.SelectedItem;
                    break;

                case SelectorWindowType.OverNetwork:
                    networkType = (NetworkType)combobox_networkType.SelectedItem;
                    if (networkType == NetworkType.Client && !regexIP.IsMatch(textbox_serverIP.Text))
                    {
                        MessageBox.Show("Invalid ipv4 address format");
                        return;
                    }
                    color = combobox_userColor.SelectedIndex == 0 ? PieceColor.White : PieceColor.Black;
                    serverIPaddress = textbox_serverIP.Text;
                    break;

                case SelectorWindowType.Replay:
                    filePath = textbox_filePath.Text;
                    if (!ValidateFilePath(textbox_filePath.Text))
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
            var brush = ValidateFilePath(textbox_filePath.Text)
                ? Brushes.Green
                : Brushes.Red;

            textbox_filePath.BorderBrush = brush;
            textbox_filePath.Foreground = brush;
        }

        private void Button_selectFilePath_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFileDialog() {
                Multiselect = false,
                CheckFileExists = true,
                DefaultExt = $".{Utils.replayFileExt}",
                Filter = $"*.{Utils.replayFileExt} files|*.{Utils.replayFileExt}",
            };

            if (f.ShowDialog() ?? false)
            {
                textbox_filePath.Text = f.FileName;
            }
        }
    }
}
