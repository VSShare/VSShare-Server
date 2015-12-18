using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class PagingItemViewModel<T>
    {
        public List<T> Results { get; set; }

        public int StartPage { get; set; }

        public int EndPage { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public string Query { get; set; }

        public SortType SortType { get; set; }

        public bool IsLive { get; set; }

    }

    public enum SortType
    {
        Created = 0,
        Visitor = 1
    }

}
