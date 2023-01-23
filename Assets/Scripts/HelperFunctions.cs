using System.Collections.Generic;

public class HelperFunctions
{
    public static void FindMinIndexOfMulti(IReadOnlyList<float[]> arr, out int index, out int element)
    {
        index = 0;
        element = 0;
        for (int i = 0; i < arr.Count; i++)
        {
            for (int e = 0; e < arr[i].Length; e++)
            {
                if (!(arr[i][e] < arr[index][element])) continue;
                
                index = i;
                element = e;
            }
            
        }
    }
}
