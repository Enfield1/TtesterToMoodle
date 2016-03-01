using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;


namespace GuiTtesterToMoodle
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker;
        public string temp;
        bool Check;

        public string[] filenames;

        public MainWindow()
        {
            
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            InitializeComponent();
        }


       public void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //ProgressBar 
            temp = Converter.BodyConverter(filenames, PBar, Check);
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {

            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";
            dlg.Multiselect = true;

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            // Open document
            
            if (result == true)
            {
                filenames = dlg.FileNames;
                foreach (var item in filenames) listBox.Items.Add(item);

                if (filenames.Length != 0) Convert.IsEnabled = true;
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            PBar.Visibility = Visibility.Visible;
            PBar.Maximum = filenames.Count();

            listBox.Items.Clear();
            textBox.Clear();

            Check = (bool)checkBox.IsChecked;

            worker.RunWorkerCompleted += (o, ea) => { if (PBar.Maximum == PBar.Value) textBox.AppendText(temp); PBar.Visibility = Visibility.Hidden; Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Ttester To Moodle Converter\\Moodle"); };
            worker.RunWorkerAsync();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Создано: Тихомиров Д.С. Корнилов В.А. \r\n По вопросам работы программы звонить 28-57-69");
        }

    }


}
