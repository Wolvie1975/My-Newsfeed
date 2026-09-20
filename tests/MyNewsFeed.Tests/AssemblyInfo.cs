// The integration tests share one live SQL Server database and take row/range locks inside their transactions.
// Running test classes in parallel makes them deadlock each other, so run them one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
