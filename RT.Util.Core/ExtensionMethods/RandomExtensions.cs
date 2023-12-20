namespace RT.Util.ExtensionMethods;

/// <summary>Provides extension methods on the <see cref="Random"/> type.</summary>
public static class RandomExtensions
{
    /// <summary>Creates a byte array of <paramref name="length"/> elements and populates it with random numbers.</summary>
    public static byte[] NextBytes(this Random rnd, int length)
    {
        var bytes = new byte[length];
        rnd.NextBytes(bytes);
        return bytes;
    }

    /// <summary>
    ///     Returns a random string of length <paramref name="length"/>, containing only characters listed in <paramref
    ///     name="charset"/>.</summary>
    public static string NextString(this Random rnd, int length, string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
    {
        if (charset == null)
            throw new ArgumentNullException(nameof(charset));
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = charset[rnd.Next(charset.Length)];
        return new string(chars);
    }

    /// <summary>Returns a random number that is greater than or equal to 0, and less than <paramref name="max"/>.</summary>
    public static double NextDouble(this Random rnd, double max)
    {
        if (max < 0)
            throw new ArgumentException("\"max\" must not be less than zero", nameof(max));
        return rnd.NextDouble() * max;
    }

    /// <summary>
    ///     Returns a random number that is greater than or equal to <paramref name="min"/>, and less than <paramref
    ///     name="max"/>.</summary>
    public static double NextDouble(this Random rnd, double min, double max)
    {
        if (min > max)
            throw new ArgumentException("\"min\" must not be greater than \"max\"", nameof(min));
        return min + rnd.NextDouble() * (max - min);
    }

#if !NET6_0_OR_GREATER
    /// <summary>Returns a random number that is greater than or equal to 0.0f, and less than 1.0f.</summary>
    public static float NextSingle(this Random rnd)
    {
        return (float) rnd.NextDouble();
    }
#endif

    /// <summary>Returns a random number that is greater than or equal to 0, and less than <paramref name="max"/>.</summary>
    public static float NextSingle(this Random rnd, float max)
    {
        if (max < 0)
            throw new ArgumentException("\"max\" must not be less than zero", nameof(max));
        return rnd.NextSingle() * max;
    }

    /// <summary>
    ///     Returns a random number that is greater than or equal to <paramref name="min"/>, and less than <paramref
    ///     name="max"/>.</summary>
    public static float NextSingle(this Random rnd, float min, float max)
    {
        if (min > max)
            throw new ArgumentException("\"min\" must not be greater than \"max\"", nameof(min));
        return min + rnd.NextSingle() * (max - min);
    }
}
