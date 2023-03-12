using System.Threading.Tasks;
using KingfoodIO.Application;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KingfoodIO.Pages
{
    public class UnSubModel : PageModel
    {
        public async Task OnGet()
        {
            string email = HttpContext.Request.Query["email"];
            if (string.IsNullOrEmpty(email))
                return;

            var url =
                $"https://apiie.kingfood.io/api/FMarketing/UnSubFoodMarketingEmail?shopId=1&email={email}";
            var response = await new RestClient().GetService(url);
        }
    }
}