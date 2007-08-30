using System;

public class Timer : IDisposable
{
    private DateTime start;
    private string label;
    
    public Timer(string label) 
    {
        this.label = label;
        start = DateTime.Now;
    }

    public TimeSpan ElapsedTime {
        get { return DateTime.Now - start; }
    }

    public void WriteElapsed(string message)
    {
        Console.WriteLine("{0} {1} {2}", label, message, ElapsedTime);
    }

    public void Dispose()
    {
        WriteElapsed("timer stopped:");
    }
}

public static class StringExtensions 
{
    public static string Flatten(string s)
    {
        return System.Text.RegularExpressions.Regex.Replace(s.Replace("\n", " "), "[ ]+", " ").Trim();
    }
}
