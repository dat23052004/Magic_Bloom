using UnityEngine;

public class LevelValidator
{
    public static void Validate(LevelDataSO levelData)
    {
        foreach(var tube in levelData.tubes)
        {
            int sum = 0;
            foreach(var seg in tube.segmentsBottomToTop)
            {
                if(seg.Amount <= 0)
                {
                    Debug.LogError($"Tube has segment with non-positive amount: {seg.Amount}");
                }
                sum += seg.Amount;
            }
            if(sum>levelData.capacity) 
            {
                Debug.LogError($"Tube exceeds capacity. Capacity: {levelData.capacity}, Sum of segments: {sum}");
            }
        }
    }
}
