using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DetekcijaLica;

namespace FaceDetection
{
    public partial class Form1 : Form
    {
        bool lice=false;
        string lokacijaRezultata = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || (!radioButton1.Checked && !radioButton2.Checked)) return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (radioButton1.Checked) ViolaJonesDetekcija.TreningLica(textBox1.Text, progressBar1);
            else ViolaJonesDetekcija.TreningNeLica(textBox1.Text, progressBar1);
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            label12.Text = "Trajanje treninga: " + trajanje.ToString()+" sekundi";
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
            label7.Text = "Rezultat detekcije:";
            label13.Text = "";
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (textBox2.Text == "")
            {
                bool rezultatDetekcije = ViolaJonesDetekcija.Detekcija(new Bitmap(pictureBox1.Image), progressBar2);
                watch.Stop();
                if (rezultatDetekcije) label7.Text = "Rezultat detekcije: Detektovano lice";
                else label7.Text = "Rezultat detekcije: Nije detektovano lice";
                lice = rezultatDetekcije;
            }
            else
            {
                ViolaJonesDetekcija.DetekcijaIzvjestaj(textBox2.Text, progressBar3, progressBar2);
                watch.Stop();
                label7.Text = "Rezultat detekcije: U datoteci";
                lokacijaRezultata = Directory.GetCurrentDirectory() + "/rezultati.txt";
            }
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            label13.Text = "Trajanje detekcije: "+trajanje.ToString()+" sekundi";
            textBox2.Text = "";
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
            if ((!radioButton3.Checked && !radioButton4.Checked) || pictureBox1.Image==null) return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ViolaJonesDetekcija.IzvrsiPoboljsanje(radioButton4.Checked, new Bitmap(pictureBox1.Image), lice, progressBar4);
            watch.Stop();
            var trajanje = watch.ElapsedMilliseconds;
            trajanje /= 1000;
            label17.Text = "Trajanje poboljšanja: " + trajanje.ToString() + " sekundi";
            double uspjesnost = ViolaJonesDetekcija.DajTrenutnuUspjesnost() * 100;
            label2.Text = uspjesnost.ToString()+" %";
            label9.Text = ViolaJonesDetekcija.DajBrojProcesiranihSlika().ToString();
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked && radioButton2.Checked) radioButton2.Checked = false;
            else if (!radioButton1.Checked && !radioButton2.Checked) radioButton2.Checked = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked && radioButton2.Checked) radioButton1.Checked = false;
            else if (!radioButton1.Checked && !radioButton2.Checked) radioButton1.Checked = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (lokacijaRezultata!="") Process.Start(lokacijaRezultata);
        }
    }
}
