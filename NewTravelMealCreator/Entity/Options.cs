using CommandLine;

namespace NewTravelMealCreator.Entity
{
    public class Options
    {

        [Option('s', "shopid", Required = true, HelpText = "Set shopid for processing.")]
        public int ShopId { get; set; }
    }
}
