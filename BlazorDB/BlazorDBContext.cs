using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDB;

public abstract class BlazorDBContext : DbContext
{
    private static IJSRuntime runtime = default!;

    public static void ConfigureInterropRuntime(IJSRuntime _runtime) => runtime = _runtime;

    protected override sealed void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=BlazorDatabase.sqlite3");
    }

    /// <summary>
    /// Creates a copy of the current database in the browser cache
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task SaveToCacheAsync()
    {
        await SaveChangesAsync();

        // Clear the connection to force a checkpoint ( merge .wal and .shm files to .db )
        Microsoft.Data.Sqlite.SqliteConnection sqliteCn = new Microsoft.Data.Sqlite.SqliteConnection(Database.GetConnectionString());
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(sqliteCn);

        await using IJSObjectReference jsModule = await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Blazor.DB/Cache.js");
        await jsModule.InvokeVoidAsync("SaveToBrowserCache");
    }

    /// <summary>
    /// Restore the database file from the browser cache
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public static async Task<bool> RestoreFromCacheAsync()    
    {
        await using IJSObjectReference jsModule = await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Blazor.DB/Cache.js");
        return await jsModule.InvokeAsync<bool>("RestoreFromBrowserCache");
    }

    /// <summary>
    /// Deletes the cached copy of the database
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task DeleteFromCacheAsync() 
    {
        await using IJSObjectReference jsModule = await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Blazor.DB/Cache.js");
        await jsModule.InvokeVoidAsync("DeleteDBFromCache");
    }
}

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