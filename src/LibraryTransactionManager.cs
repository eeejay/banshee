/***************************************************************************
 *  LibraryTransactionManager.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
 
namespace Sonance 
{
	public class LibraryTransactionManager
	{
		private Hashtable transactionTable;
		private ArrayList executionStack;
		private Type cancelType;
		private bool cancelAll;
		
		public event EventHandler ExecutionStackChanged;
		public event EventHandler ExecutionStackEmpty;
		
		public LibraryTransactionManager()
		{
			transactionTable = new Hashtable();
			executionStack = new ArrayList();
			cancelAll = false;
		}
		
		public LibraryTransaction TopExecution
		{
			get {
				if(executionStack.Count == 0)
					return null;
				
				// return top most visible transaction
				foreach(LibraryTransaction vtop in executionStack) {
					if(vtop.ShowStatus) {
						return vtop;
					}
				}
				
				return null;
			}
		}
		
		public int TableCount
		{
			get {
				int count = 0;
				
				foreach(ArrayList queue in transactionTable.Values)
					count += CountVisibleInQueue(queue);
					
				return count;
			}
		}
		
		public int CountVisibleInQueue(ArrayList queue)
		{
			int count = 0;
			foreach(LibraryTransaction transaction in queue)
				if(transaction.ShowStatus)
					count++;
			return count;
		}
		
		public bool Register(LibraryTransaction transaction)
		{
			Type transactionType = transaction.GetType();
			
			if(cancelAll || transactionType == cancelType)
				return false;
		
			transaction.Finished += OnTransactionFinished;
			
			if(transactionTable[transactionType] == null)
				transactionTable[transactionType] = new ArrayList();
				
			ArrayList transactionQueue = 
				(ArrayList)transactionTable[transactionType];
			transactionQueue.Add(transaction);
			
			DebugLog.Add("Registered LibraryTransaction [" + 
				transactionType + "]");
			
			if(transactionQueue.Count == 1) 
				ExecuteNext(transactionType);
				
			return true;
		}
		
		public void Cancel(Type cancelType)
		{
			this.cancelType = cancelType;
			Cancel((ArrayList)transactionTable[cancelType]);
			this.cancelType = null;
		}
		
		public void CancelAll()
		{
			cancelAll = true;
			foreach(ArrayList transactionQueue in transactionTable.Values)
				Cancel(transactionQueue);
			cancelAll = false;
		}
		
		private void Cancel(ArrayList transactionQueue)
		{
			if(transactionQueue == null)
				return;
			
			for(int i = 0, n = transactionQueue.Count; i < n; i++) {
				LibraryTransaction transaction = 
					(LibraryTransaction)transactionQueue[i];	
				transaction.Cancel();
				transaction = null;
			}
			
			transactionQueue.Clear();
		}

		private void ExecuteNext(Type transactionType)
		{
			if(cancelAll || transactionType == cancelType)
				return;

			ArrayList transactionQueue = 
				(ArrayList)transactionTable[transactionType];
		
			if(transactionQueue == null)
				return;
		
			if(transactionQueue.Count > 0) {
				DebugLog.Add("Executing next LibraryTransaction [" + 
					transactionType + "]");
				
				LibraryTransaction transaction = 
					(LibraryTransaction)transactionQueue[0];
				
				if(!transaction.ThreadedRun())
					ExecuteNext(transactionType);
				else 
					ExecutionStackPush(transaction);
			}
		}
		
		private void ExecutionStackPush(LibraryTransaction transaction)
		{
			executionStack.Insert(0, transaction);
			
			EventHandler handler = ExecutionStackChanged;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		private void ExecutionStackRemove(LibraryTransaction transaction)
		{
			executionStack.Remove(transaction);
			
			if(executionStack.Count == 0) {
				EventHandler handler = ExecutionStackEmpty;
				if(handler != null)
					handler(this, new EventArgs());
			}
		}
		
		private void OnTransactionFinished(object o, EventArgs args)
		{
			Type transactionType = o.GetType();
			
			ArrayList transactionQueue = 
				(ArrayList)transactionTable[transactionType];
			
			if(transactionQueue != null && transactionQueue.Count > 0 
				&& !cancelAll && cancelType != transactionType)
				transactionQueue.Remove(o);

			ExecutionStackRemove((LibraryTransaction)o);
			
			ExecuteNext(o.GetType());
		}
	}
}
