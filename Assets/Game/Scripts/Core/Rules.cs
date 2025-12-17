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

        var fromTop = from.top.Value;
        if (to.freeAmount < fromTop.Amount) return false; 
        if(to.isEmpty) return true;
        return to.top.Value.colorId == fromTop.colorId;
    }

    public static void Pour(TubeModel from, TubeModel to)
    {
        if (!CanPour(from, to)) return;

        var seg = from.top.Value;
        from.segments.RemoveAt(from.segments.Count - 1);

        if (!to.isEmpty && to.top.Value.colorId == seg.colorId)
        {
            var toTop = to.top.Value;
            toTop.Amount += seg.Amount;
            to.segments[to.segments.Count - 1] = toTop;
        }
        else
        {
            to.segments.Add(seg);
        }
    }

}

