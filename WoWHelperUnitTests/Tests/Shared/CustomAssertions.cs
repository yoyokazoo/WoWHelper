using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

public static class AssertExtensions
{
    public static void DoublesAreAlmostEqual(double expected, double actual, double tolerance = 0.01)
    {
        double diff = Math.Abs(expected - actual);
        if (diff <= tolerance)
            return;

        throw new AssertFailedException(
            $"Expected: {expected}, Actual: {actual}, Diff: {diff}, Tolerance: {tolerance}");
    }

    /// <summary>
    /// Asserts that two floats are approximately equal within a given tolerance.
    /// Throws an exception if they differ beyond epsilon.
    /// </summary>
    public static void AssertFloatApproximately(
        float expected,
        float actual,
        float epsilon = .01f)
    {
        float diff = Math.Abs(expected - actual);

        if (diff <= epsilon)
            return;

        // Relative check — helps when values are large
        float largest = Math.Max(Math.Abs(expected), Math.Abs(actual));
        if (diff <= largest * epsilon)
            return;

        throw new Exception(
            $"Float approximate equality failed.\n" +
            $"Expected: {expected}\n" +
            $"Actual:   {actual}\n" +
            $"Diff:     {diff}\n" +
            $"Epsilon:  {epsilon}");
    }
}
