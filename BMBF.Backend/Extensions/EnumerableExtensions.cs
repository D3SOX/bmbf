using System.Collections.Generic;
using System.Linq;

namespace BMBF.Backend.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Checks if <paramref name="left"/> contains the same elements as <paramref name="right"/>.
    /// This alternative allows for either left or right to be null.
    /// </summary>
    /// <param name="left">Operand A</param>
    /// <param name="right">Operand B</param>
    /// <typeparam name="T">The types of the values within the collections</typeparam>
    /// <returns>true if all the elements in <paramref name="left"/> equal those in <paramref name="right"/>, and the
    /// lengths of the operands are the same.</returns>
    public static bool NullableSequenceEquals<T>(this IEnumerable<T>? left, IEnumerable<T>? right)
    {
        if ((left == null && right != null) || (left != null && right == null))
        {
            return false;
        }

        if (left == null && right == null)
        {
            return true;
        }

        return left!.SequenceEqual(right!);
    }
}
