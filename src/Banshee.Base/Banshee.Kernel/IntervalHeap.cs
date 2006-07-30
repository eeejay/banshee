/***************************************************************************
 *  IntervalHeap.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections;
using System.Collections.Generic;

namespace Banshee.Kernel
{
    public class IntervalHeap<T> : ICollection, IEnumerable<T>, IEnumerable
    {
        private int count;
        private int generation;
        
        private int capacity;
        private Interval [] heap;
        
        private object syncroot = new object();
        
        public IntervalHeap()
        {
            Clear();
        }
        
        public T Pop()
        {
            if(count == 0) {
                throw new InvalidOperationException();
            }
            
            T item = heap[0].Item;
            MoveDown(0, heap[--count]);
            generation++;
            return item;
        }
        
        public void Push(T item, int priority)
        {
            if(count == capacity) {
                capacity = capacity * 2 + 1;
                Interval [] grown_heap = new Interval[capacity];
                Array.Copy(heap, 0, grown_heap, 0, count);
                heap = grown_heap;
            }
            
            MoveUp(++count - 1, new Interval(item, priority));
            generation++;
        }
        
        public void Clear()
        {
            capacity = 15;
            generation = 0;
            heap = new Interval[capacity];
        }
        
        public void CopyTo(Array array, int index)
        {
            Array.Copy(heap, 0, array, index, count);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new IntervalHeapEnumerator(this);
        }
        
        private int GetLeftChildIndex(int index)
        {
            return index * 2 + 1;
        }
        
        private int GetParentIndex(int index)
        {
            return (index - 1) / 2;
        }
        
        private void MoveUp(int index, Interval node)
        {
            int parent_index = GetParentIndex(index);
            
            while(index > 0 && heap[parent_index].Priority < node.Priority) {
                heap[index] = heap[parent_index];
                index = parent_index;
                parent_index = GetParentIndex(index);
            }
            
            heap[index] = node;
        }
        
        private void MoveDown(int index, Interval node)
        {
            int child_index = GetLeftChildIndex(index);
            
            while(child_index < count) {
                if(child_index + 1 < count 
                    && heap[child_index].Priority < heap[child_index + 1].Priority) {
                    child_index++;
                }
                
                heap[index] = heap[child_index];
                index = child_index;
                child_index = GetLeftChildIndex(index);
            }
            
            MoveUp(index, node);
        }
        
        public int Count {
            get { return count; }
        }
        
        public object SyncRoot {
            get { return syncroot; }
        }
        
        public bool IsSynchronized {
            get { return false; }
        }
        
        private struct Interval
        {
            private T item;
            private int priority;
            
            public Interval(T item, int priority)
            {
                this.item = item;
                this.priority = priority;
            }
            
            public T Item {
                get { return item; }
            }
            
            public int Priority { 
                get { return priority; }
            }
        }
    
        private class IntervalHeapEnumerator : IEnumerator<T>, IEnumerator
        {
            private IntervalHeap<T> heap;
            private int index;
            private int generation;
            
            public IntervalHeapEnumerator(IntervalHeap<T> heap)
            {
                this.heap = heap;
                Reset();
            }
            
            public void Reset()
            {
                generation = heap.generation;
                index = -1;
            }
            
            public void Dispose()
            {
                heap = null;
            }
 
            public bool MoveNext()
            {
                if(generation != heap.generation) {
                    throw new InvalidOperationException();
                }
                
                if(index + 1 == heap.count) {
                    return false;
                }
                
                index++;
                return true;
            }
            
            object IEnumerator.Current {
                get { return Current; }
            }
 
            public T Current {
                get {
                    if(generation != heap.generation) {
                        throw new InvalidOperationException();
                    }
                    
                    return heap.heap[index].Item;
                }
            }
        }
    }
}
 