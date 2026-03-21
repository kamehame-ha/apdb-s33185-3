using apdb_3.Classes;
using apdb_3.Classes.UserTypes;
using Spectre.Console;

class Program
{
    static void Main()
    {
        Service service = new Service();

        service.ResetConsole();

        AnsiConsole.WriteLine();

        service.Init();

        Console.ReadLine();
    }
}