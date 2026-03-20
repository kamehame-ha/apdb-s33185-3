using apdb_3.Classes;
using apdb_3.Classes.UserTypes;
using Spectre.Console;

class Program
{
    static void ResetConsole(string username)
    {
        if (username == null)
        {
            AnsiConsole.Clear();
            var rule = new Rule("[white bold]Gear Borrow System[/]");
            rule.Justification = Justify.Left;
            AnsiConsole.Write(rule);
        } else
        {
            AnsiConsole.Clear();
            var rule = new Rule($"[white bold]Gear Borrow System[/] (as [red bold]{username}[/])");
            rule.Justification = Justify.Left;
            AnsiConsole.Write(rule);
        }
    }
    static void Main()
    {
        AnsiConsole.Clear();

        ResetConsole(null);

        AnsiConsole.WriteLine();

        bool isAuthenticated = false;
        int maxAttempts = 3;
        int attempts = 0;
        User user = new User();

        while (!isAuthenticated && attempts < maxAttempts)
        {
            var username = AnsiConsole.Prompt(
                new TextPrompt<string>("   [grey]>[/] [bold white]Username:[/] ")
                    .PromptStyle("green")
            );

            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("   [grey]>[/] [bold white]Password:[/] ")
                    .PromptStyle("green")
                    .Secret()
            );

            AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green"))
                .Start("Verifying credentials...", ctx =>
                {
                    Thread.Sleep(2000);

                    user = Database.GetRecord<User>("users", "Username", username);

                    if (user != null && user.Password == password)
                    {
                        isAuthenticated = true;
                    } else
                    {
                        AnsiConsole.MarkupLine("[red]Invalid username or password. Please try again.[/]");
                    }
                });

            if (isAuthenticated)
            {
                AnsiConsole.Clear();

                ResetConsole(user.Username);
            }
        }

        Console.ReadLine();
    }
}