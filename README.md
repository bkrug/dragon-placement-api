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
