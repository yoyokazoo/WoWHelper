using InputManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WoWHelper.Code;

namespace WoWHelper
{
    // Ad-hoc calibration tasks for measuring how far the character actually turns per input
    // (the mouse sweep produced WowPathfinding.MOUSE_DRAG_PIXELS_PER_DEGREE; RightClickDragTask
    // lives in WowMovementTasks.cs). Not wired into the core loop -- call from AdHocTestTask().
    public partial class WowPlayer
    {
        private const int CALIBRATION_ITERATIONS = 20;
        private const int CALIBRATION_TURN_HOLD_MILLIS = 500;

        // How long to wait after an input ends before reading FacingDegrees back out, so the
        // addon's pixel row has repainted with the post-turn heading.
        private const int CALIBRATION_SETTLE_MILLIS = 300;

        // Holds TURN_RIGHT for CALIBRATION_TURN_HOLD_MILLIS, measures degrees turned, then
        // holds TURN_LEFT for the same duration and measures again -- CALIBRATION_ITERATIONS
        // times -- then prints mean/std dev per direction.
        public async Task<bool> MeasureKeyboardTurnRateTask()
        {
            var rightDeltas = new List<float>();
            var leftDeltas = new List<float>();

            for (int i = 0; i < CALIBRATION_ITERATIONS; i++)
            {
                rightDeltas.Add(await MeasureTurnDegreesTask(() => HoldKeyTask(WowInput.TURN_RIGHT, CALIBRATION_TURN_HOLD_MILLIS)));
                leftDeltas.Add(await MeasureTurnDegreesTask(() => HoldKeyTask(WowInput.TURN_LEFT, CALIBRATION_TURN_HOLD_MILLIS)));
                Console.WriteLine($"Keyboard turn calibration {i + 1}/{CALIBRATION_ITERATIONS}: right {rightDeltas[i]:0.00}, left {leftDeltas[i]:0.00}");
            }

            PrintCalibrationStats($"Keyboard TURN_RIGHT held {CALIBRATION_TURN_HOLD_MILLIS}ms", rightDeltas);
            PrintCalibrationStats($"Keyboard TURN_LEFT held {CALIBRATION_TURN_HOLD_MILLIS}ms", leftDeltas);
            return true;
        }

        private const int MOUSE_SWEEP_ITERATIONS_PER_PIXEL = 5;
        // Safety cap so the sweep can't run forever if the stop condition is never met.
        private const int MOUSE_SWEEP_MAX_PIXELS = 2000;

        // Steps the right-click drag distance from startPixels upward by stepPixels, measuring
        // MOUSE_SWEEP_ITERATIONS_PER_PIXEL right drags and left drags at each distance. Stops
        // at the first distance where every sample in both directions turned >= 180 degrees
        // (we never need to turn further than that -- we'd turn the other way instead).
        // Prints a per-distance line as it goes, and a CSV summary at the end for pasting.
        public async Task<bool> MouseTurnRateSweepTask(int startPixels = 1, int stepPixels = 1)
        {
            var summary = new List<string> { "pixels,rightMean,rightStdDev,rightMin,rightMax,leftMean,leftStdDev,leftMin,leftMax" };

            // Warm-up pair, discarded -- the first drag after focusing the window can misread.
            await MeasureMouseTurnDegreesTask(startPixels);
            await MeasureMouseTurnDegreesTask(-startPixels);

            for (int pixels = startPixels; pixels <= MOUSE_SWEEP_MAX_PIXELS; pixels += stepPixels)
            {
                var (rightTurned, leftTurned) = await MeasureMouseTurnRateTask(pixels, MOUSE_SWEEP_ITERATIONS_PER_PIXEL);

                var right = ComputeStats(rightTurned);
                var left = ComputeStats(leftTurned);
                string line = $"{pixels},{right.mean:0.00},{right.stdDev:0.00},{right.min:0.00},{right.max:0.00}," +
                    $"{left.mean:0.00},{left.stdDev:0.00},{left.min:0.00},{left.max:0.00}";
                summary.Add(line);
                Console.WriteLine($"Mouse sweep: {line}");

                if (right.min >= 180 && left.min >= 180)
                {
                    Console.WriteLine($"Mouse sweep: {pixels}px consistently turns >= 180 degrees in both directions, stopping");
                    break;
                }
            }

            Console.WriteLine("Mouse sweep summary:");
            Console.WriteLine(string.Join(Environment.NewLine, summary));
            return true;
        }

