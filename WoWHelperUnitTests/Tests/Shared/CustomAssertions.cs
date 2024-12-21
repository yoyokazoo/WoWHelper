using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

internal static class CustomAssert
{
    public static void DoublesAreAlmostEqual(this Assert assert, double expected, double actual)
    {
        bool almostEqual = Math.Abs(expected - actual) < .01;
        if (almostEqual)
        {
            return;
        }

        throw new AssertFailedException($"Expected: {expected} Actual: {actual}");
    }
}