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

## Podział branchy

* dev - branch na którym pisałem kod
* test - większość początkowych testów, później pełny test systemu
* master - branch produkcyjny, nie ruszany do ostatecznego merga

## Uruchomienie

1. Kompilacja projektu
2. Jeśli folder Database wraz z zawartością się nie skopiował sam do root folderu skompilowanej aplikacji należy go skopiować
3. Uruchom program

*Dane logowania użytkowników znajdują się w pliku `users.json`*