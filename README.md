# ViolaJonesDetektor

Aplikacija za detekciju lica korištenjem Viola-Jones algoritma.

## Pokretanje jednonitnog detektora

Pokretanjem *FaceDetection.exe* file-a koji se nalazi u folderu *FaceDetection\FaceDetection\bin\Release*

## Pokretanje višenitnog detektora

Pokretanjem *FaceDetection.exe* file-a koji se nalazi u folderu *FaceDetection Multithreading\FaceDetection\bin\Release*

## Mogućnosti detektora

1. Vršenje treninga detektora na osnovu slika koja predstavljaju lica;
2. Vršenje treninga detektora na osnovu slika koja ne predstavljaju lica;
3. Vršenje detekcije lica na jednoj slici;
4. Vršenje detekcije lica na više slika;
5. Poboljšavanje tačnosti detektora na osnovu rezultata detekcije lica na jednoj slici;
6. Prikaz statističkih podataka o detekciji.

## Dodatne informacije o detektoru

Sve akcije vrše se isključivo nad **.jpg** slikama.
Detektor može vršiti detekciju lica koja su u frontalnoj poziciji, bez rotacije.
Na detekciju ne utječu ni osvjetljenje, ni nijanse boja slike.
Najmanja veličina za vršenje detekcije je 24x24 piksela.
Kriterij za uspješnost je 90% (9 pozitivnih i 1 negativna povratna informacija).
Za utvrđivanje postojanja lica korištene su 4 vrste Haar-karakteristika:
1. Karakteristika nosa;
2. Karakteristika obrve;
3. Karakteristika oka;
4. Karakteristika okvira lica.

*ETF Sarajevo, 2018.*
