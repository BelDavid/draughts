using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Draughts.Pieces
{
   public class PieceShape
   {
      public PieceType pieceType;

      public Ellipse ellipse_base;
      public Ellipse crown; // TODO change to actual crown

      public IEnumerable<Shape> AllShapes()
      {
         return from shape in AllShapes_() where shape != null select shape;
      }
      private IEnumerable<Shape> AllShapes_()
      {
         yield return ellipse_base;
         yield return crown;
      }
   }
}