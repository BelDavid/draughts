using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draughts.Visualisation
{
   public enum VisualiserState
   {
      Idle,
      UserMove,
      Animating,
      Terminating,
      Disposed,
   }
}
