# Dragon Placement API

This is a project to practice Angular developement. It is to be used by an imaginary company that matches job-hunting dragons with companies looking to employ dragons in contract work.

The companion Angular code can be found at: https://github.com/bkrug/dragon-placement-ui

## Running this application for the first time

You will need to create the databse.
Run these commands from the root of the repo.

```
sqlite3 ./Database/DragonPlacement.db < ./Database/schemaDragonPlacement.sql
sqlite3 ./Database/DragonPlacement.db < ./Database/insertTestData.sql
sqlite3 ./Database/DragonPlacement.db < ./Database/insertSkillTagData.sql

dotnet run --project=DragonPlacementApi
```

## Helpful Commands

Run this command from the DragonPlacementDataLayer folder to update the models
`dotnet ef dbcontext scaffold "Data Source=../Database/DragonPlacement.db" Microsoft.EntityFrameworkCore.Sqlite -o Models`

Use the `--force` tag, if you want to replace the old models.

## Clean Architecture

The main criticism of Clean Architecture that I am aware of is that people don't always like having so many extra projects within the solution.
That is a reasonable criticism.
The soution indeed has three projects for each business domain, and three (very small) common projects.

One could potentially reduce the number of projects.
In this example, projects are a low-tech solution for enforcing one-directional dependencies between layers.
In applications that put layers in their own folders, some sort of unit testing or linting solution is necessary to confrim that a domain layer does not inherit from an infrastructure layer.
Those solutions are better,
I just haven't gotten around to implementing them yet.

## Using more of a Domain Driven Design

Note to self: Reference these every once in a while
- https://medium.com/@danceforrasputin/-411a365022f4
- https://dev.to/mashrulhaque/how-to-design-a-maintainable-net-solution-structure-for-growing-teams-284n
- https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/
- https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- https://enterprisecraftsmanship.com/posts/validation-and-ddd/