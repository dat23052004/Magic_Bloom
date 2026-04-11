using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoManager : Singleton<UndoManager>
{
    private Stack<MoveRecord> history = new();
    public bool HasHistory => history.Count > 0;
    public event Action<bool> OnHistoryChanged;

    public void RecordMove(int fromIndex, int toIndex, ColorSegment segment)
    {
        history.Push(new MoveRecord
        {
            fromIndex = fromIndex,
            toIndex = toIndex,
            segment = segment
        });

        NotifyHistoryChanged();
    }

    public bool Undo(List<TubeModel> models, List<TubeView> views)
    {
        if (history.Count == 0) return false;

        var record = history.Peek();
        var fromTube = models[record.toIndex];
        var toTube = models[record.fromIndex];

        if(!fromTube.GetTop(out var top) || top.colorId != record.segment.colorId || top.Amount < record.segment.Amount)
        {
            Debug.LogError("Undo failed: tube state mismatch");
            return false;
        }

        history.Pop();

        if(top.Amount == record.segment.Amount)
        {
            fromTube.segments.RemoveAt(fromTube.segments.Count - 1);
        }
        else
        {
            top.Amount -= record.segment.Amount;
            fromTube.segments[fromTube.segments.Count - 1] = top;
        }

        if(!toTube.isEmpty && toTube.GetTop(out var toTop) && toTop.colorId == record.segment.colorId)
        {
            toTop.Amount += record.segment.Amount;
            toTube.segments[toTube.segments.Count - 1] = toTop;
        }
        else
        {
            toTube.segments.Add(record.segment);
        }

        views[record.fromIndex].Refresh();
        views[record.toIndex].Refresh();

        NotifyHistoryChanged();
        return true;
    }
    public void ClearHistory()
    {
        history.Clear();
        NotifyHistoryChanged();
    }

    private void NotifyHistoryChanged()
    {
        OnHistoryChanged?.Invoke(HasHistory);
    }

}

public struct MoveRecord
{
    public int fromIndex;
    public int toIndex;
    public ColorSegment segment;
}
