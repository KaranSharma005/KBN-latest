using KBN.RepoHelpers;

namespace KBN.DependenciesMgt
{
    public static class ServicesRegisteration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddTransient<DIDHelper>(); 
            return services;
        }
    }
}
