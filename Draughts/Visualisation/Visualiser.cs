using Draughts.Pieces;
using Draughts.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static Draughts.Utils;

namespace Draughts.Visualisation
{
    public class Visualiser : IDisposable
    {
        private readonly Canvas canvas;
        private readonly GameControl gameControl;
        private readonly PieceColor playersPerspective;
        private BoardState boardState => gameControl.CurrentBoardState;
        public Visualiser(Canvas canvas, GameControl gameControl)
        {
            this.canvas = canvas ?? throw new ArgumentNullException("canvas can not be null");
            this.canvas.SizeChanged += Canvas_SizeChanged;

            this.gameControl = gameControl ?? throw new ArgumentNullException("gameControl can not be null");

            var whiteUser = gameControl.WhitePlayer as User;
            var blackUser = gameControl.BlackPlayer as User;

            if (blackUser != null)
            {
                blackUser.visualiser = this;
            }
            if (whiteUser != null)
            {
                whiteUser.visualiser = this;
            }

            if (whiteUser != null && blackUser != null)
            {
                playersPerspective = gameControl.gameRules.GetStartingColor();
            }
            else if (blackUser != null)
            {
                playersPerspective = PieceColor.Black;
            }
            else
            {
                playersPerspective = PieceColor.White;
            }

            piecesOnBoard = new PieceShape[NumberOfColumns, NumberOfRows];

            // Tiles
            rectTiles = new Rectangle[NumberOfColumns * NumberOfRows];
            for (byte row = 0; row < NumberOfRows; row++)
            {
                for (byte column = 0; column < NumberOfColumns; column++)
                {
                    int i = row * NumberOfColumns + column;
                    var tile = new Rectangle() { Fill = (column + row) % 2 == 0 ? Brushes.SandyBrown : Brushes.SaddleBrown, StrokeThickness = 0, };

                    rectTiles[i] = tile;
                    canvas.Children.Add(tile);
                }
            }

            // Tile Line Borders
            linesTileBorders = new System.Windows.Shapes.Line[NumberOfColumns + NumberOfRows - 2];
            for (int i = 0; i < linesTileBorders.Length; i++)
            {
                var line = new System.Windows.Shapes.Line() { Stroke = Brushes.Gray, };

                linesTileBorders[i] = line;
                canvas.Children.Add(line);
            }

            // Board border
            rectBoardBorder = new Rectangle() { Stroke = Brushes.Black, };
            canvas.Children.Add(rectBoardBorder);

            rectangleAroundSelectedPiece = new Rectangle() { Stroke = Brushes.Green, Visibility = Visibility.Hidden, };
            canvas.Children.Add(rectangleAroundSelectedPiece);

            userSelectionPath = new List<Position>();
            avaiableMovesFromSelectedPath = new List<Move>();
            avaiablePositionsForNextStep = new List<Position>();
            piecesThatCanMove = new List<Position>();

            linesSelectionPath = new List<Line>();

            ellipsesAvaiablePositions = new Ellipse[NumberOfColumns, NumberOfRows];

            linesLastMove = new List<Line>();

            for (byte row = 0; row < NumberOfRows; row++)
            {
                for (byte column = 0; column < NumberOfColumns; column++)
                {
                    // Pieces
                    var pos = new Position(column, row);
                    var pieceType = boardState.GetPieceType(pos);

                    if (pieceType != PieceType.None)
                    {
                        var pieceColor = GetColor(pieceType);
                        var pieceShape = new PieceShape() 
                        { 
                            pieceType = pieceType,
                            ellipse_base = new Ellipse() { 
                                Stroke = pieceColor == PieceColor.White ? Brushes.DarkSlateGray : Brushes.Gray,
                                Fill = pieceColor == PieceColor.White ? Brushes.White : Brushes.Black,
                            },
                        };

                        piecesOnBoard[column, row] = pieceShape;
                        foreach (var shape in pieceShape.AllShapes())
                        {
                            canvas.Children.Add(shape);
                        }

                        if (GetRank(pieceType) == PieceRank.King)
                        {
                            Promote(pieceShape);
                        }
                    }

                    // Marks of next step positions
                    var ellipse = new Ellipse()
                    {
                        Fill = Brushes.LimeGreen,
                        StrokeThickness = 0,
                        Visibility = Visibility.Hidden,
                    };

                    ellipsesAvaiablePositions[column, row] = ellipse;
                    canvas.Children.Add(ellipse);
                }
            }


            // Input
            canvas.MouseDown += Canvas_MouseDown;
        }

