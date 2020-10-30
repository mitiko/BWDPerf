using System;
using System.Diagnostics;

namespace BWDPerf.Tools
{
    public class LinearProgressBar
    {
        double Total { get; }
        double OldPercentage { get; set; }
        double Progress { get; set; }
        Stopwatch Timer { get; set; }

        // Console coordinates
        private int Left { get; set; }
        private int Top { get; set; }

        public LinearProgressBar(double total)
        {
            this.Total = total;
            Console.Write("Progress: 0%");
            (this.Left, this.Top) = Console.GetCursorPosition();
            this.Left -= 2; // Override "0%" when printing
            Timer = Stopwatch.StartNew();
        }

        public void UpdateProgress(double progressChange) =>
            this.Progress += progressChange;

        private double GetPercentage() =>
            Math.Floor(this.Progress / this.Total * 100);

        public void Print()
        {
            if (this.OldPercentage == 100)
                return;
            var percentage = GetPercentage();
            if (this.OldPercentage != percentage)
            {
                var remainingTime = (100 - percentage) / (percentage - this.OldPercentage) * Timer.Elapsed;
                Console.SetCursorPosition(this.Left, this.Top);
                Console.Write($"{percentage}%\tTime remaining: {remainingTime}");

                this.OldPercentage = percentage;
                Timer.Restart();
            }
            if (percentage == 100)
            {
                Console.WriteLine(); // Go to a new line
                Timer = null; // Remove the timer instance
            }
        }
    }
}