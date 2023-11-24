namespace PackSF;

public static class CommonUtils
{
    public static List<T> Filter<T>(this List<T> list, Func<T, int, bool> filter, bool newList = false)
    {
        if (newList)
        {
            var newList2 = new List<T>();
            for (var n = 0; n < list.Count; n++)
            {
                var element = list[n];
                if (filter(element, n))
                {
                    newList2.Add(element);
                }
            }

            return newList2;
        }

        for (var n = 0; n < list.Count; n++)
        {
            if (!filter(list[n], n))
            {
                list.RemoveAt(n);
            }
        }

        return list;
    }

    public static int GetGreaterPowerOf2(int cur)
    {
        var result = 1;
        while (result <= cur)
        {
            result *= 2;
        }

        return result;
    }
}