        public VisualiserState State { get; private set; } = VisualiserState.Idle;
        public int Size { get; private set; } = 100;
        public int OffsetX { get; private set; } = 0;
        public int OffsetY { get; private set; } = 0;
        public double TileWidth { get; private set; }
        public double TileHeight { get; private set; }
        public double Multiplier { get; private set; } = 1;
        public byte NumberOfColumns => gameControl.gameRules.numberOfColumns;
        public byte NumberOfRows => gameControl.gameRules.numberOfRows;
        public bool IsDisposed { get; private set; } = false;


        // Margin ratios
        public const double 
            boardMarginRatio = 0.05d,
            pieceMarginRatio = 0.08d,
            crownMarginRatio = 0.30d,
            avaiablePositionMarginRatio = 0.33d;

        // Animation
        public double
            animationSpeed = 5,
            animationUnitResolution = 50;

        // Const values of board of size 200
        public const double
           boardBorderThickness = 4d,
           tileBorderThickness = 1d,
           pieceBorderThickness = 1.2d,
           lastMoveLineThickness = 1d,
           ongoingMoveLineThickness = 1d,
           rectangleAroundSelectedPieceThickness = 1.5d;


        // Canvas objects
        private readonly Rectangle rectBoardBorder;
        private readonly Line[] linesTileBorders;
        private readonly Rectangle[] rectTiles;

        private readonly PieceShape[,] piecesOnBoard;


        // User Selecting move
        private Rectangle rectangleAroundSelectedPiece;
        private List<Position> userSelectionPath;
        
        private List<Move> avaiableMovesFromSelectedPath;
        private List<Move> avaiableMovesAll;
        private List<Position> piecesThatCanMove;
        private List<Position> avaiablePositionsForNextStep;
        
        private List<Line> linesSelectionPath;
        private Ellipse[,] ellipsesAvaiablePositions;


        private Move userSelectedMove;
        private bool userMoveReadyToReturn = false;
        private readonly object signaler = new object();

        // Last Move
        private Move lastMove;
        private List<Line> linesLastMove;

        // Animation
        private PieceShape movingPiece;
        private (double x, double y) movingPiecePosition;


        // Input
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not handle MouseDown event. Visualiser was disposed");
            }

            if (State != VisualiserState.UserMove)
            {
                return;
            }

            // Left button
            if (e.ChangedButton == MouseButton.Left)
            {
                double mouseX = e.GetPosition(canvas).X;
                double mouseY = e.GetPosition(canvas).Y;

                var (mouseTilePos, isMouseInBoard) = MapMouseOnTile(mouseX, mouseY);
                mouseTilePos = CorrectBoardOrientation(mouseTilePos);

                if (isMouseInBoard && boardState.IsInsideBoard(mouseTilePos))
                {
                    if (userSelectionPath.Count < 2 && piecesThatCanMove.Contains(mouseTilePos))
                    {
                        Select(mouseTilePos);
                    } 
                    else if (avaiablePositionsForNextStep.Contains(mouseTilePos))
                    {
                        MakeStep(mouseTilePos);
                    }
                }
            }
            // Right button
            else if (e.ChangedButton == MouseButton.Right)
            {
                RevertStep();
            }

