# ChristmasChallenge2022
Kayak christmas challenge by Boris Pavlov

The main idea is to store the data from the original CSV in two tables.
- 'ranges' containing only the ip ranges and a location ID
- 'locations' the data about each location
I chose to store them in a SQLite db for faster reading from disk and the convenience of the data retrieval.