        // Right-click drags dragPixels right and then left, iterations times each. Returns the
        // degrees turned in the intended direction for each sample (see MeasureMouseTurnDegreesTask).
        public async Task<(List<float> rightTurned, List<float> leftTurned)> MeasureMouseTurnRateTask(int dragPixels, int iterations)
        {
            var rightTurned = new List<float>();
            var leftTurned = new List<float>();

            for (int i = 0; i < iterations; i++)
            {
                rightTurned.Add(await MeasureMouseTurnDegreesTask(dragPixels));
                leftTurned.Add(await MeasureMouseTurnDegreesTask(-dragPixels));
            }

            return (rightTurned, leftTurned);
        }

        // Degrees turned by a single drag, measured in the drag's own direction rather than
        // MeasureTurnDegreesTask's (-180, 180] signed delta -- that range can't tell a 190 degree
        // right turn from a 170 degree left one, and the sweep needs to see past 180 to know when
        // to stop. Mapped into [-90, 270): small negatives stay negative (noise/jitter against
        // the intended direction), anything past 180 reads as past 180.
        private async Task<float> MeasureMouseTurnDegreesTask(int deltaX)
        {
            float delta = await MeasureTurnDegreesTask(() => RightClickDragTask(deltaX));
            // Facing increases turning left, and a positive deltaX drags right.
            float turned = deltaX > 0 ? -delta : delta;
            if (turned < -90f) turned += 360f;
            return turned;
        }

        // Reads the heading, runs turnAction, waits for the pixel row to catch up, reads the
        // heading again. Returns the signed, wraparound-normalized delta in (-180, 180]:
        // positive = turned left (counterclockwise, WoW's facing direction of increase),
        // negative = turned right.
        private async Task<float> MeasureTurnDegreesTask(Func<Task> turnAction)
        {
            await Task.Delay(CALIBRATION_SETTLE_MILLIS);
            UpdateWorldState();
            float before = WorldState.FacingDegrees;

            await turnAction();

            await Task.Delay(CALIBRATION_SETTLE_MILLIS);
            UpdateWorldState();
            float after = WorldState.FacingDegrees;

            return NormalizeDegreesDelta(after - before);
        }

        private static float NormalizeDegreesDelta(float delta)
        {
            delta %= 360f;
            if (delta > 180f) delta -= 360f;
            if (delta <= -180f) delta += 360f;
            return delta;
        }

        private static async Task HoldKeyTask(Keys key, int millis)
        {
            try
            {
                Keyboard.KeyDown(key);
                await Task.Delay(millis);
            }
            finally
            {
                Keyboard.KeyUp(key);
            }
        }

        // Stats on the magnitude turned (sign stripped, so left and right are comparable),
        // using sample std dev. Deltas are already wraparound-normalized, so no 359->1 jumps.
        private static void PrintCalibrationStats(string label, List<float> deltas)
        {
            var stats = ComputeStats(deltas.Select(d => Math.Abs(d)));

            Console.WriteLine($"{label}: n={deltas.Count}, mean {stats.mean:0.00} degrees, std dev {stats.stdDev:0.00} degrees, " +
                $"min {stats.min:0.00}, max {stats.max:0.00}");
            Console.WriteLine($"{label}: raw signed deltas [{string.Join(", ", deltas.Select(d => d.ToString("0.00")))}]");
        }

        // Mean, sample std dev, min, max.
        private static (double mean, double stdDev, double min, double max) ComputeStats(IEnumerable<float> samples)
        {
            var values = samples.Select(s => (double)s).ToList();
            double mean = values.Average();
            double variance = values.Count > 1
                ? values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1)
                : 0;
            return (mean, Math.Sqrt(variance), values.Min(), values.Max());
        }
    }
}
