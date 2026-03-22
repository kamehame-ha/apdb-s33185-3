# apdb_3

## Zadbanie o kohezję itp.

* Każda klasa zasadniczo posiada jedną funkcję np. klasa Database zajmuje się ekstrakcją danych json. A klasa Limits przechowuje informacje o limitach
* Zastosowałem dziedziczenie żeby klasy podtypów User oraz Gear były faktycznymi podtypami a nie tylko innymi nazwami. Pola customowe per typ sprzętu są oddzielone od bazowej klasy
* Dostęp do danych jest uogólniony, poprzez zastosowanie metod generycznych w Database dodanie nowego typu sprzętu nie jest aż tak problematyczne (przynajmniej z perspektywy bazy danych)

## Podział klas

* Warstwa danych - Database.cs
* Warstwa modelu - Gear, User, oraz klasy w folderach UserTypes i GearTypes
* Warstwa serwisowa - Service.cs oraz klasa Limits

Zastosowałem taki podział w celu zwiększenia czytelności, całe ui znajduje się w jednej klasie podczas gdy reszta klas to w zasadzie tylko klasy modeli danych. Klasa database jest kompletnie wydzielona ze względu na to że jest wykorzystywana przez wiele klas

