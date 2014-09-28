using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestGiThuBe
{

    // Simple partitioner that will extract one item at a time, in a thread-safe fashion, 
    // from the underlying collection. 
    class SingleElementPartitioner<T> : Partitioner<T>
    {
        // The collection being wrapped by this Partitioner
        IEnumerable<T> m_referenceEnumerable;

        // Internal class that serves as a shared enumerable for the 
        // underlying collection. 
        private class InternalEnumerable : IEnumerable<T>, IDisposable
        {
            IEnumerator<T> m_reader;
            bool m_disposed = false;

            // These two are used to implement Dispose() when static partitioning is being performed 
            int m_activeEnumerators;
            bool m_downcountEnumerators;

            // "downcountEnumerators" will be true for static partitioning, false for 
            // dynamic partitioning.   
            public InternalEnumerable(IEnumerator<T> reader, bool downcountEnumerators)
            {
                m_reader = reader;
                m_activeEnumerators = 0;
                m_downcountEnumerators = downcountEnumerators;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (m_disposed)
                    throw new ObjectDisposedException("InternalEnumerable: Can't call GetEnumerator() after disposing");

                // For static partitioning, keep track of the number of active enumerators. 
                if (m_downcountEnumerators) Interlocked.Increment(ref m_activeEnumerators);

                return new InternalEnumerator(m_reader, this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>)this).GetEnumerator();
            }

            public void Dispose()
            {
                if (!m_disposed)
                {
                    // Only dispose the source enumerator if you are doing dynamic partitioning 
                    if (!m_downcountEnumerators)
                    {
                        m_reader.Dispose();
                    }
                    m_disposed = true;
                }
            }

            // Called from Dispose() method of spawned InternalEnumerator.  During 
            // static partitioning, the source enumerator will be automatically 
            // disposed once all requested InternalEnumerators have been disposed. 
            public void DisposeEnumerator()
            {
                if (m_downcountEnumerators)
                {
                    if (Interlocked.Decrement(ref m_activeEnumerators) == 0)
                    {
                        m_reader.Dispose();
                    }
                }
            }
        }

        // Internal class that serves as a shared enumerator for  
        // the underlying collection. 
        private class InternalEnumerator : IEnumerator<T>
        {
            T m_current;
            IEnumerator<T> m_source;
            InternalEnumerable m_controllingEnumerable;
            bool m_disposed = false;

            public InternalEnumerator(IEnumerator<T> source, InternalEnumerable controllingEnumerable)
            {
                m_source = source;
                m_current = default(T);
                m_controllingEnumerable = controllingEnumerable;
            }

            object IEnumerator.Current
            {
                get { return m_current; }
            }

            T IEnumerator<T>.Current
            {
                get { return m_current; }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException("Reset() not supported");
            }

            // This method is the crux of this class.  Under lock, it calls 
            // MoveNext() on the underlying enumerator and grabs Current. 
            bool IEnumerator.MoveNext()
            {
                bool rval = false;
                lock (m_source)
                {
                    rval = m_source.MoveNext();
                    m_current = rval ? m_source.Current : default(T);
                }
                return rval;
            }

            void IDisposable.Dispose()
            {
                if (!m_disposed)
                {
                    // Delegate to parent enumerable's DisposeEnumerator() method
                    m_controllingEnumerable.DisposeEnumerator();
                    m_disposed = true;
                }
            }

        }

        // Constructor just grabs the collection to wrap 
        public SingleElementPartitioner(IEnumerable<T> enumerable)
        {
            // Verify that the source IEnumerable is not null 
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");

            m_referenceEnumerable = enumerable;
        }

        // Produces a list of "numPartitions" IEnumerators that can each be
        // used to traverse the underlying collection in a thread-safe manner. 
        // This will return a static number of enumerators, as opposed to 
        // GetDynamicPartitions(), the result of which can be used to produce 
        // any number of enumerators. 
        public override IList<IEnumerator<T>> GetPartitions(int numPartitions)
        {
            if (numPartitions < 1)
                throw new ArgumentOutOfRangeException("NumPartitions");

            List<IEnumerator<T>> list = new List<IEnumerator<T>>(numPartitions);

            // Since we are doing static partitioning, create an InternalEnumerable with reference 
            // counting of spawned InternalEnumerators turned on.  Once all of the spawned enumerators 
            // are disposed, dynamicPartitions will be disposed. 
            var dynamicPartitions = new InternalEnumerable(m_referenceEnumerable.GetEnumerator(), true);
            for (int i = 0; i < numPartitions; i++)
                list.Add(dynamicPartitions.GetEnumerator());

            return list;
        }

        // Returns an instance of our internal Enumerable class.  GetEnumerator() 
        // can then be called on that (multiple times) to produce shared enumerators. 
        public override IEnumerable<T> GetDynamicPartitions()
        {
            // Since we are doing dynamic partitioning, create an InternalEnumerable with reference 
            // counting of spawned InternalEnumerators turned off.  This returned InternalEnumerable 
            // will need to be explicitly disposed. 
            return new InternalEnumerable(m_referenceEnumerable.GetEnumerator(), false);
        }

        // Must be set to true if GetDynamicPartitions() is supported. 
        public override bool SupportsDynamicPartitions
        {
            get { return true; }
        }
    }
  

    class Program
    {
        
        private static int[] Data = { 3, 4, 34, 22, 34, 3, 4, 4, 2, 7, 3, 8, 122, -1, -3, 1, 2 };

        private static List<int> _list = new List<int>(Data);


        private const int testen = 80000; 
        private Partitioner<int> p;

     

        private static int FindSmallest(int numbers)
        {
            if (numbers == 0)
            {
                throw new ArgumentException("There must be at least one element in the array");
            }
            int smallestSoFar = numbers;
            for (int number = 0; number < numbers; number++)
            {
                if (number < smallestSoFar)
                {
                    smallestSoFar = number;
                }
            }
           
            //Console.WriteLine(String.Join("; ", numbers) + ": " + smallestSoFar);
            return smallestSoFar;
            List<int> list = Data.ToList();
            
        }

     

        static void Main(string[] args)
        {

            var myPart = new SingleElementPartitioner<int>(_list);
                      ConcurrentBag<int> cbag = new ConcurrentBag<int>();
            


            Console.WriteLine("Testing with Parallel.ForEach");
            Parallel.ForEach(myPart, item =>
            {
                Console.WriteLine("  item = {0}, thread id = {1}", item, Thread.CurrentThread.ManagedThreadId);
                if (Thread.CurrentThread.ManagedThreadId == 0)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 1)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 2)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 3)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 4)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 5)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 6)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 7)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 8)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 9)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
                if (Thread.CurrentThread.ManagedThreadId == 10)
                {
                    int smallest = FindSmallest(item);
                    cbag.Add(smallest);
                }
            });
            Console.WriteLine(cbag.Count);
            


            //Thread.CurrentThread.ManagedThreadId




            ////var list = new List<int>(Data);
            // // 1. partition -->



            //int smallest = FindSmallest(_list);
            ////<--



            //Console.WriteLine(smallest);




            //foreach (var num in _list)
            //{
            //    Console.WriteLine(num);
            //}

            //ConcurrentBag<int> cbag = new ConcurrentBag<int>();
            //cbag.Add(smallest);
            //int value;
            //cbag.TryPeek(out value); //Check value
            //Console.WriteLine("TryPeek: {0}", value); //Viser det sidste der blev puttet i.
            //Console.WriteLine("Counter: {0}", cbag.Count); //Viser antal elementer i bag.
            // Console.WriteLine(_list);



            //2. for each partition find the smallest element and add to ConcurrentBag
            //int smallest = FindSmallest(_list);
            //ConcurrentBag<int> cbag = new ConcurrentBag<int>();
            //cbag.Add(smallest);
            //int value;
            //cbag.TryPeek(out value); //Check value
            //Console.WriteLine("TryPeek: {0}", value); //Viser det sidste der blev puttet i.
            //Console.WriteLine("Counter: {0}", cbag.Count); //Viser antal elementer i bag.
            //SingleElementPartitioner<int> myPart = new SingleElementPartitioner<int>(_list);

            //Console.WriteLine("Testing with Parallel.ForEach");

            //Parallel.ForEach(myPart, item =>
            //{
            //    Console.WriteLine("  item = {0}, thread id = {1}", item, Thread.CurrentThread.ManagedThreadId);

            //});

            //Console.WriteLine("Finding smallest: ");
            //Console.WriteLine("Adding smallest to ConcurrentBag");
            //Parallel.ForEach(myPart, item =>
            //{

            //});

            //Parallel.ForEach(
            //    Partitioner.Create(
            //        1,
            //        testen,
            //        ((int) (testen/Environment.ProcessorCount) + 1)),
            //    range =>
            //    {

            //        var dat = new List<int>(Data);
            //        Console.WriteLine("Test {0} og {1}", range.Item1, range.Item2);
            //        for (int i = range.Item1; i < range.Item2; i++)
            //        {
            //            dat.Add(i);
            //        }
            //    });

            //Console.WriteLine("Finding Smallest");



            // Viser det sidste der blev puttet i.

            //3. find the smallest element in the ConcurrentBag
        }
    }
}
