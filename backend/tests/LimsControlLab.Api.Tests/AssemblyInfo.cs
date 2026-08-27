using Xunit;

// ponytail: Postgres CREATE DATABASE/DROP DATABASE take an exclusive lock on
// pg_database, so integration test classes racing to create/drop their own
// test db in parallel time out against each other. Run them serially instead
// of adding retry/locking machinery - the suite is fast enough (~30s) that
// this costs nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
