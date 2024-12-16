using eAvto_eSTO;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Title = "єАвто - єСТО";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Bot is waking up...");

            using var cts = new CancellationTokenSource();
            await Bot.RunAsync(cts.Token);

            Console.ReadKey();
            cts.Cancel();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}

