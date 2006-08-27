using System;
using NUnit.Framework;

using Banshee.Kernel;

namespace Banshee.Kernel.Tests
{
	[TestFixture]
	public class IntervalHeapTest
	{
		private IntervalHeap<int> heap;
		private static int [] heap_data = new int[2048];
		
		[TestFixtureSetUp]
		public void Init()
		{
			heap = new IntervalHeap<int>();
			for(int i = 0; i < heap_data.Length; i++) {
				heap_data[i] = i;
			}
		}

		private void PopulateHeap()
		{
			heap.Clear();
			
			foreach(int i in heap_data) {
				heap.Push(i, 0);
			}
			
			Assert.AreEqual(heap.Count, heap_data.Length);
		}

		[Test]
		public void PopHeap()
		{
			PopulateHeap();
			
			int i = 0;
			while(heap.Count > 0) {
				heap.Pop();
				i++;
			}

			Assert.AreEqual(i, heap_data.Length);
		}

		[Test]
		public void IterateHeap()
		{
			PopulateHeap();

			int i = 0;
			foreach(int x in heap) {
				Assert.AreEqual(x, heap_data[i++]);
			}

			Assert.AreEqual(i, heap.Count);
		}

		[Test]
		public void RemoveItemsFromHeap()
		{
			IntervalHeap<int> h = new IntervalHeap<int>();
			for(int i = 0; i < 20; i++) {
				h.Push(i, i);
			}

			h.Remove(10);
			h.Remove(2);
			h.Remove(11);
			h.Remove(9);
			h.Remove(19);
			h.Remove(0);

			Assert.AreEqual(h.Pop(), 18);
			Assert.AreEqual(h.Pop(), 17);
			Assert.AreEqual(h.Pop(), 16);
			Assert.AreEqual(h.Pop(), 15);
			Assert.AreEqual(h.Pop(), 14);
			Assert.AreEqual(h.Pop(), 13);
			Assert.AreEqual(h.Pop(), 12);
			Assert.AreEqual(h.Pop(), 8);
			Assert.AreEqual(h.Pop(), 7);
			Assert.AreEqual(h.Pop(), 6);
			Assert.AreEqual(h.Pop(), 5);
			Assert.AreEqual(h.Pop(), 4);
			Assert.AreEqual(h.Pop(), 3);
			Assert.AreEqual(h.Pop(), 1);

			Assert.AreEqual(h.Count, 0);		
		}
	}
}

