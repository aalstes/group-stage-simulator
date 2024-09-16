# Group Stage Simulator

The Group Stage Simulator is a web application that simulates a group stage for four teams.

## Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- C#
- HTML/CSS
- Bootstrap

## How It Works

1. The application starts with four pre-defined teams.
2. When the user clicks "Simulate Group Stage", it generates matches for a complete round-robin tournament.
3. Scores are randomly generated for each match, taking into account team strengths.
4. After simulation, the application displays all match results and final standings.

## Getting Started

See https://learn.microsoft.com/en-us/dotnet/core/tools/

Using the .NET command-line interface (CLI):

```sh
dotnet tool install --global dotnet-ef

dotnet ef database update

dotnet run
```
