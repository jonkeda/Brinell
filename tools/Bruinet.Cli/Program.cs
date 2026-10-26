using Bruinet.Cli;

// bruinet: Bruinet's one command line (AD-028). `bruinet --help` lists the commands.
return await BruinetCommand.RunAsync(args, CliIo.Console());
