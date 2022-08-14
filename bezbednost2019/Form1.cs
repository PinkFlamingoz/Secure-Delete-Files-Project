using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace bezbednost2019
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string[] hardlinks;

        public void printtime()
        {
            logs.Text += "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
        }

        public void helperprocess(string command, string arguments, bool mode)
        {
            using (Process hl_proc = new Process())
            {
                hl_proc.StartInfo.FileName = command;
                hl_proc.StartInfo.Arguments = arguments;
                hl_proc.StartInfo.Verb = "runas";
                hl_proc.StartInfo.UseShellExecute = false;
                hl_proc.StartInfo.RedirectStandardOutput = mode;
                hl_proc.StartInfo.CreateNoWindow = mode;
                hl_proc.Start();
                if (mode)
                {
                    StreamReader reader = hl_proc.StandardOutput;
                    string output = reader.ReadToEnd();
                    hl_proc.WaitForExit();
                    if (command == "fsutil.exe")
                    {
                        hardlinks = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    }
                    printtime();
                    logs.Text += output + "\n";
                }
                hl_proc.WaitForExit();
            }
        }

        public void accepteula(string command)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = command;
                proc.StartInfo.Arguments = string.Format("/accepteula");
                proc.StartInfo.Verb = "runas";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
            }
        }

        public void brisi(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    File.SetAttributes(filename, FileAttributes.Normal);
                    double sectors = Math.Ceiling(new FileInfo(filename).Length / 512.0);
                    byte[] zeroBuffer = new byte[512];
                    Stream inputStream;
                    try
                    {
                        inputStream = new FileStream(
                            filename,
                            FileMode.Open,
                            FileAccess.Write,
                            FileShare.None,
                            (int)filename.Length,
                            FileOptions.DeleteOnClose | FileOptions.RandomAccess | FileOptions.WriteThrough);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return;
                    }
                    using (inputStream)
                    {
                        for (int sectorsWritten = 0; sectorsWritten < sectors; sectorsWritten++)
                        {
                            progressBar1.Increment(Convert.ToInt32(75 / sectors));
                            label1.Text = progressBar1.Value.ToString() + "%";
                            inputStream.Write(zeroBuffer, 0, zeroBuffer.Length);
                        }
                        inputStream.SetLength(0);
                        inputStream.Close();
                        printtime();
                        logs.Text += "Deleted " + filename + "\n";
                    }
                    checker();
                    MessageBox.Show("Completed");
                }
            }
            catch (Exception e)
            {
                printtime();
                logs.Text += e;
                MessageBox.Show("Error");
            }
        }

        private void browse_button_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filepath.Text = openFileDialog1.FileName;
            }
            progBarInit();
        }

        private void browse_button_sd_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                sdpath.Text = openFileDialog2.FileName;
            }
        }

        private void delete_button_Click(object sender, EventArgs e)
        {
            this.timer1.Start();
            if (filepath.Text != "" && File.Exists(filepath.Text))
            {
                DialogResult okcancle = MessageBox.Show($"Are you sure you want to erase this file {filepath.Text}?", "Continue", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (okcancle == DialogResult.OK)
                {
                    printtime();
                    logs.Text += $"File selected for deletion {filepath.Text}\n";
                    progressBar1.Increment(5);
                    label1.Text = progressBar1.Value.ToString() + "%";
                    printtime();
                    logs.Text += "Hard links:\n";
                    helperprocess("fsutil.exe", "hardlink list " + filepath.Text, true);
                    progressBar1.Increment(5);
                    label1.Text = progressBar1.Value.ToString() + "%";
                    foreach (string s in hardlinks)
                    {
                        string fullPath = Path.GetFullPath(filepath.Text[0] + ":" + s);
                        if (fullPath != filepath.Text && File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            printtime();
                            logs.Text += $"Deleted hard link: {fullPath}\n";
                        }
                    }
                    progressBar1.Increment(5);
                    label1.Text = progressBar1.Value.ToString() + "%";
                    if (File.Exists(sdpath.Text))
                    {
                        accepteula(sdpath.Text);
                        helperprocess(sdpath.Text, " -p 4 " + filepath.Text, true);
                        progressBar1.Increment(20);
                        label1.Text = progressBar1.Value.ToString() + "%";
                        helperprocess(sdpath.Text, " -z " + filepath.Text.Substring(0, 3), false);
                        progressBar1.Increment(75);
                        label1.Text = progressBar1.Value.ToString() + "%";
                        MessageBox.Show("Completed");

                    }
                    else
                    {
                        printtime();
                        logs.Text += "Starting deletion " + filepath.Text + "\n";
                        brisi(filepath.Text);
                    }
                }
            }
            else
            {
                this.timer1.Stop();
                MessageBox.Show("Please select a file!", "Error - No file selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                printtime();
                logs.Text += "No file selected\n";
            }
            printtime();
            logs.Text += "Completed\n";
            filepath.Text = "";
            this.timer1.Stop();
            button1.Enabled = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("IMPORTANT: MAKE SURE NOT TO HAVE A SPACEBAR IN THE FOLDER OR FILE YOU WANT TO DELETE" + "\n" + " We simply select the file that we want to delete then " + "\n" + "We have 2 options:" + "\n" +
                " A: Use my method of secure delete(that works with filling the file with 0's and then it deletes it." + "\n" +
                " B: Use the Sdelete (I just added this option because I wanted to test it.)  " + "\n" + " NOTE: When using the Sdelet you must select it where you have unpacked it or simply use the source I have provided in the Sdelete folder. ", "Tutorial", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = progressBar1.Value.ToString() + "%";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            logs.Text = string.Empty;
            button1.Enabled = false;
            progressBar1.Value = 0;
            label1.Text = "0%";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/sysinternals/downloads/sdelete");
        }

        private void progBarInit()
        {
            progressBar1.Value = 0;
            label1.Text = "0%";
        }
        private void checker()
        {
            if (progressBar1.Value != 100)
                progressBar1.Value = 100;
            label1.Text = progressBar1.Value.ToString() + "%";
        }
    }
}