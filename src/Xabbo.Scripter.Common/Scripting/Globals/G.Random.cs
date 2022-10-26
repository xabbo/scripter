using System;
using System.Collections.Generic;
using System.Linq;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    public int Rand() => _scriptHost.Random.Next();

    /// <summary>
    /// Returns a non-negative integer that is less than the specified maximum.
    /// </summary>
    public int Rand(int max) => _scriptHost.Random.Next(max);

    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    public int Rand(int min, int max) => _scriptHost.Random.Next(min, max);

    /// <summary>
    /// Fills the elements of a specified array of bytes with random numbers.
    /// </summary>
    public void Rand(byte[] buffer) => _scriptHost.Random.NextBytes(buffer);

    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    /// <returns></returns>
    public double RandDouble() => _scriptHost.Random.NextDouble();

    /// <summary>
    /// Selects a random element from a specified enumerable.
    /// Returns <see langword="default"/>(<typeparamref name="T"/>) if the enumerable is empty.
    /// </summary>
    public T? Rand<T>(IEnumerable<T> enumerable)
    {
        if (enumerable is not Array array)
            array = enumerable.ToArray();

        if (array.Length == 0)
            return default;

        return (T?)array.GetValue(Rand(array.Length));
    }

    /// <summary>
    /// Returns a random element from a specified array.
    /// </summary>
    public T Rand<T>(T[] array) => array[Rand(array.Length)];
}
