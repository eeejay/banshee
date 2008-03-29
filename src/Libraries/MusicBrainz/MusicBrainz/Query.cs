/***************************************************************************
 *  Query.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MusicBrainz
{
    public sealed class Query<T> : IEnumerable<T> where T : MusicBrainzObject
    {
        string parameters;
        string url_extension;
        byte limit;

        internal Query (string url_extension, byte limit, string parameters)
        {
            this.url_extension = url_extension;
            this.limit = limit;
            this.parameters = parameters;
        }
        
        List<T> results;
        List<T> ResultsWindow {
            get {
                if (results == null)
                    results = MusicBrainzObject.Query<T> (url_extension, limit, offset, parameters, out count);
                return results;
            }
        }

        int offset;
        Dictionary<int, WeakReference> weak_references = new Dictionary<int, WeakReference> ();
        int Offset {
            get { return offset; }
            set {
                if (value == offset) return;
                // We WeakReference the results from previous offsets just in case.
                if (results != null)
                    if (!weak_references.ContainsKey (offset))
                        weak_references.Add (offset, new WeakReference (results));
                    else weak_references [offset].Target = results;
                results = null;
                offset = value;
                if (weak_references.ContainsKey (offset)) {
                    WeakReference weak_reference = weak_references [offset];
                    if (weak_reference.IsAlive)
                        results = weak_reference.Target as List<T>;
                }
            }
        }

        int? count;
        public int Count {
            get {
                if(count == null && ResultsWindow == null) { } // just accessing ResultsWindow will give count a value
                return count.Value;
            }
        }

        public T this [int i] {
            get {
                if (i < 0 || i >= Count) throw new IndexOutOfRangeException ();
                if (i <= offset || i >= offset + limit) 
                    Offset = i;
                return ResultsWindow [i - offset];
            }
        }

        public List<T> ToList ()
        {
            return ToList (0);
        }
        
        public List<T> ToList (int score_threshold)
        {
            List<T> list = new List<T> (score_threshold == 0 ? Count : 0);
            foreach (T result in Best(score_threshold)) list.Add (result);
            return list;
        }
        
        public T [] ToArray ()
        {
            T [] array = new T [Count];
            for(int i = 0; i < Count; i++) array [i] = this [i];
            return array;
        }

        public IEnumerator<T> GetEnumerator ()
        {
            for (int i = 0; i < Count; i++) yield return this [i];
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
        
        public IEnumerable<T> Best ()
        {
            return Best (100);
        }
        
        public IEnumerable<T> Best (int score_threshold)
        {
            foreach (T result in this) {
                if (result.Score < score_threshold) yield break;
                yield return result;
            }
        }
        
        public T PerfectMatch ()
        {
            byte tmp_limit = limit;
            limit = 2;
            T result1 = Count > 0 ? this [0] : null;
            T result2 = Count > 1 ? this [1] : null;
            limit = tmp_limit;
            
            return (result1 != null && result1.Score == 100 && (result2 == null || result2.Score < 100))
                ? result1 : null;
        }
        
        public T First ()
        {
            byte tmp_limit = limit;
            limit = 1;
            T result = Count > 0 ? this [0] : null;
            limit = tmp_limit;
            return result;
        }
        
        public static implicit operator T (Query<T> query)
        {
            return query.First ();
        }
    }

    [AttributeUsage (AttributeTargets.Property)]
    internal sealed class QueryableAttribute : Attribute
    {
        public readonly string Name;
        
        public QueryableAttribute ()
        {
        }
        
        public QueryableAttribute (string name)
        {
            Name = name;
        }
    }

    [AttributeUsage (AttributeTargets.Property)]
    internal sealed class QueryableMemberAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Member;
        public QueryableMemberAttribute (string member, string name)
        {
            Member = member;
            Name = name;
        }
    }
}
