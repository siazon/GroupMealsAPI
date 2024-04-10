using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using App.Domain.Common.Shop;
using KingfoodIO.Application.Filter;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UtilController : BaseController
    {


        IMemoryCache _memoryCache;
        private readonly AzureStorageConfig storageConfig;
        ILogManager logger;
        public UtilController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache,
            IOptions<AzureStorageConfig> _storageConfig, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache = memoryCache;
            storageConfig = _storageConfig.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="files"></param>
        /// <param name="folder">餐厅英文名，选填, 传空时,图片传到根目录(避免文件路径冲突字符)</param>
        /// <returns></returns>
        [ServiceFilter(typeof(AuthActionFilter))]
        [HttpPost]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UploadImage([FromForm] IFormCollection files,string folder)
        {
            bool isUploaded = false;
            try
            {
                if (files.Files.Count == 0)
                    return BadRequest("No files received from the upload");

                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                    return BadRequest("sorry, can't retrieve your azure storage details from appsettings.js, make sure that you add azure storage details there");

                if (storageConfig.ImageContainer == string.Empty)
                    return BadRequest("Please provide a name for your image container in the azure blob storage");
                string fileName = "";
                foreach (var formFile in files.Files)
                {
                    if (ImageUploader.IsImage(formFile))
                    {
                        if (formFile.Length > 0)
                        {
                            if (string.IsNullOrWhiteSpace(folder))
                                fileName = formFile.FileName;
                            else
                                fileName = folder + "/" + formFile.FileName;
                            using (Stream stream = formFile.OpenReadStream())
                            {
                                isUploaded = await ImageUploader.UploadFileToStorage(stream, fileName, storageConfig);
                            }
                        }
                    }
                    else
                    {
                        return new UnsupportedMediaTypeResult();
                    }
                }
                if (isUploaded)
                {
                    //if (storageConfig.ThumbnailContainer != string.Empty)
                    //    return new AcceptedAtActionResult("GetThumbNails", "Images", null, null);
                    //else
                    var res =  ImageUploader.GetImageUrl(storageConfig)+ fileName;
                    return Json(new { uri = res });
                }
                else
                    return BadRequest("Look like the image couldnt upload to the storage");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetThumbNails()
        {
            try
            {
                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                    return BadRequest("Sorry, can't retrieve your Azure storage details from appsettings.js, make sure that you add Azure storage details there.");

                if (storageConfig.ImageContainer == string.Empty)
                    return BadRequest("Please provide a name for your image container in Azure blob storage.");

                List<string> thumbnailUrls = await ImageUploader.GetThumbNailUrls(storageConfig);
                return new ObjectResult(thumbnailUrls);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
