using RazorLight;
using System;
using System.Threading.Tasks;

namespace App.Infrastructure.Builders.Common
{
    public interface IContentBuilder
    {
        Task<string> BuildRazorContent<T>(T objectContent, string contentTemplate);
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
    }
}