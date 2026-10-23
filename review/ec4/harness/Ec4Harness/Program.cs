// EC-4 review-only harness. Not a production dependency.
// Usage:
//   Ec4Harness full <repoPath> <outDir>              index + fact dump + Q1-Q8
//   Ec4Harness heritage <repoPath> <outFile>         F-Q2-01 heritage scenario
//   Ec4Harness lifecycle <repoPath> <outDir>         incremental publication scenarios
//   Ec4Harness durable <repoPath> <outDir> <dbPath>  durable LatticeDb reopen-and-query
//   Ec4Harness migrate <repoPath> <dbPath> <outFile> real p4 -> p5 migration
//   Ec4Harness distill <repoPath> <outDir>           compact evidence inventories
using Penghou.Hetu;
using Penghou.Hetu.TreeSitter;

return args switch
{
    ["full", var repo, var outDir] => await Ec4Full.RunAsync(repo, outDir),
    ["heritage", var repo, var outFile] => await Ec4Heritage.RunAsync(repo, outFile),
    ["lifecycle", var repo, var outDir] => await Ec4Lifecycle.RunAsync(repo, outDir),
    ["durable", var repo, var outDir, var db] => await Ec4Durable.RunAsync(repo, outDir, db),
    ["migrate", var repo, var db, var outFile] => await Ec4Migrate.RunAsync(repo, db, outFile),
    ["distill", var repo, var outDir] => await Ec4Distill.RunAsync(repo, outDir),
    _ => Usage()
};

static int Usage()
{
    Console.Error.WriteLine(
        "expected: full|heritage|lifecycle|durable|migrate <repoPath> <outDir|outFile> [dbPath]");
    return 2;
}
