using System;

namespace BookHelper
{
    class Program
    {
        static void Main()
        {
            var book = new Book(10);
            book.AddRange(3, 4);
            book.AddRange(6, 8);

            var left = book.HowManyPagesLeft();
            Console.WriteLine("Left: " + left);
            Console.ReadLine();
        }
    }
}
