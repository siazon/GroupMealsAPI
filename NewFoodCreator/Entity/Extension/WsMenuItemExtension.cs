using Takeaway.Service.Contract.Entities.Shop;

namespace Takeaway.Service.Contract.Extension
{
    public static class WsMenuItemExtension
    {

        public static string SubItemWithComment(this WsMenuItem source)
        {


            var builder = new System.Text.StringBuilder();
            foreach (var subOrder in source.SubItemSelections)
            {
                builder.AppendLine(string.Format("+ {0}", subOrder.Selected.Name));
            }

            return builder.ToString();

        }

    }
}