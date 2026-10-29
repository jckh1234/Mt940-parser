using System;
using Mt940Parser;

namespace Mt940Parser.Scheduler
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                SchedulerOptions options;
                string error;
                if (!SchedulerOptions.TryParse(args, out options, out error))
                {
                    Console.Error.WriteLine(error);
                    Console.Error.WriteLine(SchedulerOptions.Usage);
                    return DailyStatementProcessor.InvalidArgumentsExitCode;
                }

                if (options.Help)
                {
                    Console.WriteLine(SchedulerOptions.Usage);
                    return DailyStatementProcessor.SuccessExitCode;
                }

                var result = DailyStatementProcessor.Process(
                    options.SharedFolder,
                    options.AccountNumber,
                    options.StatementDate,
                    options.ConnectionString);

                for (int i = 0; i < result.Messages.Count; i++)
                    Console.WriteLine(result.Messages[i]);

                for (int i = 0; i < result.InsertScripts.Count; i++)
                    Console.WriteLine(result.InsertScripts[i]);

                return result.ExitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return DailyStatementProcessor.IoErrorExitCode;
            }
        }
    }
}
