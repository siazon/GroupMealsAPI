using App.Domain.Common.Setting;
using App.Infrastructure.Utility.Common;
using Microsoft.Azure.Documents;
using RazorLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Infrastructure.Builders.Common
{
    public interface IContentBuilder
    {
        Task<string> BuildRazorContent<T>(T objectContent, string contentTemplate);
        Task<bool> SendEmail<T>(T objectContent, string contentTemplate,List<DbSetting> settings, string fromEmail, string toEmail, string subject );
    }

    public class ContentBuilder : IContentBuilder
    {
        public async Task<string> BuildRazorContent<T>(T objectContent, string contentTemplate)
        {
            var engine = new RazorLightEngineBuilder()
                // required to have a default RazorLightProject type, but not required to create a template from string.
                .UseEmbeddedResourcesProject(typeof(ContentBuilder))
                .UseMemoryCachingProvider()
                .Build();

            return await engine.CompileRenderStringAsync(Guid.NewGuid().ToString(), contentTemplate, objectContent);
        }


        private readonly IEmailUtil _emailUtil; ILogManager _logger;

        public ContentBuilder(IEmailUtil emailUtil, ILogManager logger)
        {
            _emailUtil = emailUtil;
            _logger = logger;

        }

        public async Task<bool> SendEmail<T>(T objectContent, string contentTemplate,List<DbSetting> settings, string fromEmail, string toEmail,
            string subject)
        {
            var bodyHtml = await BuildRazorContent(objectContent, contentTemplate);
            Console.WriteLine(bodyHtml);
            return await _emailUtil.SendEmail(settings, fromEmail, null, toEmail, null, subject, null,
                bodyHtml);
        }

    }
}