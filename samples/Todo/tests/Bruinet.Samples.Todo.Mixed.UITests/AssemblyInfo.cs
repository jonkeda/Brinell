// One process holds every app, and the tests share them: nothing runs in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
