using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGiThuBe
{
    class Program
    {
        private static int[] Data = { 3, 4, 34, 22, 34, 3, 4, 4, 2, 7, 3, 8, 122, -1, -3, 1, 2 };

        private static List<int> _list = new List<int>(Data);
 
        

        private static int FindSmallest(IList<int> numbers)
        {
            if (numbers.Count == 0)
            {
                throw new ArgumentException("There must be at least one element in the array");
            }
            int smallestSoFar = numbers[0];
            foreach (int number in numbers)
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
            //var list = new List<int>(Data);
             // 1. partition -->
            int smallest = FindSmallest(_list);
            //<--
            Console.WriteLine(smallest);
            foreach (var num in _list)
            {
                Console.WriteLine(num);
            }

            ConcurrentBag<int> cbag = new ConcurrentBag<int>(_list);
            int value;
            cbag.TryPeek(out value); //Check value
            Console.WriteLine("TryPeek: {0}", value); //Viser det sidste der blev puttet i.
            Console.WriteLine("Counter: {0}", cbag.Count); //Viser antal elementer i bag.
            // Console.WriteLine(_list);



            //2. for each partition find the smallest element and add to ConcurrentBag
            //3. find the smallest element in the ConcurrentBag
        }
    }
}
