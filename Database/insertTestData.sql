insert into Dragon
(GivenName, FamilyName, CanBreathFire, CanTakePassengers, FightingSkills, WeightInKg, LengthInMeters)
values
('Scorch', null, 1, 1, '', 7525, 30 ),
('Tumble', 'of Grennwich', 0, 1, '', 4000, 15 ),
('Grunal', 'Stoneson', 0, 0, 'm', 6892, 23 ),
('Magno', 'Sharp', 0, 1, 'b', 5190, 21 ),
('Rose', null, 0, 0, '', 4090, 17 )

insert into Job
(JobTitle, EmployerName, NumberOfPositions, StartDateUnix, EndDateUnix)
values
('Body Guard', 'Prince Valiant', 1, unixepoch('2026-06-01'), unixepoch('2026-06-14')),
('Greeter', 'Castle of Saftey', 2, unixepoch('2026-05-15'), unixepoch('2026-09-15')),
('Thief', 'Illlegal Stuff R Us', 3, unixepoch('2026-07-12'), unixepoch('2026-07-12')),
('Boxing Coach', 'Knights Arena', 1, unixepoch('2026-06-15'), unixepoch('2026-07-25'))