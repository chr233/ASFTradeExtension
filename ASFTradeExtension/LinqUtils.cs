namespace ASFTradeExtension;
internal static class LinqUtils
{
    /// <summary>
    /// 增加1
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    public static void Increase<T>(this Dictionary<T, int> dict, T key) where T : notnull
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = 0;
        }
        dict[key] = value + 1;
    }

    /// <summary>
    /// 最小值
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static int MinValue(this IEnumerable<int> list)
    {
        int min = int.MaxValue;
        foreach (var item in list)
        {
            if (item < min)
            {
                min = item;
            }
        }
        return min;
    }

    /// <summary>
    /// 总和
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static int SumValue(this IEnumerable<int> list)
    {
        int sum = 0;
        foreach (var item in list)
        {
            sum += item;
        }
        return sum;
    }
}
