using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DetekcijaLica;

namespace FaceDetection
{
    public partial class Form1 : Form
    {
        string lokacijaRezultata = "";
        bool radi = false;
        bool lice = false, rezultatDetekcije = false;
        bool treningLica = false, treningNeLica = false, detekcija = false, detekcijaDirektorij = false, poboljsanje = false;
        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true; //Allow for the process to be cancelled
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (treningLica)
            {
                ViolaJonesDetekcija.TreningLica(textBox1.Text, backgroundWorker1);         
            }
            else if (treningNeLica)
            {
                ViolaJonesDetekcija.TreningNeLica(textBox1.Text, backgroundWorker1);
            }
            else if (detekcija)
            {
                rezultatDetekcije = ViolaJonesDetekcija.Detekcija(new Bitmap(pictureBox1.Image), backgroundWorker1);
            }
            else if (detekcijaDirektorij)
            {
                ViolaJonesDetekcija.DetekcijaIzvjestaj(textBox2.Text, backgroundWorker1);
            }
            else if (poboljsanje)
            {
                ViolaJonesDetekcija.IzvrsiPoboljsanje(radioButton4.Checked, new Bitmap(pictureBox1.Image), lice, backgroundWorker1);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if ((treningLica || treningNeLica) && e.ProgressPercentage+progressBar1.Value<101) progressBar1.Value+= e.ProgressPercentage;
            else if (detekcija && e.ProgressPercentage + progressBar2.Value < 101) progressBar2.Value += e.ProgressPercentage;
            else if (detekcijaDirektorij && e.ProgressPercentage + progressBar3.Value < 101) progressBar3.Value += e.ProgressPercentage;
            else if (poboljsanje && e.ProgressPercentage + progressBar4.Value < 101) progressBar4.Value += e.ProgressPercentage;
            if (e.ProgressPercentage == 101) {
                label12.Text = "Trajanje treninga: " + e.UserState.ToString() + " sekundi";
                treningLica = false;
                treningNeLica = false;
                radi = false;
            }
            else if (e.ProgressPercentage == 102)
            {
                if (rezultatDetekcije) label7.Text = "Rezultat detekcije: Detektovano lice";
                else label7.Text = "Rezultat detekcije: Nije detektovano lice";
                lice = rezultatDetekcije;
                label13.Text = "Trajanje detekcije: " + e.UserState.ToString() + " sekundi";
                textBox2.Text = "";
                detekcija = false;
                radi = false;
            }
            else if (e.ProgressPercentage == 103)
            {
                label7.Text = "Rezultat detekcije: U datoteci";
                lokacijaRezultata = Directory.GetCurrentDirectory() + "/rezultati.txt";
                label13.Text = "Trajanje detekcije: " + e.UserState.ToString() + " sekundi";
                textBox2.Text = "";
                detekcijaDirektorij = false;
                radi = false;
            }
            else if (e.ProgressPercentage == 104)
            {
                double uspjesnost = ViolaJonesDetekcija.DajTrenutnuUspjesnost() * 100;
                label2.Text = uspjesnost.ToString() + " %";
                label9.Text = ViolaJonesDetekcija.DajBrojProcesiranihSlika().ToString();
                label17.Text = "Trajanje poboljšanja: " + e.UserState.ToString() + " sekundi";
                poboljsanje = false;
                radi = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || (!radioButton1.Checked && !radioButton2.Checked)) return;
            if (radi) return;
            if (radioButton1.Checked) treningLica = true;
            else if (radioButton2.Checked) treningNeLica = true;
            progressBar1.Value = 0;
            label12.Text = "Trajanje treninga: ";
            radi = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = dialog.SelectedPath;
            }
            label12.Text = "Trajanje treninga:";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = dialog.FileName;
                pictureBox1.Image = new Bitmap(file);
                label7.Text = "Rezultat detekcije:";
                label13.Text = "Trajanje detekcije:";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null && textBox2.Text == "") return;
            if (radi) return;
            label7.Text = "Rezultat detekcije:";
            label13.Text = "Trajanje detekcije:";
            progressBar2.Value = 0;
            progressBar3.Value = 0;
            if (textBox2.Text == "")
            {
                detekcija = true;
            }
            else
            {
                detekcijaDirektorij = true;
            }
            radi = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked && radioButton2.Checked) radioButton2.Checked = false;
            else if (!radioButton1.Checked && !radioButton2.Checked) radioButton2.Checked = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (lokacijaRezultata != "") Process.Start(lokacijaRezultata);
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked && radioButton2.Checked) radioButton1.Checked = false;
            else if (!radioButton1.Checked && !radioButton2.Checked) radioButton1.Checked = true;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked && radioButton4.Checked) radioButton4.Checked = false;
            else if (!radioButton3.Checked && !radioButton4.Checked) radioButton4.Checked = true;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked && radioButton4.Checked) radioButton3.Checked = false;
            else if (!radioButton3.Checked && !radioButton4.Checked) radioButton3.Checked = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if ((!radioButton3.Checked && !radioButton4.Checked) || pictureBox1.Image == null) return;
            if (radi) return;
            label17.Text = "Trajanje poboljšanja:";
            if (!radioButton4.Checked)
            {
                poboljsanje = true;
                progressBar4.Value = 0;
                radi = true;
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                ViolaJonesDetekcija.IzvrsiPoboljsanje(radioButton4.Checked, new Bitmap(pictureBox1.Image), lice, backgroundWorker1);
                double uspjesnost = ViolaJonesDetekcija.DajTrenutnuUspjesnost() * 100;
                label2.Text = uspjesnost.ToString() + " %";
                label9.Text = ViolaJonesDetekcija.DajBrojProcesiranihSlika().ToString();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox2.Text = dialog.SelectedPath;
            }
            label13.Text = "Trajanje detekcije:";
        }
    }
}
