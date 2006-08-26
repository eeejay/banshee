using System;
using NUnit.Framework;

using Banshee.Kernel;

namespace Banshee.Kernel.Tests
{
	[TestFixture]
	public class IntervalHeapTest
	{
		private IntervalHeap<string> heap;
		private static string [] heap_data = new string [] { 
			"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"
		};
		
		[TestFixtureSetUp]
		public void Init()
		{
			heap = new IntervalHeap<string>();
		}

		private void PopulateHeap()
		{
			heap.Clear();
			
			foreach(string s in heap_data) {
				heap.Push(s, 0);
			}
			
			Assert.AreEqual(heap.Count, heap_data.Length);
		}

		[Test]
		public void PopHeap()
		{
			int i = 0;
			while(heap.Count > 0) {
				string a = heap.Pop(), b = heap_data[i++];
				Assert.AreEqual(a, b);
			}
		}
	}
}

