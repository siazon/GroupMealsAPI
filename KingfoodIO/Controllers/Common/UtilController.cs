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
using Microsoft.AspNetCore.WebUtilities;
using Twilio.TwiML.Voice;
using Stream = System.IO.Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
        public async Task<IActionResult> UploadImage([FromForm] IFormCollection files, string folder)
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

                                using (var myImage = await Image.LoadAsync(stream))
                                {
                                    if (fileName.ToLower().IndexOf("main") > 0)
                                        myImage.Mutate(x => x.Resize(173, 130));
                                    using (var outStream = new MemoryStream())
                                    {
                                        await myImage.SaveAsync(outStream, new WebpEncoder());
                                        fileName = fileName.Substring(0, fileName.LastIndexOf(".")) + ".webp";
                                        outStream.Position = 0;
                                        isUploaded = await ImageUploader.UploadFileToStorage(outStream, fileName, storageConfig);
                                    }
                                }

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
                    var res = ImageUploader.GetImageUrl(storageConfig) + fileName;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [ServiceFilter(typeof(AuthActionFilter))]
        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteImage(string fileName)
        {
            await ImageUploader.DeleteImage(fileName, storageConfig);
            return Json(new { msg = "ok" });
        }


        private async Task<Stream> WebpConverter(Stream inStream)
        {

            //var imageBytes = await File.ReadAllBytesAsync("your-image.jpg");

            //using var inStream = new MemoryStream(imageBytes);

            using var myImage = await Image.LoadAsync(inStream);

            using (var outStream = new MemoryStream())
            {
                await myImage.SaveAsync(outStream, new WebpEncoder());
            }


            return inStream;
            //return new FileContentResult(outStream.ToArray(), "image/webp");
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
