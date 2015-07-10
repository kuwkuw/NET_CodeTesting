using System;

namespace BookHelper
{
    internal struct PagesRange
    {
        public readonly int From;
        public readonly int To;

        public PagesRange(int from, int to)
        {
            if (from < 0 || from > to)
                throw new ArgumentException("Page number can't be negative.", "from");

            if (to < 0)
                throw new ArgumentException("Page number can't be negative.", "to");

            From = from;
            To = to;
        }
    }
}