using System.Text;
using CosmosPatch.Domain.Interfaces;

namespace CosmosPatch.Console.Progress;

/// <summary>
/// Colorful console progress bar using Unicode block character █.
/// Displays percentage, elapsed time, and estimated remaining time.
/// </summary>
public sealed class ConsoleProgressBar : IProgressReporter
{
    private static readonly Dictionary<int, ConsoleColor> Colors =
        Enum.GetValues<ConsoleColor>().ToDictionary(c => (int)c, c => c);

    public int Max { get; set; }
    public int Value { get; set; }
    public int ColorIndex { get; set; }

    private readonly DateTime _startTime = DateTime.Now;
    private const int BarWidth = 50;

    public ConsoleProgressBar(int max, ConsoleColor color = ConsoleColor.Green)
    {
        Max = max;
        Value = 0;
        ColorIndex = (int)color;
    }

    public void Tick(int value = 1)
    {
        Value += value;
        Render();
    }

    private void Render()
    {
        if (Max <= 0) return;

        int percent = Math.Min(100, (int)((float)Value / Max * 100));
        int filled = (int)(percent / 2f);

        System.Console.Write("\r[");
        for (int i = 0; i < BarWidth; i++)
        {
            if (i < filled)
            {
                System.Console.ForegroundColor = Colors[ColorIndex % Colors.Count];
                System.Console.Write('█');
            }
            else if (i == filled)
            {
                System.Console.Write('>');
            }
            else
            {
                System.Console.Write(' ');
            }
        }

        TimeSpan elapsed = DateTime.Now - _startTime;
        int totalSeconds = percent > 0 ? (int)(elapsed.TotalSeconds / ((float)percent / 100)) : 0;
        int remainingSeconds = Math.Max(0, totalSeconds - (int)elapsed.TotalSeconds);

        System.Console.ForegroundColor = Colors[ColorIndex % Colors.Count];
        System.Console.Write($"] {percent,3}%  Elapsed: {elapsed:mm\\:ss}  Remaining: {remainingSeconds / 60}:{remainingSeconds % 60:D2}");
        System.Console.ResetColor();
    }

    public void Dispose()
    {
        System.Console.WriteLine();
        GC.SuppressFinalize(this);
    }
}
