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
}
