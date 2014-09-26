using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGiThuBe
{
    class Program
    {
        private static readonly int[] Data = { 3, 4, 34, 22, 34, 3, 4, 4, 2, 7, 3, 8, 122, -1, -3, 1, 2 };

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
        }

     

        static void Main(string[] args)
        {
             // 1. partition -->




       //2. for each partition find the smallest element and add to ConcurrentBag
       //3. find the smallest element in the ConcurrentBag
        }
    }
}
