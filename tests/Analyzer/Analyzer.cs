//
// Analyzer.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;

using Hyena;

namespace Banshee.Tests
{
    public class Analyzer
    {
        public static void Main (string [] args)
        {
            if (args == null || args.Length == 0) {
                Console.Error.WriteLine ("compare-perf-results [OVERALL-RESULTS-DIR | INDIVIDUAL-RESULTS-DIR1 ...]");
                return;
            }

            if (args.Length == 1)
                new Analyzer (args[0]);
            else
                new Analyzer (args);
        }

        private Analyzer (string results_dir) : this (System.IO.Directory.GetDirectories (results_dir))
        {
        }

        private Analyzer (string [] results_dirs)
        {
            // Get the averaged tests for each directory
            var all_tests = results_dirs.SelectMany<string, TestCase> (AveragedTests);

            Console.Write ("{0,-42} ", ""); 
            Console.WriteLine ("min   avg   max");

            // Group by the test suite
            var grouped_tests = all_tests.GroupBy (t => t.GroupName);
            foreach (var group in grouped_tests) {

                // Group by test name and calculate some stats
                var unique_tests = group.GroupBy (t => t.Name);
                //ConsoleCrayon.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine ("{0}", group.Key);
                //ConsoleCrayon.ResetColor ();

                foreach (var tests in unique_tests) {
                    Console.Write ("    {0,-36}", tests.Key);
                    Console.WriteLine ();

                    bool first = true;
                    double first_avg = 0;
                    foreach (var test in tests.OrderBy (t => t.RunTime)) {
                        if (first) {
                            first_avg = test.Avg;
                            first = false;
                        }

                        Console.Write ("      {0,-36}", test.RunId);
                        Console.Write (" {0,3:#00}   {1,3:#00}   {2,3:#00}", 100*test.Min/first_avg, 100*test.Avg/first_avg, 100*test.Max/first_avg);
                        Console.Write ("     ({0,5:##0.00}   {1,5:##0.00}   {2,5:##0.00})", test.Min, test.Avg, test.Max);
                        Console.WriteLine ();

                        first = false;
                    }

                    Console.WriteLine ();
                }

                Console.WriteLine ();
            }
        }

        private IEnumerable<TestCase> AveragedTests (string dir)
        {
            // Load all the test results in this directory
            var tests = System.IO.Directory.GetFiles (dir)
                            .Where (f => f.EndsWith (".xml"))
                            .Select (f => XDocument.Load (f))
                            .SelectMany<XDocument, TestCase> (TestCase.For)
                            .ToArray ();

            string [] run_info = File.ReadAllLines (dir + Path.DirectorySeparatorChar + "run-info");
            string run_id = run_info[0];
            string run_time = run_info[1];

            // Return the avg runtime of each test
            var grouped_tests = tests.GroupBy (t => t.Name);
            foreach (var group in grouped_tests) {
                var group_tests = group.ToList ();
                if (group_tests.Any (t => t.Success)) {
                    var min = group_tests.Where (t => t.Success).Min (t => t.Time);
                    var max = group_tests.Where (t => t.Success).Max (t => t.Time);
                    var avg = group_tests.Where (t => t.Success).Average (t => t.Time);

                    var test = group_tests.Find (t => t.Success);
                    test.Min = min;
                    test.Max = max;
                    test.Avg = avg;
                    test.RunId = run_id;
                    test.RunTime = run_time;
                    yield return test;
                }
            }
        }
    }

    public class TestCase {
        public string FullName { get; set; }
        public string RunId { get; set; }
        public string RunTime { get; set; }
        public double Time { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg { get; set; }
        public bool Success { get; set; }
        public bool Executed { get; set; }
        public int Asserts { get; set; }

        public string Name {
            get { return FullName.Substring (FullName.LastIndexOf (".") + 1); }
        }

        public string GroupName {
            get { return FullName.Substring (0, FullName.LastIndexOf (".")); }
        }

        public static IEnumerable<TestCase> For (XDocument doc)
        {
            return doc.Descendants ()
               .Where (d => d.Name == "test-case")
               .Select (t => new TestCase {
                    FullName = t.Attribute ("name").Value, 
                    Time = Convert.ToDouble (t.Attribute ("time").Value),
                    Success = Convert.ToBoolean (t.Attribute ("success").Value),
                    Executed = Convert.ToBoolean (t.Attribute ("executed").Value),
                    Asserts = Convert.ToInt32 (t.Attribute ("asserts").Value)
                });
        }
    }
}
