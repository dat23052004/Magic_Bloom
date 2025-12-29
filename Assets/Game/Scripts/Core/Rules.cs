using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public static class Rules
{
    public static bool CanPour(TubeModel from, TubeModel to)
    {
        if(from.isEmpty) return false;
        if (ReferenceEquals(from, to)) return false;
        if (!from.isEmpty && from.filledAmount == from.capacity && from.segments.Count == 1)
            return false;
        from.GetTop(out var fromTop);
        if (to.freeAmount < fromTop.Amount) return false; 
        if(to.isEmpty) return true;
        to.GetTop(out var toTop);
        return toTop.colorId == fromTop.colorId;
    }

    public static void Pour(TubeModel from, TubeModel to)
    {
        if (!CanPour(from, to)) return;
        from.GetTop(out var fromTop);
        from.segments.RemoveAt(from.segments.Count - 1);
        if (!to.isEmpty && to.GetTop(out var toTop) && toTop.colorId == fromTop.colorId)
        {
            toTop.Amount += fromTop.Amount;
            to.segments[to.segments.Count - 1] = toTop;
        }
        else
        {
            to.segments.Add(fromTop);
        }
    }

    public static bool IsCompleted(TubeModel tube)
    {
        if (tube.isEmpty) return true;
        if (tube.segments.Count != 1) return false;
        return tube.segments[0].Amount == 2;
    }
}

