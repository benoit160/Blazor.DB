using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorDB;

public static class Extensions
{
    /// <summary>
    /// Initialize BlazorDBContext, and restore the database from cache if applicable
    /// </summary>
    /// <typeparam name="TContext">Your implementation of BlazorDBContext</typeparam>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static async Task<IServiceProvider> ConfigureBlazorDBAsync<TContext>(this IServiceProvider serviceProvider)
        where TContext : BlazorDBContext
    {
        BlazorDBContext.ConfigureInterropRuntime(serviceProvider.GetRequiredService<IJSRuntime>());

        bool restoredSuccessfully = await BlazorDBContext.RestoreFromCacheAsync();

        if (!restoredSuccessfully)
        {
            // check if BlazorDBContext subtype has default parameterless constructor
            if (typeof(TContext).GetConstructors().Any(x => x.GetParameters().Length == 0))
            {
                // creates an instance of the BlazorDBContext subtype, to initialize it's database
                TContext dbContextInstance = (Activator.CreateInstance(typeof(TContext)) as TContext)!;

                // makes sure the database is correctly created
                await dbContextInstance.Database.EnsureCreatedAsync();
            }
            else
                throw new Exception($"{typeof(TContext).Name} must have a public parameterless constructor");
        }

        return serviceProvider;
    }
}