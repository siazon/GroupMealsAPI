using System;
using CommandLine;

namespace NewShopCreator.Entity
{
    public class Options
    {

        [Option('s', "shopid", Required = true, HelpText = "Set shopid for processing.")]
        public int ShopId { get; set; }
        
        [Option('c', "country", Required = true, HelpText = "Set country for processing.")]
        public string Country { get; set; }
    }
}
