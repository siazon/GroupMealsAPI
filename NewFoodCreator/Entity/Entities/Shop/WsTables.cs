using System;
using System.Collections.Generic;

namespace Takeaway.Service.Contract.Entities.Shop
{
    public class WsTables:WsEntity
    {
        
        public string Name { get; set; }

        public string Alias { get; set; }

        public int NumberOfPeople { get; set; }

        public DateTime? Started { get; set; }

        public WsOrder Order { get; set; }
       
    }
}