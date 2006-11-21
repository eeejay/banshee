using System;
using System.Collections.Generic;

using NUnit.Framework;

using Banshee.Base;

namespace Banshee.Base.Tests
{
    [TestFixture]
    public class GenericCollectionControllerTest : GenericCollectionController<GenericCollectionControllerTest.TestTrack>
    {
        private class TestTrack
        {
            public int Value;
            
            public TestTrack(int value)
            {
                Value = value;
            }
        }
        
        List<TestTrack> source = new List<TestTrack>();
    
        [TestFixtureSetUp]
        public void Init()
        {
            for(int i = 0; i < 10; i++) {
                source.Add(new TestTrack(i));
            }
            
            Data = source;
        }
        
        [Test]
        public void AdvanceTest()
        {
            Reset();
            
            for(int i = 0; i < Data.Count; i++) {
                Assert.AreEqual(i, Advance().Value);
            }
        }
        
        [Test]
        public void RegressTest()
        {
            Reset();
            
            Advance();
            Advance();
            Advance();
            
            Assert.AreEqual(3, index);
            
            Assert.AreEqual(1, Regress().Value);
            Assert.AreEqual(3, index);
            
            Assert.AreEqual(0, Regress().Value);
            Assert.AreEqual(3, index);
            
            Assert.AreEqual(1, Advance().Value);
            Assert.AreEqual(3, index);
            
            Assert.AreEqual(2, Advance().Value);
            Assert.AreEqual(3, index);
            
            Assert.AreEqual(3, Advance().Value);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(2, Regress().Value);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(1, Regress().Value);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(0, Regress().Value);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(0, Regress().Value);
            Assert.AreEqual(4, index);
        }
        
        /*[Test]
        public void ComplexTest()
        {
            Reset();
            
            Assert.AreEqual(0, Advance().Value);
            Assert.AreEqual(1, Advance().Value);
            Assert.AreEqual(2, Advance().Value);
            Assert.AreEqual(3, Advance().Value);
            
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(2, Regress().Value);
            Assert.AreEqual(1, Regress().Value);
            Assert.AreEqual(0, Regress().Value);
            
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(1, Advance().Value);
            Assert.AreEqual(2, Advance().Value);
            Assert.AreEqual(3, Advance().Value);
            
            //Assert.AreEqual(2, past_stack.Peek());
            //Assert.AreEqual(4, past_stack.Count);
            Assert.AreEqual(0, future_stack.Count);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(2, Regress().Value);
            Assert.AreEqual(1, Regress().Value);
            
            Assert.AreEqual(2, past_stack.Count);
            Assert.AreEqual(2, future_stack.Count);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(2, Advance().Value);
            
            Assert.AreEqual(3, past_stack.Count);
            Assert.AreEqual(1, future_stack.Count);
            Assert.AreEqual(4, index);
            
            Assert.AreEqual(3, Advance().Value);
            Assert.AreEqual(4, Advance().Value);
            Assert.AreEqual(0, future_stack.Count);
            Assert.AreEqual(5, index);
            
            Assert.AreEqual(3, Regress().Value);
            Assert.AreEqual(1, future_stack.Count);
            Assert.AreEqual(4, future_stack.Peek().Value);
            Assert.AreEqual(2, Regress().Value);
        }*/
    }
}
