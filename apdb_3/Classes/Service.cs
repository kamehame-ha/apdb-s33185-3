using System;
using System.Collections.Generic;
using System.Text;
using apdb_3.Classes.GearTypes;
using Spectre.Console;

namespace apdb_3.Classes
{
    public class Service
    {
        private User _user = new User();
        public record MenuOption(int i, string text);
        public void ResetConsole()
        {
            AnsiConsole.Clear();

            if (_user.Username == null)
            {
                AnsiConsole.Clear();
                var rule = new Rule("[white bold]Gear Borrow System[/]");
                rule.Justification = Justify.Left;
                AnsiConsole.Write(rule);
            }
            else
            {
                AnsiConsole.Clear();
                var rule = new Rule($"[white bold]Gear Borrow System[/] (as [green bold]{_user.Username}[/])");
                rule.Justification = Justify.Left;
                AnsiConsole.Write(rule);
            }
        }

        public void GoBackToMenu()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]>[/] Press [white]Enter[/] to return to the main menu...");
            Console.ReadLine();
            AnsiConsole.Clear();
            ResetConsole();
            MainMenu();
        }

        public void Init()
        {
            bool isAuthenticated = false;
            int maxAttempts = 3;
            int attempts = 0;

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

                        _user = Database.GetRecord<User>("users", "Username", username);

                        if (_user != null && _user.Password == password)
                        {
                            isAuthenticated = true;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]Invalid username or password. Please try again.[/]");
                        }
                    });

                if (isAuthenticated)
                {
                    ResetConsole();
                    MainMenu();
                }
            }
        }

        private void HandleOvertimeFee(Borrow borrow)
        {
            TimeSpan timeRemainingForReturn = borrow.BorrowEnd - DateTime.Now;

            if (timeRemainingForReturn.TotalSeconds < 0)
            {
                int overdueDays = Math.Max(1, Math.Abs(timeRemainingForReturn.Days));

                double feePerDay = _user.PermissionLevel > 0 ? 2.5 : 5.0;
                double totalFee = overdueDays * feePerDay;

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]This item is overdue by {overdueDays} day(s).[/]");
                AnsiConsole.MarkupLine($"[red]Your overtime fee is:[/] [bold white]${totalFee:0.00}[/]");

                AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("You must approve the overtime fee to proceed with the return:")
                        .AddChoices(new[] { $"Approve and pay ${totalFee:0.00}" })
                        .HighlightStyle(new Style(foreground: Color.Red, decoration: Decoration.Bold))
                );
            }
        }
        public void MainMenu()
        {
            var regularChoices = new[] {
                new MenuOption(0, "List gear"),
                new MenuOption(1, "My borrowed gear"),
                new MenuOption(2, "Return gear")
            };

            var employeeChoices = new[] {
                new MenuOption(3, "List all gear"),
                new MenuOption(4, "Lend gear"),
                new MenuOption(5, "Change gear status"),
                new MenuOption(6, "List user gear"),
                new MenuOption(7, "List borrow overtimes"),
                new MenuOption(10, "Add new item to warehouse"),
            };

            var adminChoices = new[] {
                new MenuOption(8, "Add new user"),
                new MenuOption(9, "Generate warehouse report")
            };

            var menuList = new List<MenuOption>(regularChoices);

            if (_user.PermissionLevel >= 1) menuList.AddRange(employeeChoices);
            if (_user.PermissionLevel >= 2) menuList.AddRange(adminChoices);

            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<MenuOption>()
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(menuList)
                    .UseConverter(option => $"[grey]{option.text}[/]")
            );

            switch (choice.i)
            {
                case 0: ListGear(); break;
                case 1: MyBorrowedGear(); break;
                case 2: ReturnGear(); break;
                case 3: ListAllGear(); break;
                case 4: LendGear(); break;
                case 5: ChangeGearStatus(); break;
                case 6: ListUserGear(); break;
                case 7: ListBorrowOvertimes(); break;
                case 8: AddNewUser(); break;
                case 9: GenerateWarehouseReport(); break;
                case 10: AddNewItemToWarehouse(); break;
            }
        }
        private void ListGear()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.ShowRowSeparators();
            table.AddColumn("[bold]Name[/]");
            table.AddColumn("[bold]Desciption[/]");
            table.AddColumn("[bold]Additional Info[/]");
            table.Title("Gear ready to be borrowed");

            gearList.RemoveAll(x => borrows.Any(y =>
                y.GearId == x.Id &&
                y.BorrowEnd > DateTime.Now
            ));

            gearList.ForEach(gear =>
            {
                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]";
                } else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]";
                } else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]";
                }

                table.AddRow($"[green]{gear.Name}[/]", $"[purple]{gear.Description}[/]", $"{additionalInfo}");
            });

            AnsiConsole.Write(table);
            GoBackToMenu();
        }
        private void MyBorrowedGear()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.ShowRowSeparators();
            table.AddColumn("[bold]Name[/]");
            table.AddColumn("[bold]Description[/]");
            table.AddColumn("[bold]Additional Info[/]");
            table.AddColumn("[bold]Return Date[/]");
            table.Title("Your borrowed gear");

            var myBorrows = borrows.Where(b => b.ClientUsername == _user.Username).ToList();

            foreach (var borrow in myBorrows)
            {
                var gear = gearList.FirstOrDefault(g => g.Id == borrow.GearId);

                if (gear == null) continue;

                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]";
                }

                TimeSpan timeRemaining = borrow.BorrowEnd - DateTime.Now;
                string formattedDate = borrow.BorrowEnd.ToString("dd:MM:yyyy");
                string humanizedDateText;

                if (timeRemaining.TotalSeconds > 0)
                {
                    humanizedDateText = $"[green]{formattedDate} (in {timeRemaining.Days} days)[/]";
                }
                else
                {
                    humanizedDateText = $"[red]{formattedDate} (overdue by {Math.Abs(timeRemaining.Days)} days)[/]";
                }

                table.AddRow($"[green]{gear.Name}[/]", $"[purple]{gear.Description}[/]", additionalInfo, humanizedDateText);
            }

            AnsiConsole.Write(table);
            GoBackToMenu();
        }
        private void ReturnGear()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var myBorrows = borrows.Where(b => b.ClientUsername == _user.Username).ToList();

            if (myBorrows.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You don't have any borrowed gear to return at the moment.[/]");
                GoBackToMenu();
                return;
            }

            var selectedBorrow = AnsiConsole.Prompt(
                new SelectionPrompt<Borrow>()
                    .Title("Select the gear you want to [green]return[/]:")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(myBorrows)
                    .UseConverter(borrow =>
                    {
                        var gear = gearList.FirstOrDefault(g => g.Id == borrow.GearId);

                        TimeSpan timeRemaining = borrow.BorrowEnd - DateTime.Now;
                        string formattedDate = borrow.BorrowEnd.ToString("dd:MM:yyyy");
                        string humanizedDateText;

                        if (timeRemaining.TotalSeconds > 0)
                        {
                            humanizedDateText = $"[green]{formattedDate} (in {timeRemaining.Days} days)[/]";
                        }
                        else
                        {
                            humanizedDateText = $"[red]{formattedDate} (overdue by {Math.Abs(timeRemaining.Days)} days)[/]";
                        }

                        return $"{gear.Name} - {humanizedDateText}";
                    })
            );

            HandleOvertimeFee(selectedBorrow);

            selectedBorrow.DeleteBorrow();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]Gear successfully returned![/]");
            GoBackToMenu();
        }
        private void ListAllGear()
        {

        }
        private void LendGear()
        {

        }
        private void ChangeGearStatus()
        {

        }
        private void ListUserGear()
        {

        }
        private void ListBorrowOvertimes()
        {

        }
        private void AddNewUser()
        {

        }
        private void GenerateWarehouseReport()
        {

        }
        private void AddNewItemToWarehouse()
        {

        }
    }
}
