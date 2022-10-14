using System.Reflection;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PromotionCoreExtensions
    {
        public static IServiceCollection AddPromotionCore(this IServiceCollection services)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}