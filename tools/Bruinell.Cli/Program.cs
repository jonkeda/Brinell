using Bruinell.Cli;

// bruinell: Bruinell's one command line (AD-028). `bruinell --help` lists the commands.
return await BruinellCommand.RunAsync(args, CliIo.Console());