            Refresh();
        }
        
        private void Select(Position pos)
        {
            if (userSelectionPath.Count > 0)
            {
                Deselect();
            }

            if (!piecesThatCanMove.Contains(pos))
            {
                throw new Exception("This piece can not be selected");
            }

            var ps = piecesOnBoard[pos.column, pos.row];

            // Readd to bring it forward
            foreach (var shape in ps.AllShapes())
            {
                canvas.Children.Remove(shape);
                canvas.Children.Add(shape);
            }

            userSelectionPath.Add(pos);
            rectangleAroundSelectedPiece.Visibility = Visibility.Visible;

            for (int i = 0; i < avaiableMovesFromSelectedPath.Count; i++)
            {
                if (avaiableMovesFromSelectedPath[i].path[0] != pos)
                {
                    avaiableMovesFromSelectedPath.RemoveAt(i--);
                }
            }
            SetupNextStepPositions();
            Refresh();
        }
        private void Deselect()
        {
            if (userSelectionPath.Count > 0)
            {
                if (userSelectionPath.Count > 1)
                {
                    var from = userSelectionPath.Last();
                    var to = userSelectionPath.First();
                    piecesOnBoard[to.column, to.row] = piecesOnBoard[from.column, from.row];
                    piecesOnBoard[from.column, from.row] = null;
                }

                userSelectionPath.Clear();
                avaiableMovesFromSelectedPath.Clear();
                avaiableMovesFromSelectedPath.AddRange(avaiableMovesAll);
                avaiablePositionsForNextStep.Clear();

                foreach (var line in linesSelectionPath)
                {
                    canvas.Children.Remove(line);
                }
                linesSelectionPath.Clear();

                rectangleAroundSelectedPiece.Visibility = Visibility.Hidden;

                SetupNextStepPositions();
                Refresh();
            }
        }
        private void MakeStep(Position pos)
        {
            if (!avaiablePositionsForNextStep.Contains(pos))
            {
                throw new Exception("Can not move to this position");
            }

            var from = userSelectionPath.Last();
            piecesOnBoard[pos.column, pos.row] = piecesOnBoard[from.column, from.row];
            piecesOnBoard[from.column, from.row] = null;

            userSelectionPath.Add(pos);
            
            var line = new Line() { StrokeDashArray = { 2d, 4d }, Stroke = Brushes.Purple, };
            linesSelectionPath.Add(line);
            canvas.Children.Add(line);
            
            avaiablePositionsForNextStep.Clear();

            for (int i = 0; i < avaiableMovesFromSelectedPath.Count; i++)
            {
                if (avaiableMovesFromSelectedPath[i].path[userSelectionPath.Count - 1] != pos)
                {
                    avaiableMovesFromSelectedPath.RemoveAt(i--);
                }
            }

            if (avaiableMovesFromSelectedPath.Count == 1 && userSelectionPath.Count == avaiableMovesFromSelectedPath[0].path.Length)
            {
                UserFinishedSelectingMove(avaiableMovesFromSelectedPath[0]);
            }
            else
            {
                SetupNextStepPositions();
                Refresh();
            }
        }
        private void RevertStep()
        {
            if (userSelectionPath.Count == 1)
            {
                Deselect();
            }
            else if (userSelectionPath.Count > 0)
            {
                var from = userSelectionPath.Last();
                var to = userSelectionPath[userSelectionPath.Count - 2];
                piecesOnBoard[to.column, to.row] = piecesOnBoard[from.column, from.row];
                piecesOnBoard[from.column, from.row] = null;

                userSelectionPath.RemoveAt(userSelectionPath.Count - 1);

                var line = linesSelectionPath.Last();
                canvas.Children.Remove(line);
                linesSelectionPath.Remove(line);

                avaiableMovesFromSelectedPath.Clear();
                foreach (var move in avaiableMovesAll)
                {
                    for (int i = 0; i < userSelectionPath.Count; i++)
                    {
                        if (move.path.Length <= i || move.path[i] != userSelectionPath[i])
                        {
                            continue;
                        }
                        else
                        {
                            avaiableMovesFromSelectedPath.Add(move);
                        }
                    }
                }

                SetupNextStepPositions();
                Refresh();
            }
        }

        private void SetupNextStepPositions()
        {
            avaiablePositionsForNextStep.Clear();
            if (userSelectionPath.Count > 0)
            {
                avaiablePositionsForNextStep.AddRange((from move in avaiableMovesFromSelectedPath select move.path[userSelectionPath.Count]).Distinct());
            }
            for (byte column = 0; column < NumberOfColumns; column++)
            {
                for (byte row = 0; row < NumberOfRows; row++)
                {
                    var pos = new Position(column, row);
                    var posCorected = CorrectBoardOrientation(pos);
                    ellipsesAvaiablePositions[posCorected.column, posCorected.row].Visibility =
                        avaiablePositionsForNextStep.Contains(pos)
                        ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        private void UserFinishedSelectingMove(Move move)
        {
            Deselect();

            lock (signaler)
            {
                userSelectedMove = move;
                userMoveReadyToReturn = true;
                Monitor.PulseAll(signaler);
            }

            SetupNextStepPositions();
            Refresh();
        }


        public Move LetUserDoTheMove(List<Move> moves)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not handle user move. Visualiser was disposed");
            }

            if (Thread.CurrentThread == canvas.Dispatcher.Thread)
            {
                throw new MethodAccessException("This method is not supposed to be called from dispatcher's thread.");
            }

            if (moves is null || moves.Count == 0)
            {
                return null;
            }

            State = VisualiserState.UserMove;

            piecesThatCanMove.AddRange((from move in moves select move.path.First()).Distinct());
            avaiableMovesFromSelectedPath.AddRange(moves);
            avaiableMovesAll = moves;

            Move m;
            lock (signaler)
            {
                while (!userMoveReadyToReturn)
                {
                    // Waits till 'playerMove' is set
                    Monitor.Wait(signaler);
                }
                m = userSelectedMove;
                userSelectedMove = null;
                userMoveReadyToReturn = false;
            }

            piecesThatCanMove.Clear();
            avaiableMovesFromSelectedPath.Clear();
            avaiableMovesAll = null;

            State = VisualiserState.Idle;
            return m;
        }
        public void ApplyMove(Move move, bool animate)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not apply move. Visualiser disposed.");
            }

            canvas.Dispatcher.Invoke(() =>
            {
                foreach (var line in linesLastMove)
                {
                    canvas.Children.Remove(line);
                }
                linesLastMove.Clear();
                lastMove = move;
            });

            if (animate)
            {
                State = VisualiserState.Animating;

                for (int i = 0; i < move.path.Length - 1; i++)
                {
                    var from = move.path[i];
                    var to = move.path[i + 1];

                    var distance = Math.Sqrt(Math.Pow(to.column - from.column, 2) + Math.Pow(to.row - from.row, 2));

                    var ps = piecesOnBoard[from.column, from.row];
                    piecesOnBoard[from.column, from.row] = null;

                    var correctedFrom = CorrectBoardOrientation(from);
                    var correctedTo = CorrectBoardOrientation(to);

                    movingPiece = ps;
                    movingPiecePosition = (correctedFrom.column, correctedFrom.row);

                    canvas.Dispatcher.Invoke(() =>
                    {
                        // Readd to bring it forward
                        foreach (var shape in ps.AllShapes())
                        {
                            canvas.Children.Remove(shape);
                            canvas.Children.Add(shape);
                        }

                        var line = new Line()
                        {
                            StrokeDashArray = { 4d, 4d },
                            Stroke = Brushes.Pink,
                        };
                        linesLastMove.Add(line);
                        canvas.Children.Add(line);
                    });

                    double dx = correctedTo.column - correctedFrom.column;
                    double dy = correctedTo.row - correctedFrom.row;

                    double delta = animationUnitResolution;// * distance;

                    double ddx = dx / delta;
                    double ddy = dy / delta;

                    if (i != 0)
                    {
                        Thread.Sleep(100);
                    }
                    for (int j = 0; j < delta; j++)
                    {
                        movingPiecePosition = (movingPiecePosition.x + ddx, movingPiecePosition.y + ddy);
                        canvas.Dispatcher.Invoke(Refresh);

                        Thread.Sleep((int)(1000 / animationUnitResolution / animationSpeed));
                    }

                    movingPiece = null;
                    piecesOnBoard[to.column, to.row] = ps;


                    if (move.promotion == i + 1)
                    {
                        canvas.Dispatcher.Invoke(() => Promote(ps));
                    }

                    canvas.Dispatcher.Invoke(Refresh);
                }


                canvas.Dispatcher.Invoke(() =>
                {
                    if (move.positionsOfTakenPieces != null)
                    {
                        foreach (var pos in move.positionsOfTakenPieces)
                        {
                            RemovePieceFromBoard(pos);
                        }
                    }

                    Refresh();
                });

                State = VisualiserState.Idle;
            }
            else
            {
                canvas.Dispatcher.Invoke(() =>
                {
                    var from = move.path.First();
                    var to = move.path.Last();

                    var ps = piecesOnBoard[from.column, from.row];
                    piecesOnBoard[from.column, from.row] = null;
                    piecesOnBoard[to.column, to.row] = ps;

                    if (move.promotion != -1)
                    {
                        Promote(ps);
                    }

                    if (move.positionsOfTakenPieces != null)
                    {
                        foreach (var pos in move.positionsOfTakenPieces)
                        {
                            RemovePieceFromBoard(pos);
                        }
                    }

                    // Last Move Lines
                    for (int i = 0; i < move.path.Length - 1; i++)
                    {
                        var line = new Line()
                        {
                            StrokeDashArray = { 4d, 4d },
                            Stroke = Brushes.Pink,
                        };
                        linesLastMove.Add(line);
                        canvas.Children.Add(line);
                    }

                    Refresh();

                });
            }

