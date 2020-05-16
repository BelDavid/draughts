using Draughts.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Draughts.GUI
{
   public class PieceShape
   {
      public PieceType pieceType;

      public Ellipse ellipse_base;
      public Image image_crown; // TODO change to actual crown

      public IEnumerable<UIElement> AllShapes()
      {
         return from shape in AllShapes_() where shape != null select shape;
      }
      private IEnumerable<UIElement> AllShapes_()
      {
         yield return ellipse_base;
         yield return image_crown;
      }
   }
}