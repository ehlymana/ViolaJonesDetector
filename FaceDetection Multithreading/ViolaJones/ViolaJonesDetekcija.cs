using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;

namespace DetekcijaLica
{
    class HaarKarakteristika
    {
        int suma;
        int snagaKlasifikatora;
        int brojTacnihDetekcija=0;
        int brojNetacnihDetekcija=0;

        public int Suma { get => suma; set => suma = value; }
        public int SnagaKlasifikatora { get => snagaKlasifikatora; set => snagaKlasifikatora = value; }
        public int BrojTacnihDetekcija { get => brojTacnihDetekcija; set => brojTacnihDetekcija = value; }
        public int BrojNetacnihDetekcija { get => brojNetacnihDetekcija; set => brojNetacnihDetekcija = value; }

        public HaarKarakteristika (int s, int snaga, int tacne, int netacne)
        {
            Suma = s;
            SnagaKlasifikatora = snaga;
            BrojTacnihDetekcija = tacne;
            BrojNetacnihDetekcija = netacne;
        }
    }
    public static class ViolaJonesDetekcija
    {
        static double kriterijZaDetekcijuNos = 0.25;
        static double kriterijZaDetekcijuObrva = 0.5;
        static double kriterijZaDetekcijuOko = 0.55;
        static double kriterijZaDetekcijuOkvir = 0.34;
        static double kriterijZaTacnost = 0.9;
        static List<HaarKarakteristika> haarKarakteristikeLica1 = new List<HaarKarakteristika>(); //karakteristike za nos
        static List<HaarKarakteristika> haarKarakteristikeLica2 = new List<HaarKarakteristika>(); //karakteristike za obrvu
        static List<HaarKarakteristika> haarKarakteristikeLica3 = new List<HaarKarakteristika>(); //karakteristike za oko
        static List<HaarKarakteristika> haarKarakteristikeLica4 = new List<HaarKarakteristika>(); //karakteristike za okvir lica
        static List<int[,]> matricneVerzijeSlika = new List<int[,]>();
        static int brojUspjesnihDetekcija = 0, brojNeuspjesnihDetekcija = 0;
        static int[,] integralnaSlika;
        public static void TreningLica(string direktorij, BackgroundWorker progres)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int postoOstalo = 100;
            #region Uzimanje Svih Slika Iz Direktorija
            //kako bi se olakšalo korisniku, sve slike za trening se nalaze u jednom direktoriju
            //u ovom dijelu koda sve slike se preuzimaju kako bi se mogao izvršiti trening
            //napomena: sve slike predstavljaju lica
            string[] lokacijeSlika = System.IO.Directory.GetFiles(direktorij);
            List<Bitmap> slike = new List<Bitmap>();
            Parallel.ForEach(lokacijeSlika, (lokacija) =>
            //foreach(string lokacija in lokacijeSlika)
           {
               slike.Add(new Bitmap(lokacija));
           });
            #endregion
            #region Pretvaranje RGB Slika u Crno-Bijele Slike
            //sve slike u boji je potrebno normalizovati
            //to se vrši u sljedećem dijelu koda
            //foreach(Bitmap slika in slike)
            Parallel.ForEach(slike, (slika) =>
           {
               if (slika != null)
               {
                   int[,] matrica = new int[slika.Height, slika.Width];
                   for (int x = 0; x < slika.Height; x++)
                   {
                       for (int y = 0; y < slika.Width; y++)
                       {
                           Color c = slika.GetPixel(y, x);
                           int gs = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                           matrica[x, y] = gs;
                           slika.SetPixel(y, x, Color.FromArgb(gs, gs, gs));
                       }
                   }
                   matricneVerzijeSlika.Add(matrica);
               }
           });
            #endregion
            progres.ReportProgress(10);
            postoOstalo -= 10;
            int postotak = 90 / slike.Count;
            if (postotak == 0) postotak = 1;
            #region Pronađi i Spremi Haar Karakteristike
            //ovisno od toga da li su slike lica (ili nisu), računaju se Haar karakteristike za svaki 24x24 isječak slike
            //provjerava se da li je Haar karakteristika već pronađena
            //ukoliko nije, dodaje se u listu Haar karakteristika
            //ažuriraju se pozitivni uzorci (ukoliko je u pitanju lice) i negativni uzorci (ukoliko nije lice)
            foreach (int[,] slika in matricneVerzijeSlika)
            {
                //prvo je potrebno odrediti koliko puta će se vršiti skaliranje ovisno o veličini slike
                //dimenzije se dijele s 24 jer je osnovna veličina prozora 24 x 24
                double kolikoJePutaVisinaVeća = slika.GetLength(0) / 24 - 1;
                double kolikoJePutaŠirinaVeća = slika.GetLength(1) / 24 - 1;
                if (kolikoJePutaVisinaVeća > 5) kolikoJePutaVisinaVeća = 5;
                if (kolikoJePutaŠirinaVeća > 5) kolikoJePutaŠirinaVeća = 5;
                bool skaliranje = true;
                double prvaVeličina = 1;
                //ukoliko je slika manjih dimenzija od 24x24 skaliranje se neće vršiti
                IzracunajIntegralnuSliku(slika);
                do
                {
                    double visinaProzora = prvaVeličina * 24;
                    double širinaProzora = prvaVeličina * 24;
                    //sada se vrši ekstrakcija svih ovakvih prostora i računanje 5 Haar karakteristika za svaki prozor
                    Parallel.For(0, (int)(slika.GetLength(0) - visinaProzora - 1), i =>
                    //for (int i = 0; i < slika.GetLength(0) - visinaProzora - 1; i += 24)
                    {
                        if (i % 24 == 0)
                        {
                            Parallel.For(0, (int)(slika.GetLength(1) - širinaProzora - 1), j =>
                            //for (int j = 0; j < slika.GetLength(1) - širinaProzora - 1; j += 24)
                            {
                                if (j % 24 == 0)
                                {
                                    Tuple<double, double> sume1 = null, sume2 = null, sume3 = null, sume4 = null;
                                    Parallel.Invoke(
                                    () =>
                                    sume1 = IzračunajHK1(slika, (int)i, j, prvaVeličina * 24),
                                    () =>
                                    sume2 = IzračunajHK2(slika, (int)i, j, prvaVeličina * 24),
                                    () => 
                                    sume3 = IzračunajHK3(slika, (int)i, j, prvaVeličina * 24),
                                    () => 
                                    sume4 = IzračunajHK4(slika, (int)i, j, prvaVeličina * 24)
                                    );
                                    HaarKarakteristika karakteristika;
                                    if (sume1.Item1 - sume1.Item2 > kriterijZaDetekcijuNos * sume1.Item1)
                                    {
                                        karakteristika = PronađiHK((int)(sume1.Item1 - sume1.Item2), haarKarakteristikeLica1); //provjeravamo da li smo već prije spremili istu karakteristiku
                                        SpremiHK(karakteristika, (int)(sume1.Item1 - sume1.Item2), haarKarakteristikeLica1);
                                    }
                                    if (sume2.Item1 - sume2.Item2 > kriterijZaDetekcijuObrva * sume2.Item1)
                                    {
                                        karakteristika = PronađiHK((int)(sume2.Item1 - sume2.Item2), haarKarakteristikeLica2); //provjeravamo da li smo već prije spremili istu karakteristiku
                                        SpremiHK(karakteristika, (int)(sume2.Item1 - sume2.Item2), haarKarakteristikeLica2);
                                    }
                                    if (sume3.Item2 - sume3.Item1 > kriterijZaDetekcijuOko * sume3.Item2)
                                    {
                                        karakteristika = PronađiHK((int)(sume3.Item2 - sume3.Item1), haarKarakteristikeLica3); //provjeravamo da li smo već prije spremili istu karakteristiku
                                        SpremiHK(karakteristika, (int)(sume3.Item2 - sume3.Item1), haarKarakteristikeLica3);
                                    }
                                    if (sume4.Item1 - sume4.Item2 > kriterijZaDetekcijuOkvir * sume4.Item1)
                                    {
                                        karakteristika = PronađiHK((int)(sume4.Item1 - sume4.Item2), haarKarakteristikeLica4); //provjeravamo da li smo već prije spremili istu karakteristiku
                                        SpremiHK(karakteristika, (int)(sume4.Item1 - sume4.Item2), haarKarakteristikeLica4);
                                    }
                                }
                            });
                        }
                        });
                    //vršimo naredno skaliranje ovisno od toga koliko puta je slika veća od dimenzija 24x24
                    kolikoJePutaVisinaVeća -= 1;
                    kolikoJePutaŠirinaVeća -= 1;
                    skaliranje = (kolikoJePutaVisinaVeća > 0 && kolikoJePutaŠirinaVeća > 0);
                    prvaVeličina += 0.1;
                }
                while (skaliranje);
                if (postoOstalo > 0) progres.ReportProgress(postotak);
                postoOstalo -= postotak;
            }
            #endregion
            if (postoOstalo>0) progres.ReportProgress(postoOstalo);
            matricneVerzijeSlika.Clear();
            integralnaSlika = null;
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            progres.ReportProgress(101, trajanje);
        }

