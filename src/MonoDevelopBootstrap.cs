using System;
using System.Diagnostics;

public class MonoDevelopBootstrap
{
    public static void Main()
    {
        Process process = Process.Start("make", "run-nocheck");
        process.WaitForExit();
    }
}
