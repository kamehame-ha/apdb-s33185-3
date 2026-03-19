using apdb_3.Classes;

Console.WriteLine("Start");

User testuser = new User
{
    Username = "Test",
    PermissionLevel = 0,
    Password = "Test"
};

Database.AddRecord("users", testuser);

User testuser2 = new User
{
    Username = "Test2",
    PermissionLevel = 1,
    Password = "Test2"
};

Database.AddRecord("users", testuser2);

//User targetuser = (User)Database.GetRecord("users", "Username", "Test");

//Console.WriteLine($"Found user!\nUsername: {targetuser.Username}");

Console.ReadLine();

Database.DeleteRecord("users", testuser2);

Console.WriteLine("Finish");