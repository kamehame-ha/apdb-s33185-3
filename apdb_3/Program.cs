using apdb_3.Classes;
using apdb_3.Classes.GearTypes;
using apdb_3.Classes.UserTypes;

Laptop laptop = new Laptop {
    Name = "Lenovo ThinkPad X1 Carbon",
    Description = "A high-end business laptop with a sleek design and powerful performance.",
    Processor = "Intel Core i7-1165G7"
};

laptop.CreateGear();

System.Console.ReadLine();