#if DEBUG   // Consistency check
            foreach (var (pos, pieceType) in boardState.IterateBoard())
            {
                if (pieceType == PieceType.None && piecesOnBoard[pos.column, pos.row] == null
                    || pieceType == piecesOnBoard[pos.column, pos.row].pieceType)
                {

                }
                else
                {
                    throw new Exception("Inconsistency found");
                }
            }
#endif
        }

        private void Promote(PieceShape ps)
        {
            ps.crown = new Ellipse()
            {
                StrokeThickness = 0,
                Fill = Brushes.Blue,
            };
            canvas.Children.Add(ps.crown);
            ps.pieceType = PromoteToKing(ps.pieceType);
        }

        public (Position tilePos, bool isInBoard) MapMouseOnTile(double mouseX, double mouseY)
        {
            double x = mouseX - OffsetX;
            double y = mouseY - OffsetY;

            if (x < 0 || x > Size || y < 0 || y > Size)
            {
                return (Position.None, false);
            }
            else
            {
                return (new Position() { column = (byte)(x * NumberOfColumns / Size), row = (byte)(y * NumberOfRows / Size) }, true);
            }
        }

        // Methods
        public void Refresh()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not refresh visualiser after it was disposed");
            }
            Resize((int)canvas.ActualWidth, (int)canvas.ActualHeight);
        }

        private void Canvas_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not handle SizeChanged event. Visualiser was disposed");
            }

            Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        public void Resize(int width, int height)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Can not resize visualiser after it was disposed");
            }

            Size = (int)((1 - 2 * boardMarginRatio) * Math.Min(width, height));

            OffsetX = (width - Size) / 2;
            OffsetY = (height - Size) / 2;

            Multiplier = Size / 200d;

            // Tiles
            TileWidth = (double)Size / NumberOfColumns;
            TileHeight = (double)Size / NumberOfRows;
            for (int row = 0; row < NumberOfRows; row++)
            {
                for (int column = 0; column < NumberOfColumns; column++)
                {
                    int i = row * NumberOfColumns + column;

                    var tile = rectTiles[i];
                    tile.Width = TileWidth;
                    tile.Height = TileHeight;

                    Canvas.SetLeft(tile, OffsetX + column * TileWidth);
                    Canvas.SetTop(tile, OffsetY + row * TileHeight);
                }
            }

            // Pieces
            for (byte column = 0; column < NumberOfColumns; column++)
            {
                for (byte row = 0; row < NumberOfRows; row++)
                {
                    var pieceShape = piecesOnBoard[column, row];

                    if (pieceShape != null)
                    {
                        var pos = CorrectBoardOrientation(new Position(column, row));
                        var pixelPosX = OffsetX + (pos.column + .5d) * TileWidth;
                        var pixelPosY = OffsetY + (pos.row + .5d) * TileHeight;
                        DrawPiece(pixelPosX, pixelPosY, 1d, pieceShape);
                    }
                }
            }
            if (movingPiece != null)
            {
                var pixelPosX = OffsetX + (movingPiecePosition.x + .5d) * TileWidth;
                var pixelPosY = OffsetY + (movingPiecePosition.y + .5d) * TileHeight;
                DrawPiece(pixelPosX, pixelPosY, 1d, movingPiece);
            }


            // Last move
            if (lastMove != null)
            {
                for (int i = 0; i < linesLastMove.Count; i++)
                {
                    var line = linesLastMove[i];
                    var from = CorrectBoardOrientation(lastMove.path[i]);
                    var to = CorrectBoardOrientation(lastMove.path[i + 1]);

                    line.X1 = OffsetX + (from.column + 0.5d) * TileWidth;
                    line.Y1 = OffsetY + (from.row + 0.5d) * TileHeight;

                    if (i != linesLastMove.Count - 1 || movingPiece == null)
                    {
                        line.X2 = OffsetX + (to.column + 0.5d) * TileWidth;
                        line.Y2 = OffsetY + (to.row + 0.5d) * TileHeight;
                    }
                    else
                    {
                        line.X2 = OffsetX + (movingPiecePosition.x + 0.5d) * TileWidth;
                        line.Y2 = OffsetY + (movingPiecePosition.y + 0.5d) * TileHeight;
                    }

                    line.StrokeThickness = lastMoveLineThickness * Multiplier;
                }
            }

            // Ongoing move
            if (userSelectionPath.Count > 0)
            {
                rectangleAroundSelectedPiece.StrokeThickness = rectangleAroundSelectedPieceThickness * Multiplier;
                rectangleAroundSelectedPiece.Width = TileWidth;
                rectangleAroundSelectedPiece.Height = TileHeight;

                var pos = CorrectBoardOrientation(userSelectionPath.First());
                Canvas.SetLeft(rectangleAroundSelectedPiece, OffsetX + pos.column * TileWidth);
                Canvas.SetTop(rectangleAroundSelectedPiece, OffsetY + pos.row * TileHeight);
            }

            if (userSelectionPath.Count > 1)
            {
                for (int i = 0; i < userSelectionPath.Count - 1; i++)
                {
                    var line = linesSelectionPath[i];
                    var from = CorrectBoardOrientation(userSelectionPath[i]);
                    var to = CorrectBoardOrientation(userSelectionPath[i+1]);

                    line.StrokeThickness = ongoingMoveLineThickness * Multiplier;
                    line.X1 = OffsetX + (from.column + .5d) * TileWidth;
                    line.Y1 = OffsetY + (from.row + .5d) * TileHeight;

                    line.X2 = OffsetX + (to.column + .5d) * TileWidth;
                    line.Y2 = OffsetY + (to.row + .5d) * TileHeight;
                }
            }


            // Avaiable Positions
            for (int column = 0; column < NumberOfColumns; column++)
            {
                for (int row = 0; row < NumberOfRows; row++)
                {
                    var ellipse = ellipsesAvaiablePositions[column, row];

                    Canvas.SetLeft(ellipse, OffsetX + (column + avaiablePositionMarginRatio) * TileWidth);
                    Canvas.SetTop (ellipse, OffsetY + (row    + avaiablePositionMarginRatio) * TileHeight);

                    ellipse.Width  = (1 - 2 * avaiablePositionMarginRatio) * TileWidth;
                    ellipse.Height = (1 - 2 * avaiablePositionMarginRatio) * TileHeight;
                }
            }

            // Tile Line Borders
            for (int i = 0; i < NumberOfRows - 1; i++)
            {
                var line = linesTileBorders[i];
                line.StrokeThickness = tileBorderThickness * Multiplier;

                line.X1 = OffsetX;
                line.Y1 = OffsetY + (i + 1) * Size / NumberOfRows;

                line.X2 = OffsetX + Size;
                line.Y2 = OffsetY + (i + 1) * Size / NumberOfRows;
            }
            for (int i = 0; i < NumberOfColumns - 1; i++)
            {
                var line = linesTileBorders[NumberOfRows - 1 + i];
                line.StrokeThickness = tileBorderThickness * Multiplier;

                line.X1 = OffsetX + (i + 1) * Size / NumberOfColumns;
                line.Y1 = OffsetY;

                line.X2 = OffsetX + (i + 1) * Size / NumberOfColumns;
                line.Y2 = OffsetY + Size;
            }

            // Board border
            rectBoardBorder.StrokeThickness = boardBorderThickness * Multiplier;
            rectBoardBorder.Width = Size + 2 * rectBoardBorder.StrokeThickness;
            rectBoardBorder.Height = Size + 2 * rectBoardBorder.StrokeThickness;
            Canvas.SetLeft(rectBoardBorder, OffsetX - rectBoardBorder.StrokeThickness);
            Canvas.SetTop(rectBoardBorder, OffsetY - rectBoardBorder.StrokeThickness);
        }
        private void DrawPiece(double centerX, double centerY, double scale, PieceShape pieceShape)
        {
            double width = TileWidth * scale;
            double height = TileHeight * scale;

            double x = centerX - width / 2;
            double y = centerY - height / 2;

            double pieceWidth = width * (1 - 2 * pieceMarginRatio);
            double pieceHeight = height * (1 - 2 * pieceMarginRatio);

            // ellipse_base
            pieceShape.ellipse_base.StrokeThickness = pieceBorderThickness * Multiplier;

            pieceShape.ellipse_base.Width = pieceWidth;
            pieceShape.ellipse_base.Height = pieceHeight;

            Canvas.SetLeft(pieceShape.ellipse_base, x + pieceMarginRatio * width);
            Canvas.SetTop(pieceShape.ellipse_base, y + pieceMarginRatio * height);

            // crown   TODO
            if (pieceShape.crown != null)
            {
                double crownWidth = width * (1 - 2 * crownMarginRatio);
                double crownHeight = height * (1 - 2 * crownMarginRatio);

                pieceShape.crown.Width = crownWidth;
                pieceShape.crown.Height = crownHeight;

                Canvas.SetLeft(pieceShape.crown, x + crownMarginRatio * width);
                Canvas.SetTop(pieceShape.crown, y + crownMarginRatio * height);
            }
        }

        public Position CorrectBoardOrientation(Position pos)
        {
            return new Position(
                playersPerspective == PieceColor.White ? pos.column : (byte)(NumberOfColumns - 1 - pos.column),
                playersPerspective == PieceColor.White ? pos.row : (byte)(NumberOfRows - 1 - pos.row));
        }

        public void Dispose()
        {
            // TODO make sure everything is disposed

            canvas.Children.Remove(rectBoardBorder);
            foreach (var line in linesTileBorders)
            {
                canvas.Children.Remove(line);
            }
            foreach (var tile in rectTiles)
            {
                canvas.Children.Remove(tile);
            }

            foreach (var piece in piecesOnBoard)
            {
                if (piece != null)
                {
                    foreach (var shape in piece.AllShapes())
                    {
                        canvas.Children.Remove(shape);
                    }
                }
            }

            canvas.Children.Remove(rectangleAroundSelectedPiece);

            foreach (var line in linesLastMove)
            {
                canvas.Children.Remove(line);
            }

            foreach (var line in linesSelectionPath)
            {
                canvas.Children.Remove(line);
            }
            foreach (var ellipse in ellipsesAvaiablePositions)
            {
                canvas.Children.Remove(ellipse);
            }

            canvas.MouseDown -= Canvas_MouseDown;
            canvas.SizeChanged -= Canvas_SizeChanged;

            IsDisposed = true;
        }
        private void RemovePieceFromBoard(Position pos)
        {
            var piece = piecesOnBoard[pos.column, pos.row];
            piecesOnBoard[pos.column, pos.row] = null;

            foreach (var shape in piece.AllShapes())
            {
                canvas.Children.Remove(shape);
            }
        }
    }
}