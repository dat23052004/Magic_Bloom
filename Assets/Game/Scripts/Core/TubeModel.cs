using System;
using System.Collections.Generic;


public class TubeModel
{
    public int capacity { get; }
    public int totalColor { get; }
    public List<ColorSegment> segments { get; } = new();

    public TubeModel(int capacity, int totalColor)
    {
        this.capacity = capacity;
        this.totalColor = totalColor;
    }

    public int filledAmount
    {
        get
        {
            int sum = 0;
            for(int  i = 0; i < segments.Count; i++)
            {
                sum += segments[i].Amount;
            }
            return sum;
        }
    }

    public int freeAmount => capacity - filledAmount;
    public bool isEmpty => segments.Count == 0;
    public bool GetTop(out ColorSegment seg)
    {
        if (isEmpty)
        {
            seg = default;
            return false;
        }

        seg = segments[segments.Count - 1];
        return true;
    }
}

