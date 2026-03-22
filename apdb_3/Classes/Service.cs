using apdb_3.Classes.GearTypes;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

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
        private DateTime PromptForDate(string promptMsg)
        {
            string input = AnsiConsole.Prompt(
                new TextPrompt<string>(promptMsg)
                    .Validate(str =>
                    {
                        if (DateTime.TryParseExact(str, "dd:MM:yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                        {
                            return ValidationResult.Success();
                        }
                        return ValidationResult.Error("[red]Invalid date format. Please use DD:MM:YYYY[/]");
                    })
            );

            return DateTime.ParseExact(input, "dd:MM:yyyy", System.Globalization.CultureInfo.InvariantCulture);
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

                double feePerDay = _user.PermissionLevel > 0 ? Limits.EmployeeOverdueFee : Limits.RegularOverdueFee;
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

            gearList.RemoveAll(x => x.Broken || borrows.Any(y => y.GearId == x.Id && !y.Returned));

            gearList.ForEach(gear =>
            {
                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)gear).Lens}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)gear).Storage}GB[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)gear).DiscReader ? "Yes" : "No")}[/]";
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

            if (myBorrows.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You don't have any borrowed gear at the moment.[/]");
                GoBackToMenu();
                return;
            }

            foreach (var borrow in myBorrows)
            {
                var gear = gearList.FirstOrDefault(g => g.Id == borrow.GearId);

                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)gear).Lens}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)gear).Storage}GB[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)gear).DiscReader ? "Yes" : "No")}[/]";
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

            var myBorrows = borrows.Where(b => b.ClientUsername == _user.Username && b.Returned != true).ToList();

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

            TimeSpan finalTimeRemaining = selectedBorrow.BorrowEnd - DateTime.Now;
            bool returnedInTime = finalTimeRemaining.TotalSeconds > 0;

            selectedBorrow.MakeReturn(returnedInTime);

            AnsiConsole.MarkupLine("[bold green]Gear successfully returned![/]");
            GoBackToMenu();
        }
        private void ListAllGear()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.ShowRowSeparators();
            table.AddColumn("[bold]Name[/]");
            table.AddColumn("[bold]Description[/]");
            table.AddColumn("[bold]Additional Info[/]");
            table.AddColumn("[bold]Status[/]");
            table.Title("All gear in warehouse");

            foreach (var gear in gearList)
            {
                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)gear).Lens}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)gear).Storage}GB[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)gear).DiscReader ? "Yes" : "No")}[/]";
                }

                string status;

                var activeBorrow = borrows.FirstOrDefault(b => b.GearId == gear.Id && b.Returned != true);

                if (gear.Broken)
                {
                    status = "[red]Broken[/]";
                }
                else if (activeBorrow != null)
                {
                    status = $"[yellow]Borrowed by {activeBorrow.ClientUsername}[/]";
                }
                else
                {
                    status = "[green]Available[/]";
                }

                table.AddRow($"[green]{gear.Name}[/]", $"[purple]{gear.Description}[/]", additionalInfo, status);
            }

            AnsiConsole.Write(table);
            GoBackToMenu();
        }
        private void LendGear()
        {
            var choice_user = AnsiConsole.Prompt(
                new SelectionPrompt<User>()
                    .Title("Select user you want to lend gear to:")
                    .PageSize(10)
                    .EnableSearch()
                    .SearchPlaceholderText("Type username to search...")
                    .UseConverter(user => $"[grey]{user.Username}[/]{(user.Username == _user.Username ? " (You)" : "")}")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(Database.GetRecords<User>("users"))
            );

            AnsiConsole.Clear();

            ResetConsole();

            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            int limit = choice_user.PermissionLevel > 0 ? Limits.EmployeeMaxActiveBorrows : Limits.MaxActiveBorrows;

            if (borrows.FindAll(x => x.ClientUsername == choice_user.Username).Count >= limit)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]User [bold]{choice_user.Username}[/] exceeded borrow limit[/]");
                GoBackToMenu();
                return;
            }

            List<Gear> gearList = Database.GetRecords<Gear>("gear");

            gearList.RemoveAll(x => x.Broken || borrows.Any(y => y.GearId == x.Id && !y.Returned));

            var choice_gear = AnsiConsole.Prompt(
                new SelectionPrompt<Gear>()
                    .Title("Select gear you want to lend:")
                    .PageSize(10)
                    .EnableSearch()
                    .SearchPlaceholderText("Type text to search...")
                    .UseConverter(gear => $"[grey]{gear.Name}[/]")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(gearList)
            );

            AnsiConsole.Clear();

            ResetConsole();

            AnsiConsole.WriteLine();
            int duration = AnsiConsole.Ask<int>("For how many [bold]days[/]?");

            AnsiConsole.Clear();

            ResetConsole();

            AnsiConsole.WriteLine();

            var user_table = new Table();
            user_table.Border(TableBorder.Rounded);
            user_table.ShowRowSeparators();
            user_table.AddColumn("[bold]Username[/]");
            user_table.AddColumn("[bold]Type[/]");
            user_table.AddColumn("[bold]Active borrows[/]");

            int activeBorrows = borrows.FindAll(x => x.ClientUsername == choice_user.Username && x.Returned != true).Count;

            user_table.AddRow($"[green]{choice_user.Username}[/]", $"{(choice_user.PermissionLevel == 0 ? "Regular" : choice_user.PermissionLevel == 1 ? "Employee" : "Admin")}", $"[yellow]{activeBorrows}[/]");

            var gear_table = new Table();
            gear_table.Border(TableBorder.Rounded);
            gear_table.ShowRowSeparators();
            gear_table.AddColumn("[bold]Name[/]");
            gear_table.AddColumn("[bold]Desciption[/]");
            gear_table.AddColumn("[bold]Additional Info[/]");

            string additionalInfo = "";

            if (choice_gear.GetType() == typeof(Camera))
            {
                additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)choice_gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)choice_gear).Lens}[/]";
            }
            else if (choice_gear.GetType() == typeof(Laptop))
            {
                additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)choice_gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)choice_gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)choice_gear).Storage}GB[/]";
            }
            else if (choice_gear.GetType() == typeof(GamingConsole))
            {
                additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)choice_gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)choice_gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)choice_gear).DiscReader ? "Yes" : "No")}[/]";
            }

            gear_table.AddRow($"[green]{choice_gear.Name}[/]", $"[purple]{choice_gear.Description}[/]", additionalInfo);

            AnsiConsole.Write(user_table);
            AnsiConsole.Write(gear_table);
            AnsiConsole.MarkupLine($"[yellow]Duration:[/] [cyan]{duration}[/] [yellow]days[/]");

            AnsiConsole.WriteLine();

            if (AnsiConsole.Confirm("Check data above and then confirm..."))
            {
                Borrow borrow = new Borrow()
                {
                    BorrowStart = DateTime.Now,
                    BorrowEnd = DateTime.Now.AddDays(duration),
                    ClientUsername = choice_user.Username,
                    GearId = choice_gear.Id,
                    Overdue = false,
                    Returned = false
                };

                borrow.CreateBorrow();

                AnsiConsole.Clear();
                ResetConsole();
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold green]Gear successfully lent to {choice_user.Username}![/]");
                GoBackToMenu();
            } else
            {
                AnsiConsole.Clear();
                ResetConsole();
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold red]Operation aborted[/]");
                GoBackToMenu();
            }
        }
        private void ChangeGearStatus()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            gearList.RemoveAll(x => borrows.Any(y => y.GearId == x.Id && !y.Returned));

            if (gearList.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No available gear to change status at the moment.[/]");
                GoBackToMenu();
                return;
            }

            var choice_gear = AnsiConsole.Prompt(
                new SelectionPrompt<Gear>()
                    .Title("Select gear to change its [bold]Broken[/] status:")
                    .PageSize(10)
                    .EnableSearch()
                    .SearchPlaceholderText("Type text to search...")
                    .UseConverter(gear => $"[grey]{gear.Name}[/] - Status: {(gear.Broken ? "[red]Broken[/]" : "[green]Working[/]")}")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(gearList)
            );

            bool newStatus = !choice_gear.Broken;

            Database.UpdateRecord("gear", "Id", choice_gear.Id, "Broken", newStatus);

            AnsiConsole.Clear();
            ResetConsole();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold green]Gear '{choice_gear.Name}' status successfully changed to {(newStatus ? "[red]Broken[/]" : "[green]Working[/]")}![/]");
            GoBackToMenu();
        }
        private void ListUserGear()
        {
            var choice_user = AnsiConsole.Prompt(
                new SelectionPrompt<User>()
                    .Title("Select user to view their borrowed gear:")
                    .PageSize(10)
                    .EnableSearch()
                    .SearchPlaceholderText("Type username to search...")
                    .UseConverter(user => $"[grey]{user.Username}[/]")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(Database.GetRecords<User>("users"))
            );

            AnsiConsole.Clear();
            ResetConsole();

            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.ShowRowSeparators();
            table.AddColumn("[bold]Name[/]");
            table.AddColumn("[bold]Description[/]");
            table.AddColumn("[bold]Additional Info[/]");
            table.AddColumn("[bold]Return Date[/]");
            table.Title($"Borrowed gear: {choice_user.Username}");

            var userBorrows = borrows.Where(b => b.ClientUsername == choice_user.Username && !b.Returned).ToList();

            if (userBorrows.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]User {choice_user.Username} doesn't have any borrowed gear at the moment.[/]");
                GoBackToMenu();
                return;
            }

            foreach (var borrow in userBorrows)
            {
                var gear = gearList.FirstOrDefault(g => g.Id == borrow.GearId);
                if (gear == null) continue;

                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)gear).Lens}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)gear).Storage}GB[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)gear).DiscReader ? "Yes" : "No")}[/]";
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
        private void ListBorrowOvertimes()
        {
            List<Gear> gearList = Database.GetRecords<Gear>("gear");
            List<Borrow> borrows = Database.GetRecords<Borrow>("borrows");

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.ShowRowSeparators();
            table.AddColumn("[bold]Client[/]");
            table.AddColumn("[bold]Name[/]");
            table.AddColumn("[bold]Description[/]");
            table.AddColumn("[bold]Additional Info[/]");
            table.AddColumn("[bold]Overdue By[/]");
            table.Title("All Overdue Gear");

            var overdueBorrows = borrows.Where(b => !b.Returned && (b.BorrowEnd - DateTime.Now).TotalSeconds < 0).ToList();

            if (overdueBorrows.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]No gears are currently overdue![/]");
                GoBackToMenu();
                return;
            }

            foreach (var borrow in overdueBorrows)
            {
                var gear = gearList.FirstOrDefault(g => g.Id == borrow.GearId);
                if (gear == null) continue;

                string additionalInfo = "";

                if (gear.GetType() == typeof(Camera))
                {
                    additionalInfo = $"[grey]Mpx:[/] [cyan]{((Camera)gear).Mpx}[/]\n[grey]Lens:[/] [cyan]{((Camera)gear).Lens}[/]";
                }
                else if (gear.GetType() == typeof(Laptop))
                {
                    additionalInfo = $"[grey]Processor:[/] [cyan]{((Laptop)gear).Processor}[/]\n[grey]Ram:[/] [cyan]{((Laptop)gear).Ram}GB[/]\n[grey]Storage:[/] [cyan]{((Laptop)gear).Storage}GB[/]";
                }
                else if (gear.GetType() == typeof(GamingConsole))
                {
                    additionalInfo = $"[grey]Brand:[/] [cyan]{((GamingConsole)gear).Brand}[/]\n[grey]Storage:[/] [cyan]{((GamingConsole)gear).Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(((GamingConsole)gear).DiscReader ? "Yes" : "No")}[/]";
                }

                TimeSpan timeRemaining = borrow.BorrowEnd - DateTime.Now;
                string overdueText = $"[red]{Math.Abs(timeRemaining.Days)} days[/]";

                table.AddRow($"[yellow]{borrow.ClientUsername}[/]", $"[green]{gear.Name}[/]", $"[purple]{gear.Description}[/]", additionalInfo, overdueText);
            }

            AnsiConsole.Write(table);
            GoBackToMenu();
        }
        private void AddNewUser()
        {
            var username = AnsiConsole.Ask<string>("Enter [green]username[/] for the new user:");
            var password = AnsiConsole.Ask<string>("Enter [green]password[/]:");

            var permissionChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select user's [green]permission level[/]:")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(new[] { "0 = Student", "1 = Employee", "2 = Admin" })
            );

            int permissionLevel = int.Parse(permissionChoice.Substring(0, 1));

            User newUser = new User
            {
                Username = username,
                Password = password,
                PermissionLevel = permissionLevel
            };

            newUser.CreateUser();

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold green]User '{username}' created successfully![/]");
            GoBackToMenu();
        }
        private void GenerateWarehouseReport()
        {
            var reportType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]type of report[/] to generate:")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices("Users", "Gear", "Borrows")
            );

            bool applyFilters = AnsiConsole.Confirm("Would you like to apply filters?");

            if (reportType == "Users")
            {
                var users = Database.GetRecords<User>("users");

                if (applyFilters)
                {
                    var filters = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("Select [green]filters[/] to apply (Press Space to select, Enter to accept):")
                            .AddChoices("Permission Level", "Username")
                    );

                    if (filters.Contains("Permission Level"))
                    {
                        var levelChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select [green]Permission Level[/]:")
                                .AddChoices("0 = Student", "1 = Employee", "2 = Admin")
                        );
                        int level = int.Parse(levelChoice.Substring(0, 1));
                        users = users.Where(u => u.PermissionLevel == level).ToList();
                    }

                    if (filters.Contains("Username"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter [green]Username[/] search string:");
                        users = users.Where(u => u.Username.Contains(searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                }

                var table = new Table().Border(TableBorder.Rounded).ShowRowSeparators().Title("Users Report");
                table.AddColumn("[bold]Username[/]");
                table.AddColumn("[bold]Permission Level[/]");

                foreach (var user in users)
                {
                    string permStr = user.PermissionLevel == 0 ? "Student (0)" : user.PermissionLevel == 1 ? "Employee (1)" : "Admin (2)";
                    table.AddRow($"[green]{user.Username}[/]", $"[cyan]{permStr}[/]");
                }

                AnsiConsole.Clear();
                ResetConsole();
                AnsiConsole.Write(table);
            }
            else if (reportType == "Gear")
            {
                var gears = Database.GetRecords<Gear>("gear");

                if (applyFilters)
                {
                    var filters = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("Select [green]filters[/] to apply:")
                            .AddChoices("Type", "Type and additional info", "Name", "Description", "Id", "Broken status")
                    );

                    if (filters.Contains("Type") && !filters.Contains("Type and additional info"))
                    {
                        var typeChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select [green]Gear Type[/]:")
                                .AddChoices("Camera", "Laptop", "Gaming Console")
                        );
                        gears = gears.Where(g =>
                            (typeChoice == "Camera" && g is Camera) ||
                            (typeChoice == "Laptop" && g is Laptop) ||
                            (typeChoice == "Gaming Console" && g is GamingConsole)
                        ).ToList();
                    }

                    if (filters.Contains("Type and additional info"))
                    {
                        var typeChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select [green]Gear Type[/] for advanced filtering:")
                                .AddChoices("Camera", "Laptop", "Gaming Console")
                        );

                        if (typeChoice == "Camera")
                        {
                            var minMpx = AnsiConsole.Ask<int>("Enter minimum [green]Mpx[/] (Enter 0 to ignore):");
                            gears = gears.Where(g => g is Camera c && c.Mpx >= minMpx).ToList();
                        }
                        else if (typeChoice == "Laptop")
                        {
                            var minRam = AnsiConsole.Ask<int>("Enter minimum [green]RAM[/] in GB (Enter 0 to ignore):");
                            gears = gears.Where(g => g is Laptop l && l.Ram >= minRam).ToList();
                        }
                        else if (typeChoice == "Gaming Console")
                        {
                            var brandSearch = AnsiConsole.Ask<string>("Enter [green]Brand[/] to search (or leave empty):");
                            gears = gears.Where(g => g is GamingConsole gc && gc.Brand.Contains(brandSearch, StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                    }

                    if (filters.Contains("Name"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter [green]Name[/] search string:");
                        gears = gears.Where(g => g.Name.Contains(searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (filters.Contains("Description"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter [green]Description[/] search string:");
                        gears = gears.Where(g => g.Description.Contains(searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (filters.Contains("Id"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter full [green]Id[/]:");
                        gears = gears.Where(g => g.Id == searchStr).ToList();
                    }

                    if (filters.Contains("Broken status"))
                    {
                        var isBroken = AnsiConsole.Confirm("Filter for [red]Broken[/] gear? (No = Working gear)");
                        gears = gears.Where(g => g.Broken == isBroken).ToList();
                    }
                }

                var table = new Table().Border(TableBorder.Rounded).ShowRowSeparators().Title("Gear Report");
                table.AddColumn("[bold]Id[/]");
                table.AddColumn("[bold]Name[/]");
                table.AddColumn("[bold]Description[/]");
                table.AddColumn("[bold]Type[/]");
                table.AddColumn("[bold]Additional Info[/]");
                table.AddColumn("[bold]Broken[/]");

                foreach (var gear in gears)
                {
                    string typeStr = "Gear";
                    string addInfo = "-";

                    if (gear is Camera c) { typeStr = "Camera"; addInfo = $"[grey]Mpx:[/] [cyan]{c.Mpx}[/]\n[grey]Lens:[/] [cyan]{c.Lens}[/]"; }
                    else if (gear is Laptop l) { typeStr = "Laptop"; addInfo = $"[grey]Processor:[/] [cyan]{l.Processor}[/]\n[grey]Ram:[/] [cyan]{l.Ram}GB[/]\n[grey]Storage:[/] [cyan]{l.Storage}GB[/]"; }
                    else if (gear is GamingConsole gc) { typeStr = "Gaming Console"; addInfo = $"[grey]Brand:[/] [cyan]{gc.Brand}[/]\n[grey]Storage:[/] [cyan]{gc.Storage}GB[/]\n[grey]Disc Reader:[/] [cyan]{(gc.DiscReader ? "Yes" : "No")}[/]"; }

                    table.AddRow($"[grey]{gear.Id}[/]", $"[green]{gear.Name}[/]", $"[purple]{gear.Description}[/]", $"[cyan]{typeStr}[/]", addInfo, gear.Broken ? "[red]Yes[/]" : "[green]No[/]");
                }

                AnsiConsole.Clear();
                ResetConsole();
                AnsiConsole.Write(table);
            }
            else if (reportType == "Borrows")
            {
                var borrows = Database.GetRecords<Borrow>("borrows");

                if (applyFilters)
                {
                    var filters = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("Select [green]filters[/] to apply:")
                            .AddChoices("BorrowStart", "BorrowEnd", "Client username", "Id", "GearId", "Overdue value", "Returned value")
                    );

                    if (filters.Contains("BorrowStart"))
                    {
                        var startDate = PromptForDate("Enter [green]BorrowStart START Date[/] (DD:MM:YYYY):");
                        var endDate = PromptForDate("Enter [green]BorrowStart END Date[/] (DD:MM:YYYY):");
                        borrows = borrows.Where(b => b.BorrowStart >= startDate && b.BorrowStart <= endDate.AddDays(1).AddTicks(-1)).ToList();
                    }

                    if (filters.Contains("BorrowEnd"))
                    {
                        var startDate = PromptForDate("Enter [green]BorrowEnd START Date[/] (DD:MM:YYYY):");
                        var endDate = PromptForDate("Enter [green]BorrowEnd END Date[/] (DD:MM:YYYY):");
                        borrows = borrows.Where(b => b.BorrowEnd >= startDate && b.BorrowEnd <= endDate.AddDays(1).AddTicks(-1)).ToList();
                    }

                    if (filters.Contains("Client username"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter [green]Client Username[/] search string:");
                        borrows = borrows.Where(b => b.ClientUsername.Contains(searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (filters.Contains("Id"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter full Borrow [green]Id[/]:");
                        borrows = borrows.Where(b => b.Id == searchStr).ToList();
                    }

                    if (filters.Contains("GearId"))
                    {
                        var searchStr = AnsiConsole.Ask<string>("Enter full [green]GearId[/]:");
                        borrows = borrows.Where(b => b.GearId == searchStr).ToList();
                    }

                    if (filters.Contains("Overdue value"))
                    {
                        var isOverdue = AnsiConsole.Confirm("Filter for [red]Overdue[/] borrows? (No = Not Overdue)");
                        borrows = borrows.Where(b => b.Overdue == isOverdue).ToList();
                    }

                    if (filters.Contains("Returned value"))
                    {
                        var isReturned = AnsiConsole.Confirm("Filter for [green]Returned[/] borrows? (No = Not Returned)");
                        borrows = borrows.Where(b => b.Returned == isReturned).ToList();
                    }
                }

                var table = new Table().Border(TableBorder.Rounded).ShowRowSeparators().Title("Borrows Report");
                table.AddColumn("[bold]Id[/]");
                table.AddColumn("[bold]GearId[/]");
                table.AddColumn("[bold]Client[/]");
                table.AddColumn("[bold]BorrowStart[/]");
                table.AddColumn("[bold]BorrowEnd[/]");
                table.AddColumn("[bold]Returned[/]");
                table.AddColumn("[bold]Returned as overdue[/]");

                foreach (var borrow in borrows)
                {
                    table.AddRow(
                        $"[grey]{borrow.Id}[/]",
                        $"[grey]{borrow.GearId}[/]",
                        $"[yellow]{borrow.ClientUsername}[/]",
                        $"[cyan]{borrow.BorrowStart:dd:MM:yyyy HH:mm}[/]",
                        $"[cyan]{borrow.BorrowEnd:dd:MM:yyyy HH:mm}[/]",
                        borrow.Returned ? "[green]Yes[/]" : "[red]No[/]",
                        borrow.Overdue ? "[red]Yes[/]" : "[green]No[/]"
                    );
                }

                AnsiConsole.Clear();
                ResetConsole();
                AnsiConsole.Write(table);
            }

            GoBackToMenu();
        }
        private void AddNewItemToWarehouse()
        {
            var typeChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]type[/] of gear to add:")
                    .HighlightStyle(new Style(foreground: Color.White, decoration: Decoration.Bold))
                    .AddChoices(new[] { "Camera", "Laptop", "Gaming Console" })
            );

            var name = AnsiConsole.Ask<string>("Enter [green]Name[/]:");
            var description = AnsiConsole.Ask<string>("Enter [green]Description[/]:");

            Gear newGear = null;

            if (typeChoice == "Camera")
            {
                var mpx = AnsiConsole.Ask<int>("Enter [green]Megapixels (Mpx)[/]:");
                var lens = AnsiConsole.Ask<string>("Enter [green]Lens[/]:");
                newGear = new Camera { Name = name, Description = description, Mpx = mpx, Lens = lens };
            }
            else if (typeChoice == "Laptop")
            {
                var processor = AnsiConsole.Ask<string>("Enter [green]Processor[/]:");
                var ram = AnsiConsole.Ask<int>("Enter [green]RAM (GB)[/]:");
                var storage = AnsiConsole.Ask<int>("Enter [green]Storage (GB)[/]:");
                newGear = new Laptop { Name = name, Description = description, Processor = processor, Ram = ram, Storage = storage };
            }
            else if (typeChoice == "Gaming Console")
            {
                var brand = AnsiConsole.Ask<string>("Enter [green]Brand[/]:");
                var storage = AnsiConsole.Ask<int>("Enter [green]Storage (GB)[/]:");
                var discReader = AnsiConsole.Confirm("Does it have a [green]Disc Reader[/]?");
                newGear = new GamingConsole { Name = name, Description = description, Brand = brand, Storage = storage, DiscReader = discReader };
            }

            if (newGear != null)
            {
                newGear.CreateGear();
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold green]New {typeChoice} '{name}' successfully added to the warehouse![/]");
            }

            GoBackToMenu();
        }
    }
}
