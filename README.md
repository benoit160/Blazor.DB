# Blazor.DB
### An easy to use database storage in the browser for your Blazor Webassembly app, with the power of Entity Framework Core and SQLite.

 ## Table of Contents
 1. [Features](#features)
 2. [Setup](#setup)
 3. [How to use](#how-to-use)
 4. [Example code](#example-code)

## Features 

- Provides permanent storage  to your Blazor WebAssembly  app
  - Uses Entity Framework Core with SQLite as the database engine
  - Your DB is stored inside the browser cache

## Setup 

- If you don't have it already, install the wasm tools with the following command in VS powershell
```
dotnet install workload wasm-tools
```

- Add the nuget package reference to your project
  
```csharp
  <ItemGroup>
	<PackageReference Include="Blazor.DB" Version="1.2.0" />
  </ItemGroup>
```
## How to use

- Create your database class inheriting from BlazorDBContext in the BlazorDB namespace

```csharp
using BlazorDB;
using Microsoft.EntityFrameworkCore;

namespace MyBlazorApp;

public class Person 
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public Person()
    {
        Name = string.Empty;
        Address = string.Empty;
    }
}

public class MyBlazorDatabase : BlazorDBContext 
{
    public DbSet<Person> Persons { get; set; }
    
    // no need to override OnConfiguring, it is already good to go
}
```

- In the `Main` method of the Program.cs class, call the `ConfigureBlazorDBAsync` method
  - It requires the type of your database class as a generic argument

```csharp
public static async Task Main(string[] args)
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    WebAssemblyHost host = builder.Build();

    await host.Services.ConfigureBlazorDBAsync<MyBlazorDatabase>();

    await host.RunAsync();
}
```

- When you want to copy the database to the browser permanent storage, call the `SaveToCacheAsync` method

## Example code

- Some basic usage in a razor page : adding a Person to the database, and the data persists when your refresh the page

```
@page "/"
@using Microsoft.EntityFrameworkCore

<PageTitle>Index</PageTitle>

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

<button @onclick="AddNewPerson">Add new person</button>

<ul>
    @foreach (Person p in People)
    {
        <li>Id : @p.Id - @p.Name</li>
    }
</ul>
```

```csharp
@code {
    private List<Person> People = new List<Person>();

    private string[] names = { "Ben", "Johnny", "Rob", "Ryan", "James", "Bob", "Carlos" };

    protected override async Task OnInitializedAsync()
    {
        using MyBlazorDatabase db = new MyBlazorDatabase();

        People = await db.Persons.ToListAsync();
    }

    /// <summary>
    /// Adds a new person to the database, and then saves it to the cache storage
    /// </summary>
    /// <returns></returns>
    private async Task AddNewPerson()
    {
        using MyBlazorDatabase db = new MyBlazorDatabase();

        Person p = new Person
        {
            // Selects a random name
            Name = names[Random.Shared.Next(names.Length)],
            Address = "No adress yet"
        };

        db.Persons.Add(p);
        People.Add(p);

        // Saves a copy of the DB to the permament cache storage
        await db.SaveToCacheAsync();
    }
}
```
