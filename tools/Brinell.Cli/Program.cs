using Brinell.Cli;

// brinell: Brinell's one command line (AD-028). `brinell --help` lists the commands.
return await BrinellCommand.RunAsync(args, CliIo.Console());
