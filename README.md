# Dragon Placement API

This is a project to practice Angular developement. It is to be used by an imaginary company that matches job-hunting dragons with companies looking to employ dragons in contract work.

The companion Angular code can be found at: https://github.com/bkrug/dragon-placement-ui

## Running this application for the first time

1. Create an empty SQLite database in the "Database" folder.
1. Run the SQL in `./Database/schemaDragonPlacement.sql` in the new database, creating the schema.
1. Run the SQL in `./Database/insertTestData.sql` to populate the 'Job' and 'Dragon' tables. The application currently only edits records in the 'Assignments' table.

## Helpful Commands

Run this command from the DragonPlacementDataLayer folder to update the models
`dotnet ef dbcontext scaffold "Data Source=../Database/DragonPlacement.db" Microsoft.EntityFrameworkCore.Sqlite -o Models --force`
