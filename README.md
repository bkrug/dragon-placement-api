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

## Domains/Modules

In this application, there are two domain-modules: assigment and timekeeping.
Assignment is responsible for matching a dragon with a job based on required skills.
Timekeeping is responsible for keeping track of how much a dragon has worked at a given job.

Someday there can be a billing module, separate from timekeeping or assignment.
It would know how many hours to bill a client for work done by a particular dragon.
The billing module wouldn't know the dragon's hours in the same detail as the timekeeping module.
It also wouldn't care about the skills associated with a dragon or a job.

## Clean Architecture

The main criticism of Clean Architecture that I am aware of is that people don't always like having so many extra projects within the solution.
That is a reasonable criticism.
So in this repo we are putting layers in their own folders
The NetArchTest library is being used to enforce the correct dependency direction for different layers.
See tests in the "ArchitectureTests" folder.

## Using more of a Domain Driven Design

Note to self: Reference these every once in a while
- https://medium.com/@danceforrasputin/-411a365022f4
- https://dev.to/mashrulhaque/how-to-design-a-maintainable-net-solution-structure-for-growing-teams-284n
- https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/
- https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- https://enterprisecraftsmanship.com/posts/validation-and-ddd/

Shared content between domains:
- https://martinfowler.com/bliki/BoundedContext.html
- https://www.oreilly.com/library/view/what-is-domain-driven/9781492057802/ch04.html
- https://deviq.com/domain-driven-design/shared-kernel/
- https://medium.com/@iamprovidence/relationships-between-bounded-contexts-in-ddd-ce5cfe3aaa04