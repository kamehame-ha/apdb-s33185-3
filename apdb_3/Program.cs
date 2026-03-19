using apdb_3.Classes;
using apdb_3.Classes.UserTypes;

Admin admin = new Admin() { Username = "admin", Password = "strong"};
admin.CreateUser(admin);

Console.ReadLine();