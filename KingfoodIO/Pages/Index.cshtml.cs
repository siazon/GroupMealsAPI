using KingfoodIO.Application.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace KingfoodIO.Pages
{
    public class IndexModel : PageModel
    {

        private IConfiguration  ConfigRoot;
        private IAppVersionService _appVersionService;
        public string Site { get; private set; } = "";
        public string Version { get; private set; } = "";


        public IndexModel(IConfiguration configRoot, IAppVersionService appVersionService)
        {
            ConfigRoot = configRoot;
            _appVersionService = appVersionService;
        }

        public void OnGet()
        {
            Site = ConfigRoot["AppSetting:ShopUrl"];
            Version = _appVersionService.Version;
        }

    }
}
