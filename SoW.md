#  Statement of Works

## Email Setup

[x] Enable email for new user registration

## UI Tweaks

[x] Add the Session Date and Time to the Sets pages in the format dd/mm/yy hh:mm

[x] Add the Dession Date and Time to the Reps pages in the format dd/mm/yy hh:mm

[ ] Add the Set Type to the Reps Pages

[ ] Add the Session name to the Rep Pages

[ ] Display Reps in a Table

[ ] Add Sorting and Filtering to Sets pages

[ ] Add Sorting and Filtering to Reps pages

## Refactor Excercises

[x] Add ExcerciseTypes table, this will have two fields ID and Name

[x] Add pages to manage ExcerciseTypes using dotnet aspnet-codegenerator razorpage

[x] Remove Excercise table 

[x] remove Excercise pages

[x] Fix sets Model to not reference Excercises Table

[x] Fix Sets Pages to not reference Excercises

[x] update sets pages so that each Set has an associated Excercise from the  ExcerciseTypes table.

[x] Update sets table to reference selected excercise type

## Refactor Weight tracking

[x] Add weight field to Sets.

[x] Add code to copy the weight value from a set to reps as they are created

## Documentation

[x] Update readme file sumerising the functionality of the project

[x] Create an inventory.md file