        public static void TreningNeLica(string direktorij, BackgroundWorker progres)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int postoOstalo = 100;
            #region Uzimanje Svih Slika Iz Direktorija
            //kako bi se olakšalo korisniku, sve slike za trening se nalaze u jednom direktoriju
            //u ovom dijelu koda sve slike se preuzimaju kako bi se mogao izvršiti trening
            //napomena: sve slike predstavljaju lica
            string[] lokacijeSlika = System.IO.Directory.GetFiles(direktorij);
            List<Bitmap> slike = new List<Bitmap>();
            Parallel.ForEach(lokacijeSlika, (lokacija) =>
            {
                slike.Add(new Bitmap(lokacija));
            });
            #endregion
            #region Pretvaranje RGB Slika u Crno-Bijele Slike
            //sve slike u boji je potrebno normalizovati
            //to se vrši u sljedećem dijelu koda
            Parallel.ForEach(slike, (slika) =>
            {
                if (slika != null)
                {
                    int[,] matrica = new int[slika.Height, slika.Width];
                    for (int x = 0; x < slika.Height; x++)
                    {
                        for (int y = 0; y < slika.Width; y++)
                        {
                            Color c = slika.GetPixel(y, x);
                            int gs = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                            matrica[x, y] = gs;
                            slika.SetPixel(y, x, Color.FromArgb(gs, gs, gs));
                        }
                    }
                    matricneVerzijeSlika.Add(matrica);
                }
            });
            #endregion
            progres.ReportProgress(10);
            postoOstalo -= 10;
            int postotak = 90 / slike.Count;
            #region Ažuriranje Karakteristika Lica Na Slici
            //ukoliko je ista slika već korištena za trening/detekciju, potrebno je ažurirati
            //tačnost klasifikatora - budući da je pogrešno detektovano, potrebno je oslabiti klasifikatore
            //koji su doveli do toga da je izvršena pogrešna detekcija
            //ako ne klasifikatori se spremaju kao i u prethodnim slučajevima
            foreach (int[,] matrica in matricneVerzijeSlika)
            {
                double kolikoJePutaVisinaVeća = matrica.GetLength(0) / 24 - 1;
                double kolikoJePutaŠirinaVeća = matrica.GetLength(1) / 24 - 1;
                if (kolikoJePutaVisinaVeća > 5) kolikoJePutaVisinaVeća = 5;
                if (kolikoJePutaŠirinaVeća > 5) kolikoJePutaŠirinaVeća = 5;
                bool skaliranje = true;
                double prvaVeličina = 1;
                IzracunajIntegralnuSliku(matrica);
                progres.ReportProgress(postotak);
                postoOstalo -= postotak;
                do
                {
                    double visinaProzora = prvaVeličina * 24;
                    double širinaProzora = prvaVeličina * 24;
                    //sada se vrši pokušaj pronalaska poznatih Haar karakteristika za sliku
                    Parallel.For(0, (int)(matrica.GetLength(0) - visinaProzora - 1), i =>
                    //for (int i = 0; i < matrica.GetLength(0) - visinaProzora - 1; i += 24)
                    {
                        if (i % 24 == 0)
                        {
                            Parallel.For(0, (int)(matrica.GetLength(1) - širinaProzora - 1), j =>
                            //for (int j = 0; j < matrica.GetLength(1) - širinaProzora - 1; j += 24)
                            {
                                if (j % 24 == 0)
                                {
                                    Tuple<double, double> sume1 = null, sume2 = null, sume3 = null, sume4 = null;
                                    Parallel.Invoke(
                                        () => sume1 = IzračunajHK1(matrica, (int)i, j, prvaVeličina * 24),
                                        () => sume2 = IzračunajHK2(matrica, (int)i, j, prvaVeličina * 24),
                                        () => sume3 = IzračunajHK3(matrica, (int)i, j, prvaVeličina * 24),
                                        () => sume4 = IzračunajHK4(matrica, (int)i, j, prvaVeličina * 24)
                                        );
                                    HaarKarakteristika karakteristika1 = null, karakteristika2 = null, karakteristika3 = null, karakteristika4 = null;
                                    Parallel.Invoke(
                                        () => karakteristika1 = PronađiHK((int)(sume1.Item1 - sume1.Item2), haarKarakteristikeLica1),
                                        () => karakteristika2 = PronađiHK((int)(sume2.Item1 - sume2.Item2), haarKarakteristikeLica2),
                                        () => karakteristika3 = PronađiHK((int)(sume3.Item2 - sume3.Item1), haarKarakteristikeLica3),
                                        () => karakteristika4 = PronađiHK((int)(sume4.Item1 - sume4.Item2), haarKarakteristikeLica4)
                                        );
                                    Parallel.Invoke(
                                    () =>
                                    {
                                        if (karakteristika1 != null)
                                        {
                                            SpremiHK(karakteristika1, haarKarakteristikeLica1, false);
                                        }
                                    },
                                    () =>
                                    {
                                        if (karakteristika2 != null)
                                        {
                                            SpremiHK(karakteristika2, haarKarakteristikeLica2, false);
                                        }
                                    },
                                    () =>
                                    {
                                        if (karakteristika3 != null)
                                        {
                                            SpremiHK(karakteristika3, haarKarakteristikeLica3, false);
                                        }
                                    },
                                    () =>
                                    {
                                        if (karakteristika4 != null)
                                        {
                                            SpremiHK(karakteristika4, haarKarakteristikeLica4, false);
                                        }
                                    }
                                    );
                                }
                            });
                        }
                    });
                    //vršimo naredno skaliranje ovisno od toga koliko puta je slika veća od dimenzija 24x24
                    kolikoJePutaVisinaVeća -= 1;
                    kolikoJePutaŠirinaVeća -= 1;
                    skaliranje = (kolikoJePutaVisinaVeća > 0 && kolikoJePutaŠirinaVeća > 0);
                    prvaVeličina += 0.1;
                }
                while (skaliranje);
            }
            //petlja se završava ukoliko se izvrše sva skaliranja i ne nađu se sve distinktivne karakteristike lica
            //ili se prije toga pronađu sve distinktivne karakteristike lica
            #endregion
            progres.ReportProgress(postoOstalo);
            matricneVerzijeSlika.Clear();
            integralnaSlika = null;
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            progres.ReportProgress(101, trajanje);
        }

        public static bool Detekcija(Bitmap slika, BackgroundWorker progres)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int postoOstalo = 100;
            #region Pretvaranje RGB Slike u Crno-Bijelu Sliku
            int[,] matrica = new int[slika.Height, slika.Width];
            //Parallel.For(0, slika.Height, x =>
            for (int x = 0; x < slika.Height; x++)
            {
                //Parallel.For(0, slika.Width, y =>
                for (int y = 0; y < slika.Width; y++)
                {
                    Color c = slika.GetPixel(y, x);
                    int gs = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    matrica[x, y] = gs;
                    slika.SetPixel(y, x, Color.FromArgb(gs, gs, gs));
                }//);
            }//);
            #endregion
            if (progres!=null) progres.ReportProgress(10);
            postoOstalo -= 10;
            #region Formiranje Snažnih Klasifikatora
            //formiramo liste koje se sastoje samo od onih Haar karakteristika koje su pokazale dobre rezultate
            //odnosno, koje su ostvarile veliki broj ispravnih detekcija
            List<HaarKarakteristika> klasifikatoriLica1 = new List<HaarKarakteristika>();
            List<HaarKarakteristika> klasifikatoriLica2 = new List<HaarKarakteristika>();
            List<HaarKarakteristika> klasifikatoriLica3 = new List<HaarKarakteristika>();
            List<HaarKarakteristika> klasifikatoriLica4 = new List<HaarKarakteristika>();
            Parallel.Invoke(
                () => kopirajListu(klasifikatoriLica1, haarKarakteristikeLica1),
                () => kopirajListu(klasifikatoriLica2, haarKarakteristikeLica2),
                () => kopirajListu(klasifikatoriLica3, haarKarakteristikeLica3),
                () => kopirajListu(klasifikatoriLica4, haarKarakteristikeLica4)
            );
            //Parallel.ForEach(klasifikatoriLica1, klasifikator =>
            foreach (var klasifikator in haarKarakteristikeLica1)
            {
                if (klasifikator!=null && klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija > 0 && (double)klasifikator.BrojTacnihDetekcija / (double)(klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija) < kriterijZaTacnost)
                {
                    HaarKarakteristika zaBrisanje = klasifikatoriLica1.Find(k => k!=null && k.Suma == klasifikator.Suma);
                    klasifikatoriLica1.Remove(zaBrisanje);
                }
            }//);
            //Parallel.ForEach(klasifikatoriLica2, klasifikator =>
            foreach (var klasifikator in haarKarakteristikeLica2)
            {
                if (klasifikator != null && klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija > 0 && (double)klasifikator.BrojTacnihDetekcija / (double)(klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija) < kriterijZaTacnost)
                {
                    HaarKarakteristika zaBrisanje = klasifikatoriLica2.Find(k => k != null && k.Suma == klasifikator.Suma);
                    klasifikatoriLica2.Remove(zaBrisanje);
                }
            }//);
            //Parallel.ForEach(klasifikatoriLica3, klasifikator =>
            foreach (var klasifikator in haarKarakteristikeLica3)
            {
                if (klasifikator != null && klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija > 0 && (double)klasifikator.BrojTacnihDetekcija / (double)(klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija) < kriterijZaTacnost)
                {
                    HaarKarakteristika zaBrisanje = klasifikatoriLica3.Find(k => k != null && k.Suma == klasifikator.Suma);
                    klasifikatoriLica3.Remove(zaBrisanje);
                }
            }//);
            //Parallel.ForEach(klasifikatoriLica4, klasifikator =>
            foreach (var klasifikator in haarKarakteristikeLica4)
            {
                if (klasifikator != null && klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija > 0 && (double)(klasifikator.BrojTacnihDetekcija) / (double)(klasifikator.BrojTacnihDetekcija + klasifikator.BrojNetacnihDetekcija) < kriterijZaTacnost)
                {
                    HaarKarakteristika zaBrisanje = klasifikatoriLica4.Find(k => k != null && k.Suma == klasifikator.Suma);
                    klasifikatoriLica4.Remove(zaBrisanje);
                }
            }//);
            #endregion
            if (progres!=null) progres.ReportProgress(40);
            postoOstalo -= 40;
            #region Pokušaj Pronalaska Karakteristika Lica Na Slici
            double kolikoJePutaVisinaVeća = matrica.GetLength(0) / 24 - 1;
            double kolikoJePutaŠirinaVeća = matrica.GetLength(1) / 24 - 1;
            if (kolikoJePutaVisinaVeća > 5) kolikoJePutaVisinaVeća = 5;
            if (kolikoJePutaŠirinaVeća > 5) kolikoJePutaŠirinaVeća = 5;
            bool skaliranje = true;
            double prvaVeličina = 1;
            IzracunajIntegralnuSliku(matrica);
            if (progres!=null) progres.ReportProgress(30);
            postoOstalo -= 30;
            int nos = 0, obrva = 0, oko = 0, okvirLica = 0;
            do
            {
                double visinaProzora = prvaVeličina * 24;
                double širinaProzora = prvaVeličina * 24;
                //sada se vrši pokušaj pronalaska poznatih Haar karakteristika za sliku
                Parallel.For(0, (int)(matrica.GetLength(0) - visinaProzora - 1), i =>
                //for (int i = 0; i < matrica.GetLength(0) - visinaProzora - 1; i += 24)
                {
                    if (i % 24 == 0 && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1))
                    {
                        Parallel.For(0, (int)(matrica.GetLength(1) - širinaProzora - 1), j =>
                        //for (int j = 0; j < matrica.GetLength(1) - širinaProzora - 1; j += 24)
                        {
                            if (j % 24 == 0 && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1))
                            {
                                Tuple<double, double> sume1 = null, sume2 = null, sume3 = null, sume4 = null;
                                Parallel.Invoke(
                                 () =>
                                sume1 = IzračunajHK1(matrica, (int)i, j, prvaVeličina * 24),
                                 () =>
                                sume2 = IzračunajHK2(matrica, (int)i, j, prvaVeličina * 24),
                                 () =>
                                sume3 = IzračunajHK3(matrica, (int)i, j, prvaVeličina * 24),
                                 () =>
                                sume4 = IzračunajHK4(matrica, (int)i, j, prvaVeličina * 24)
                                );
                                HaarKarakteristika karakteristika1=null, karakteristika2=null, karakteristika3=null, karakteristika4=null;
                                Parallel.Invoke(
                                    () =>
                                    {
                                        karakteristika1 = PronađiHK((int)(sume1.Item1 - sume1.Item2), klasifikatoriLica1);
                                    },
                                    () =>
                                    {
                                        karakteristika2 = PronađiHK((int)(sume2.Item1 - sume2.Item2), klasifikatoriLica2);
                                    },
                                    () =>
                                    {
                                        karakteristika3 = PronađiHK((int)(sume3.Item2 - sume3.Item1), klasifikatoriLica3);
                                    },
                                    () =>
                                    {
                                        karakteristika4 = PronađiHK((int)(sume4.Item1 - sume4.Item2), klasifikatoriLica4);
                                    }
                                );
                                if (karakteristika1 != null)
                                {
                                    nos++; //pronađen je nos na slici
                                }
                                if (karakteristika2 != null)
                                {
                                    obrva++; //pronađena je obrva na slici
                                }
                                if (karakteristika3 != null)
                                {
                                    oko++; //pronađeno je oko na slici
                                }
                                if (karakteristika4 != null)
                                {
                                    okvirLica++; //pronađen je okvir lica na slici
                                }
                            }
                        });
                    }
                });
                //vršimo naredno skaliranje ovisno od toga koliko puta je slika veća od dimenzija 24x24
                kolikoJePutaVisinaVeća -= 1;
                kolikoJePutaŠirinaVeća -= 1;
                skaliranje = (kolikoJePutaVisinaVeća > 0 && kolikoJePutaŠirinaVeća > 0);
                prvaVeličina += 0.1;
            }
            while (skaliranje && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1));
            //petlja se završava ukoliko se izvrše sva skaliranja i ne nađu se sve distinktivne karakteristike lica
            //ili se prije toga pronađu sve distinktivne karakteristike lica
            #endregion 
            if (progres!=null) progres.ReportProgress(20);
            postoOstalo -= 20;
            if (progres!=null) progres.ReportProgress(postoOstalo);
            matricneVerzijeSlika.Clear();
            integralnaSlika = null;
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            if (progres != null) progres.ReportProgress(102, trajanje);
            return (nos > 0 && obrva > 1 && oko > 1 && okvirLica > 0);
        }

        public static void DetekcijaIzvjestaj(string direktorij, BackgroundWorker progres)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int postoOstalo = 100;
            #region Uzimanje Svih Slika Iz Direktorija
            //kako bi se olakšalo korisniku, sve slike za trening se nalaze u jednom direktoriju
            //u ovom dijelu koda sve se preuzimaju kako bi se izvršio trening
            //napomena: sve slike ili predstavljaju, ili ne predstavljaju lica (a ne pojedinačno)
            string[] lokacijeSlika = System.IO.Directory.GetFiles(direktorij);
            List<Bitmap> slike = new List<Bitmap>();
            //Parallel.ForEach(lokacijeSlika, (lokacija) =>
            foreach (string lokacija in lokacijeSlika)
           {
               slike.Add(new Bitmap(lokacija));
           }//);
            #endregion
            StreamWriter file = File.CreateText(Directory.GetCurrentDirectory()+"/rezultati.txt");
            file.WriteLine("Broj slika: " + slike.Count.ToString());
            for (int i=0; i<slike.Count; i++)
            {
               bool rezultat = Detekcija(slike.ElementAt(i), null);
               string red = "Slika: " + lokacijeSlika[i] + " Rezultat: ";
               if (rezultat) red += "Pronađeno lice\n";
               else red += "Nije pronađeno lice\n";
               file.WriteLine(red);
               progres.ReportProgress(100 / slike.Count);
               postoOstalo -= 100 / slike.Count;
           }
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            progres.ReportProgress(postoOstalo);
            progres.ReportProgress(103, trajanje);
            file.Close();
        }

        public static void IzvrsiPoboljsanje (bool tacnostDetekcije, Bitmap slika, bool lice, BackgroundWorker progres)
        {
            if (tacnostDetekcije) brojUspjesnihDetekcija++;
            else
            {
                brojNeuspjesnihDetekcija++;
                AzuriranjeKarakteristika(slika, tacnostDetekcije, lice, progres);
            }
        }

        static void AzuriranjeKarakteristika(Bitmap slika, bool tacnost, bool lice, BackgroundWorker progres) //ažuriramo tačnost za svaku pronađenu HK
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int postoOstalo = 100;
            #region Pretvaranje RGB Slike u Crno-Bijelu Sliku
            int[,] matrica = new int[slika.Height, slika.Width];
            for (int x = 0; x < slika.Height; x++)
            {
                for (int y = 0; y < slika.Width; y++)
                {
                    Color c = slika.GetPixel(y, x);
                    int gs = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    matrica[x, y] = gs;
                    slika.SetPixel(y, x, Color.FromArgb(gs, gs, gs));
                }
            }
            #endregion
            progres.ReportProgress(25);
            postoOstalo -= 25;
            #region Ažuriranje Karakteristika Lica Na Slici
            //ukoliko je ista slika već korištena za trening/detekciju, potrebno je ažurirati
            //tačnost klasifikatora - budući da je pogrešno detektovano, potrebno je oslabiti klasifikatore
            //koji su doveli do toga da je izvršena pogrešna detekcija
            //ako ne klasifikatori se spremaju kao i u prethodnim slučajevima
            double kolikoJePutaVisinaVeća = matrica.GetLength(0) / 24 - 1;
            double kolikoJePutaŠirinaVeća = matrica.GetLength(1) / 24 - 1;
            if (kolikoJePutaVisinaVeća > 5) kolikoJePutaVisinaVeća = 5;
            if (kolikoJePutaŠirinaVeća > 5) kolikoJePutaŠirinaVeća = 5;
            bool skaliranje = true;
            double prvaVeličina = 1;
            IzracunajIntegralnuSliku(matrica);
            progres.ReportProgress(25);
            postoOstalo -= 25;
            int nos = 0;
            int obrva = 0;
            int oko = 0;
            int okvirLica = 0;
            do
            {
                double visinaProzora = prvaVeličina * 24;
                double širinaProzora = prvaVeličina * 24;
                //sada se vrši pokušaj pronalaska poznatih Haar karakteristika za sliku
                Parallel.For(0, (int)(matrica.GetLength(0) - visinaProzora - 1), i =>
                //for (int i = 0; i < matrica.GetLength(0) - visinaProzora - 1; i += 24)
                {
                    if (i % 24 == 0)
                    {
                        Parallel.For(0, (int)(matrica.GetLength(1) - širinaProzora - 1), j =>
                        //for (int j = 0; j < matrica.GetLength(1) - širinaProzora - 1; j += 24)
                        {
                            if (j % 24 == 0)
                            {
                                Tuple<double, double> sume = IzračunajHK1(matrica, i, j, prvaVeličina * 24);
                                var karakteristika = PronađiHK((int)(sume.Item1 - sume.Item2), haarKarakteristikeLica1);
                                if (karakteristika != null)
                                {
                                    SpremiHK(karakteristika, haarKarakteristikeLica1, !lice);
                                    nos++;
                                }
                                else if (karakteristika != null) nos++;
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuNos * sume.Item1 && !lice)
                                {
                                    SpremiHK(karakteristika, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica1);
                                    nos++;
                                }
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuNos * sume.Item1) nos++;
                                sume = IzračunajHK2(matrica, i, j, prvaVeličina * 24);
                                karakteristika = PronađiHK((int)(sume.Item1 - sume.Item2), haarKarakteristikeLica2);
                                if (karakteristika != null)
                                {
                                    SpremiHK(karakteristika, haarKarakteristikeLica2, !lice);
                                    obrva++;
                                }
                                else if (karakteristika != null) obrva++;
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuObrva * sume.Item1 && !lice)
                                {
                                    SpremiHK(karakteristika, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica2);
                                    obrva++;
                                }
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuObrva * sume.Item1) obrva++;
                                sume = IzračunajHK3(matrica, i, j, prvaVeličina * 24);
                                karakteristika = PronađiHK((int)(sume.Item2 - sume.Item1), haarKarakteristikeLica3);
                                if (karakteristika != null)
                                {
                                    SpremiHK(karakteristika, haarKarakteristikeLica3, !lice);
                                    oko++;
                                }
                                else if (karakteristika != null) oko++;
                                else if (sume.Item2 - sume.Item1 > kriterijZaDetekcijuOko * sume.Item2 && !lice)
                                {
                                    SpremiHK(karakteristika, (int)(sume.Item2 - sume.Item1), haarKarakteristikeLica3);
                                    oko++;
                                }
                                else if (sume.Item2 - sume.Item1 > kriterijZaDetekcijuOko * sume.Item2) oko++;
                                sume = IzračunajHK4(matrica, i, j, prvaVeličina * 24);
                                karakteristika = PronađiHK((int)(sume.Item1 - sume.Item2), haarKarakteristikeLica4);
                                if (karakteristika != null)
                                {
                                    SpremiHK(karakteristika, haarKarakteristikeLica4, !lice);
                                    okvirLica++;
                                }
                                else if (karakteristika != null) okvirLica++;
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuOkvir * sume.Item1 && !lice)
                                {
                                    SpremiHK(karakteristika, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica4);
                                    okvirLica++;
                                }
                                else if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuOkvir * sume.Item1) okvirLica++;
                            }
                        });
                    }
                });
                //vršimo naredno skaliranje ovisno od toga koliko puta je slika veća od dimenzija 24x24
                kolikoJePutaVisinaVeća -= 1;
                kolikoJePutaŠirinaVeća -= 1;
                skaliranje = (kolikoJePutaVisinaVeća > 0 && kolikoJePutaŠirinaVeća > 0);
                prvaVeličina += 0.1;
            }
            while (skaliranje);
            //petlja se završava ukoliko se izvrše sva skaliranja i ne nađu se sve distinktivne karakteristike lica
            //ili se prije toga pronađu sve distinktivne karakteristike lica
            #endregion
            progres.ReportProgress(25);
            postoOstalo -= 25;
            #region Ažuriranje Kriterija
            if (!tacnost && !lice) //pogriješio a nije detektovao lice - kriterij treba biti blaži
            {
                bool nadeno = false;
                double kriterij = 0.95; //za početak tražimo Haar-karakteristike koje su +/-5% u odnosu na postavljene
                //kriterije
                while (!nadeno && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1))
                {
                    kolikoJePutaVisinaVeća = matrica.GetLength(0) / 24 - 1;
                    kolikoJePutaŠirinaVeća = matrica.GetLength(1) / 24 - 1;
                    if (kolikoJePutaVisinaVeća > 5) kolikoJePutaVisinaVeća = 5;
                    if (kolikoJePutaŠirinaVeća > 5) kolikoJePutaŠirinaVeća = 5;
                    skaliranje = true;
                    prvaVeličina = 1;
                    do
                    {
                        double visinaProzora = prvaVeličina * 24;
                        double širinaProzora = prvaVeličina * 24;
                        //sada se vrši pokušaj pronalaska poznatih Haar karakteristika za sliku
                        Parallel.For(0, (int)(matrica.GetLength(0) - visinaProzora - 1), i =>
                        //for (int i = 0; i < matrica.GetLength(0) - visinaProzora - 1; i += 24)
                        {
                            if (i % 24 == 0 && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1))
                            {
                                Parallel.For(0, (int)(matrica.GetLength(1) - širinaProzora - 1), j =>
                                //for (int j = 0; j < matrica.GetLength(1) - širinaProzora - 1; j += 24)
                                {
                                    if (j % 24 == 0 && (nos < 1 || obrva < 2 || oko < 2 || okvirLica < 1))
                                    {
                                        if (nos < 1)
                                        {
                                            Tuple<double, double> sume = IzračunajHK1(matrica, i, j, prvaVeličina * 24);
                                            if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuNos * sume.Item1 * kriterij)
                                            {
                                                SpremiHK(null, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica1);
                                                nadeno = true;
                                            }
                                        }
                                        else if (obrva < 2)
                                        {
                                            Tuple<double, double> sume = IzračunajHK2(matrica, i, j, prvaVeličina * 24);
                                            if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuObrva * sume.Item1 * kriterij)
                                            {
                                                SpremiHK(null, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica2);
                                                if (obrva == 0) obrva++;
                                                else nadeno = true;
                                            }
                                        }
                                        else if (oko < 2)
                                        {
                                            Tuple<double, double> sume = IzračunajHK3(matrica, i, j, prvaVeličina * 24);
                                            if (sume.Item2 - sume.Item1 > kriterijZaDetekcijuOko * sume.Item2 * kriterij)
                                            {
                                                SpremiHK(null, (int)(sume.Item2 - sume.Item1), haarKarakteristikeLica3);
                                                if (oko == 0) oko++;
                                                else nadeno = true;
                                            }
                                        }
                                        else if (okvirLica < 1)
                                        {
                                            Tuple<double, double> sume = IzračunajHK4(matrica, i, j, prvaVeličina * 24);
                                            if (sume.Item1 - sume.Item2 > kriterijZaDetekcijuOkvir * sume.Item1 * kriterij)
                                            {
                                                SpremiHK(null, (int)(sume.Item1 - sume.Item2), haarKarakteristikeLica4);
                                                nadeno = true;
                                            }
                                        }
                                    }
                                });
                            }
                        });
                        //vršimo naredno skaliranje ovisno od toga koliko puta je slika veća od dimenzija 24x24
                        kolikoJePutaVisinaVeća -= 1;
                        kolikoJePutaŠirinaVeća -= 1;
                        skaliranje = (kolikoJePutaVisinaVeća > 0 && kolikoJePutaŠirinaVeća > 0);
                        prvaVeličina += 0.1;
                    }
                    while (skaliranje);
                    kriterij -= 0.05;
                }
                if (nos < 1) kriterijZaDetekcijuNos *= kriterij;
                else if (obrva < 2) kriterijZaDetekcijuObrva *= kriterij;
                else if (oko < 2) kriterijZaDetekcijuOko *= kriterij;
                else if (okvirLica < 1) kriterijZaDetekcijuOkvir *= kriterij;
            }
            else if (!tacnost && lice)
            { //da se ubuduće ne bi pronalazile Haar-karakteristike na ovakvim slikama, kriterij se povećava
                if (nos > 0) kriterijZaDetekcijuNos *= 1.05;
                else if (obrva > 1) kriterijZaDetekcijuObrva *= 1.05;
                else if (oko > 1) kriterijZaDetekcijuOko *= 1.05;
                else if (okvirLica > 0) kriterijZaDetekcijuOkvir *= 1.05;
            }
            #endregion
            progres.ReportProgress(25);
            postoOstalo -= 25;
            matricneVerzijeSlika.Clear();
            integralnaSlika = null;
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            progres.ReportProgress(postoOstalo);
            progres.ReportProgress(104, trajanje);
        }

        public static double DajTrenutnuUspjesnost ()
        {
            double b1 = brojUspjesnihDetekcija;
            double b2 = brojUspjesnihDetekcija + brojNeuspjesnihDetekcija;
            if(b2>0) return b1 / b2;
            return 0;
        }

        public static int DajBrojProcesiranihSlika ()
        {
            return brojUspjesnihDetekcija + brojNeuspjesnihDetekcija;
        }

        static void IzracunajIntegralnuSliku (int[,] slika)
        {
            integralnaSlika = new int[slika.GetLength(0), slika.GetLength(1)];
            Parallel.For(0, slika.GetLength(0), i =>
            //for (int i=0; i<slika.GetLength(0); i++)
           {
               Parallel.For(0, slika.GetLength(1), j =>
               //for (int j = 0; j < slika.GetLength(1); j++)
               {
                  //Parallel.For(0, i, k =>
                  for (int k = 0; k <= i; k++)
                   {
                       //Parallel.For(0, j, l =>
                       for (int l = 0; l <= j; l++)
                       {
                           if (integralnaSlika != null) integralnaSlika[i, j] += slika[k, l];
                       }//);
                   }//);
               });
           });
        }

        static Tuple<double, double> IzračunajHK1 (int[,] slika, int i, int j, double skaliranje) //nos
         {
             if (integralnaSlika == null) return new Tuple<double, double>(0, 0);
             int sumaBijelo = 0;
             Tuple<int, int> indexiGL = new Tuple<int, int>(i - 1, j-1);
             Tuple<int, int> indexiGD = new Tuple<int, int>(i-1, (int)(j+skaliranje/2-1));
             Tuple<int, int> indexiDL = new Tuple<int, int>((int)(i + skaliranje - 1), j-1);
             Tuple<int, int> indexiDD = new Tuple<int, int>((int)(i + skaliranje - 1), (int)(j + skaliranje / 2 - 1));
             if (indexiGL.Item1 > -1 && indexiGL.Item2 > -1 && indexiGL.Item1 < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiGL.Item1, indexiGL.Item2];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 > -1 && indexiDD.Item1 < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiDD.Item1, indexiDD.Item2];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 > -1 && indexiGD.Item1 < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiGD.Item1, indexiGD.Item2];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 > -1 && indexiDL.Item1 < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiDL.Item1, indexiDL.Item2];
             int sumaCrno = 0;
             if (indexiGL.Item1 > -1 && indexiGL.Item2+ skaliranje / 2 > -1 && indexiGL.Item1 < slika.GetLength(0) && (int)(indexiGL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiGL.Item1, (int)(indexiGL.Item2+skaliranje/2)];
             if (indexiDD.Item1 > -1 && indexiDD.Item2+ skaliranje / 2 > -1 && indexiDD.Item1 < slika.GetLength(0) && (int)(indexiDD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiDD.Item1, (int)(indexiDD.Item2 + skaliranje / 2)];
             if (indexiGD.Item1 > -1 && indexiGD.Item2+ skaliranje / 2 > -1 && indexiGD.Item1 < slika.GetLength(0) && (int)(indexiGD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiGD.Item1, (int)(indexiGD.Item2 + skaliranje / 2)];
             if (indexiDL.Item1 > -1 && indexiDL.Item2+ skaliranje / 2 > -1 && indexiDL.Item1 < slika.GetLength(0) && (int)(indexiDL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiDL.Item1, (int)(indexiDL.Item2 + skaliranje / 2)];
             return new Tuple<double, double>(sumaCrno, sumaBijelo);
         }

         static Tuple<double, double> IzračunajHK2(int[,] slika, int i, int j, double skaliranje) //obrva
         {
             if (integralnaSlika==null) return new Tuple<double, double>(0, 0);
             int sumaBijelo = 0;
             Tuple<int, int> indexiGL = new Tuple<int, int>(i - 1, j - 1);
             Tuple<int, int> indexiGD = new Tuple<int, int>(i - 1, (int)(j + skaliranje - 1));
             Tuple<int, int> indexiDL = new Tuple<int, int>((int)(i + skaliranje / 2 - 1), j - 1);
             Tuple<int, int> indexiDD = new Tuple<int, int>((int)(i + skaliranje / 2 - 1), (int)(j + skaliranje - 1));
             if (indexiGL.Item1 > -1 && indexiGL.Item2 > -1 && indexiGL.Item1 < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiGL.Item1, indexiGL.Item2];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 > -1 && indexiDD.Item1 < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiDD.Item1, indexiDD.Item2];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 > -1 && indexiGD.Item1 < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiGD.Item1, indexiGD.Item2];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 > -1 && indexiDL.Item1 < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiDL.Item1, indexiDL.Item2];
             int sumaCrno = 0;
             if (indexiGL.Item1 + skaliranje / 2 > -1 && indexiGL.Item2 > -1 && (int)(indexiGL.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaCrno += integralnaSlika[(int)(indexiGL.Item1 + skaliranje / 2), indexiGL.Item2];
             if (indexiDD.Item1 + skaliranje / 2 > -1 && indexiDD.Item2 > -1 && (int)(indexiDD.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaCrno += integralnaSlika[(int)(indexiDD.Item1 + skaliranje / 2), indexiDD.Item2];
             if (indexiGD.Item1 + skaliranje / 2 > -1 && indexiGD.Item2 > -1 && (int)(indexiGD.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaCrno -= integralnaSlika[(int)(indexiGD.Item1 + skaliranje / 2), indexiGD.Item2];
             if (indexiDL.Item1 + skaliranje / 2 > -1 && indexiDL.Item2 > -1 && (int)(indexiDL.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaCrno -= integralnaSlika[(int)(indexiDL.Item1 + skaliranje / 2), indexiDL.Item2];
             return new Tuple<double, double>(sumaCrno, sumaBijelo);
         }

         static Tuple<double, double> IzračunajHK3(int[,] slika, int i, int j, double skaliranje) //oko
         {
             if (integralnaSlika == null) return new Tuple<double, double>(0, 0);
             int sumaBijelo = 0;
             Tuple<int, int> indexiGL = new Tuple<int, int>(i - 1, j - 1);
             Tuple<int, int> indexiGD = new Tuple<int, int>(i - 1, (int)(j + skaliranje / 3 - 1));
             Tuple<int, int> indexiDL = new Tuple<int, int>((int)(i + skaliranje - 1), j - 1);
             Tuple<int, int> indexiDD = new Tuple<int, int>((int)(i + skaliranje - 1), (int)(j + skaliranje / 3 - 1));
             if (indexiGL.Item1 > -1 && indexiGL.Item2 > -1 && indexiGL.Item1 < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiGL.Item1, indexiGL.Item2];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 > -1 && indexiDD.Item1 < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiDD.Item1, indexiDD.Item2];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 > -1 && indexiGD.Item1 < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiGD.Item1, indexiGD.Item2];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 > -1 && indexiDL.Item1 < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiDL.Item1, indexiDL.Item2];
             int sumaCrno = 0;
             if (indexiGL.Item1 > -1 && indexiGL.Item2 + skaliranje / 3 > -1 && indexiGL.Item1 < slika.GetLength(0) && (int)(indexiGL.Item2 + skaliranje / 3) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiGL.Item1, (int)(indexiGL.Item2 + skaliranje / 3)];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 + skaliranje / 3 > -1 && indexiDD.Item1 < slika.GetLength(0) && (int)(indexiDD.Item2 + skaliranje / 3) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiDD.Item1, (int)(indexiDD.Item2 + skaliranje / 3)];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 + skaliranje / 3 > -1 && indexiGD.Item1 < slika.GetLength(0) && (int)(indexiGD.Item2 + skaliranje / 3) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiGD.Item1, (int)(indexiGD.Item2 + skaliranje / 3)];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 + skaliranje / 3 > -1 && indexiDL.Item1 < slika.GetLength(0) && (int)(indexiDL.Item2 + skaliranje / 3) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiDL.Item1, (int)(indexiDL.Item2 + skaliranje / 3)];
             if (indexiGL.Item1 > -1 && indexiGL.Item2 + 2*skaliranje / 3 > -1 && indexiGL.Item1 < slika.GetLength(0) && (int)(indexiGL.Item2 + 2 * skaliranje / 3) < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiGL.Item1, (int)(indexiGL.Item2 + 2*skaliranje / 3)];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 + 2*skaliranje / 3 > -1 && indexiDD.Item1 < slika.GetLength(0) && (int)(indexiDD.Item2 + 2 * skaliranje / 3) < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiDD.Item1, (int)(indexiDD.Item2 + 2*skaliranje / 3)];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 + 2*skaliranje / 3 > -1 && indexiGD.Item1 < slika.GetLength(0) && (int)(indexiGD.Item2 + 2 * skaliranje / 3) < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiGD.Item1, (int)(indexiGD.Item2 + 2*skaliranje / 3)];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 + 2*skaliranje / 3 > -1 && indexiDL.Item1 < slika.GetLength(0) && (int)(indexiDL.Item2 + 2 * skaliranje / 3) < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiDL.Item1, (int)(indexiDL.Item2 + 2*skaliranje / 3)];
             return new Tuple<double, double>(sumaCrno, sumaBijelo);
         }

         static Tuple<double, double> IzračunajHK4(int[,] slika, int i, int j, double skaliranje) //okvir lica
         {
             if (integralnaSlika == null) return new Tuple<double, double>(0, 0);
             int sumaBijelo = 0, sumaCrno=0;
             Tuple<int, int> indexiGL = new Tuple<int, int>(i - 1, j - 1);
             Tuple<int, int> indexiGD = new Tuple<int, int>(i - 1, (int)(j + skaliranje / 2 - 1));
             Tuple<int, int> indexiDL = new Tuple<int, int>((int)(i + skaliranje / 2 - 1), j - 1);
             Tuple<int, int> indexiDD = new Tuple<int, int>((int)(i + skaliranje / 2 - 1), (int)(j + skaliranje / 2 - 1));
             if (indexiGL.Item1 > -1 && indexiGL.Item2 > -1 && indexiGL.Item1 < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiGL.Item1, indexiGL.Item2];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 > -1 && indexiDD.Item1 < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaBijelo += integralnaSlika[indexiDD.Item1, indexiDD.Item2];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 > -1 && indexiGD.Item1 < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiGD.Item1, indexiGD.Item2];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 > -1 && indexiDL.Item1 < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaBijelo -= integralnaSlika[indexiDL.Item1, indexiDL.Item2];
             if (indexiGL.Item1 > -1 && indexiGL.Item2 + skaliranje / 2 > -1 && indexiGL.Item1 < slika.GetLength(0) && (int)(indexiGL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiGL.Item1, (int)(indexiGL.Item2 + skaliranje / 2)];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 + skaliranje / 2 > -1 && indexiDD.Item1 < slika.GetLength(0) && (int)(indexiDD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno += integralnaSlika[indexiDD.Item1, (int)(indexiDD.Item2 + skaliranje / 2)];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 + skaliranje / 2 > -1 && indexiGD.Item1 < slika.GetLength(0) && (int)(indexiGD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiGD.Item1, (int)(indexiGD.Item2 + skaliranje / 2)];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 + skaliranje / 2 > -1 && indexiDL.Item1 < slika.GetLength(0) && (int)(indexiDL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaCrno -= integralnaSlika[indexiDL.Item1, (int)(indexiDL.Item2 + skaliranje / 2)];
             if (indexiGL.Item1 + skaliranje / 2 > -1 && indexiGL.Item2 + skaliranje / 2 > -1 && (int)(indexiGL.Item1 + skaliranje / 2) < slika.GetLength(0) && (int)(indexiGL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaBijelo += integralnaSlika[(int)(indexiGL.Item1 + skaliranje / 2), (int)(indexiGL.Item2 + skaliranje / 2)];
             if (indexiDD.Item1 + skaliranje / 2 > -1 && indexiDD.Item2 + skaliranje / 2 > -1 && (int)(indexiDD.Item1 + skaliranje / 2) < slika.GetLength(0) && (int)(indexiDD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaBijelo += integralnaSlika[(int)(indexiDD.Item1 + skaliranje / 2), (int)(indexiDD.Item2 + skaliranje / 2)];
             if (indexiGD.Item1 + skaliranje / 2 > -1 && indexiGD.Item2 + skaliranje / 2 > -1 && (int)(indexiGD.Item1 + skaliranje / 2) < slika.GetLength(0) && (int)(indexiGD.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaBijelo -= integralnaSlika[(int)(indexiGD.Item1 + skaliranje / 2), (int)(indexiGD.Item2 + skaliranje / 2)];
             if (indexiDL.Item1 + skaliranje / 2 > -1 && indexiDL.Item2 + skaliranje / 2 > -1 && (int)(indexiDL.Item1 + skaliranje / 2) < slika.GetLength(0) && (int)(indexiDL.Item2 + skaliranje / 2) < slika.GetLength(1)) sumaBijelo -= integralnaSlika[(int)(indexiDL.Item1 + skaliranje / 2), (int)(indexiDL.Item2 + skaliranje / 2)];
             if (indexiGL.Item1 > -1 && indexiGL.Item2 > -1 && (int)(indexiGL.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiGL.Item2 < slika.GetLength(1)) sumaCrno += integralnaSlika[(int)(indexiGL.Item1 + skaliranje / 2), indexiGL.Item2];
             if (indexiDD.Item1 > -1 && indexiDD.Item2 > -1 && (int)(indexiDD.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiDD.Item2 < slika.GetLength(1)) sumaCrno += integralnaSlika[(int)(indexiDD.Item1 + skaliranje / 2), indexiDD.Item2];
             if (indexiGD.Item1 > -1 && indexiGD.Item2 > -1 && (int)(indexiGD.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiGD.Item2 < slika.GetLength(1)) sumaCrno -= integralnaSlika[(int)(indexiGD.Item1 + skaliranje / 2), indexiGD.Item2];
             if (indexiDL.Item1 > -1 && indexiDL.Item2 > -1 && (int)(indexiDL.Item1 + skaliranje / 2) < slika.GetLength(0) && indexiDL.Item2 < slika.GetLength(1)) sumaCrno -= integralnaSlika[(int)(indexiDL.Item1 + skaliranje / 2), indexiDL.Item2];
             return new Tuple<double, double>(sumaCrno, sumaBijelo);
         }

         static HaarKarakteristika PronađiHK(int suma, List<HaarKarakteristika> lista)
         {
             if (lista != null && lista.Count>0) return lista.Find(karakteristika => karakteristika!=null && karakteristika.Suma == suma);
             else return null;
         }

         static void SpremiHK(HaarKarakteristika pronađenaKarakteristika, int suma, List<HaarKarakteristika> lista)
         {
             if (pronađenaKarakteristika == null)
             {
                 lista.Add(new HaarKarakteristika(suma, 1, 0, 0));
             }
             else
             {
                 lista.Remove(pronađenaKarakteristika);
                 var novaKarakteristika = new HaarKarakteristika(pronađenaKarakteristika.Suma, pronađenaKarakteristika.SnagaKlasifikatora + 1, pronađenaKarakteristika.BrojTacnihDetekcija, pronađenaKarakteristika.BrojNetacnihDetekcija);
                 lista.Add(novaKarakteristika);
             }
         }

         static void SpremiHK(HaarKarakteristika pronađenaKarakteristika, List<HaarKarakteristika> lista, bool tacnost)
         {
             lista.Remove(pronađenaKarakteristika);
             HaarKarakteristika novaKarakteristika;
             if (tacnost)
             {
                 novaKarakteristika = new HaarKarakteristika(pronađenaKarakteristika.Suma, pronađenaKarakteristika.SnagaKlasifikatora, pronađenaKarakteristika.BrojTacnihDetekcija+1, pronađenaKarakteristika.BrojNetacnihDetekcija);
             }
             else
             {
                 novaKarakteristika = new HaarKarakteristika(pronađenaKarakteristika.Suma, pronađenaKarakteristika.SnagaKlasifikatora, pronađenaKarakteristika.BrojTacnihDetekcija, pronađenaKarakteristika.BrojNetacnihDetekcija+1);
             }
             lista.Add(novaKarakteristika);
         }

         static void kopirajListu (List<HaarKarakteristika> lista1, List<HaarKarakteristika> lista2)
         {
             foreach (var element in lista2)
            {
                if (element!=null) lista1.Add(new HaarKarakteristika(element.Suma, element.SnagaKlasifikatora, element.BrojTacnihDetekcija, element.BrojNetacnihDetekcija));
            }
         }

    }
}