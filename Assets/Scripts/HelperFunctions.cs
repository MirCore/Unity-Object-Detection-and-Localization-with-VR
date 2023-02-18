using System.Collections.Generic;

public static class HelperFunctions
{
    public static void FindMinIndexOfMulti(IReadOnlyList<float[]> arr, out int index, out int element)
    {
        index = 0;
        element = 0;
        for (int i = 0; i < arr.Count; i++)
        {
            for (int e = 0; e < arr[i].Length; e++)
            {
                if (float.IsNaN(arr[i][e])) continue;
                if (!(arr[i][e] < arr[index][element]) && !float.IsNaN(arr[index][element])) continue;

                index = i;
                element = e;
            }
        }
    }
    
    public static void RemoveUsedPairsInDistanceArray(int aCount, int bCount, IReadOnlyList<float[]> distanceArray, int a,
        int b)
    {
        for (int x = 0; x < bCount; x++)
        {
            distanceArray[x][a] = float.NaN;
        }

        for (int y = 0; y < aCount; y++)
        {
            distanceArray[b][y] = float.NaN;
        }
    }
}