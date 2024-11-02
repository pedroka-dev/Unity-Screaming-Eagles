using UnityEngine;

public static class UnityExtensions
{
    /// <summary>
    /// Extension method to check if a Layer is in a Layermask. 
    /// </summary>
    /// <param name="mask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool Contains(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }
}
