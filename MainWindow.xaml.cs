using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace MHW_MonsterSize_Editor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static int SizeCount = 38;
        private Header header;
        private Byte[] backup;
        private List<SizeTable> sizeTable;
        private List<EmSize> emSize;

        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.SaveDirectory == "")
            {
                Properties.Settings.Default.SaveDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            LanguageChanged(Languages.Items.GetItemAt(Properties.Settings.Default.Language), null);
        }

        private void InitTable(byte[] data)
        {

            sizeTable = new List<SizeTable>();
            emSize = new List<EmSize>();
            header = new Header(data);
            int index = 12;
            for (int i = 0; i < header.EmCount; i++)
            {
                emSize.Add(new EmSize(data.Skip(index).Take(20).ToArray()));
                index += 20;
                emSize[i].Key = i;
            }
            header.TableCount = BitConverter.ToInt32(data, index);
            index += 4;
            for (int i = 0; i < header.TableCount; i++)
            {
                var temp = new SizeTable(data, index);
                sizeTable.Add(temp);
                index += temp.Type.Length + 8 * SizeCount;
                sizeTable[i].Key = i;
            }

            sizeIndex.Header = Application.Current.FindResource("index");
            probIndex.Header = Application.Current.FindResource("index");
            crownIndex.Header = Application.Current.FindResource("index");
            sizeDesc.Header = Application.Current.FindResource("desc");
            probDesc.Header = Application.Current.FindResource("desc");
            total.Header = Application.Current.FindResource("total");

            SizeT.ItemsSource = null;
            SizeT.ItemsSource = sizeTable;

            ProbT.ItemsSource = null;
            ProbT.ItemsSource = sizeTable;

            CrownT.ItemsSource = null;
            CrownT.ItemsSource = emSize;

            if (SizeT.Columns.Count < 4)
            {
                for (int i = 0; i < SizeCount; i++)
                {
                    var newCol = new DataGridTextColumn();
                    newCol.Header = 88 + i;
                    newCol.Width = 50;
                    newCol.Binding = new Binding(string.Format("Size[{0}]", i));
                    SizeT.Columns.Add(newCol);
                    newCol = new DataGridTextColumn();
                    newCol.Header = 88 + i;
                    newCol.Width = 50;
                    newCol.Binding = new Binding(string.Format("Chance[{0}]", i));
                    ProbT.Columns.Add(newCol);
                }
            }


        }

        private void LanguageChanged(object sender, RoutedEventArgs e)
        {

            (sender as MenuItem).IsChecked = true;
            ResourceDictionary dict = new ResourceDictionary();
            
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), string.Format("{0}.xaml", (sender as MenuItem).Tag))))
            {
                dict.Source = new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), string.Format("{0}.xaml", (sender as MenuItem).Tag)), UriKind.Absolute);
                var extDict = new Uri(string.Format("{0}.xaml", (sender as MenuItem).Tag), UriKind.Relative);
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = extDict });
            }
            else
                dict.Source = new Uri(string.Format("{0}.xaml", (sender as MenuItem).Tag), UriKind.Relative);

            foreach (MenuItem lang in Languages.Items)
            {
                if (lang != (sender as MenuItem))
                {
                    lang.IsChecked = false;
                }
            }

            Application.Current.Resources.MergedDictionaries.Add(dict);

            if (sizeTable != null)
                foreach (SizeTable st in sizeTable)
                    st.LangChange();
                    
            if (emSize != null)
                foreach (EmSize es in emSize)
                    es.LangChange();

            probIndex.Header = (string)Application.Current.FindResource("index");
            sizeIndex.Header = (string)Application.Current.FindResource("index");
            crownIndex.Header = (string)Application.Current.FindResource("index");

            probDesc.Header = (string)Application.Current.FindResource("desc");
            sizeDesc.Header = (string)Application.Current.FindResource("desc");

            total.Header = (string)Application.Current.FindResource("total");

            monster.Header = (string)Application.Current.FindResource("monster");
            basesize.Header = (string)Application.Current.FindResource("base");
            small.Header = (string)Application.Current.FindResource("small");
            silver.Header = (string)Application.Current.FindResource("silver");
            big.Header = (string)Application.Current.FindResource("big");

            Properties.Settings.Default.Language = Languages.Items.IndexOf(sender as MenuItem);
            Properties.Settings.Default.Save();
            
        }

        private void LangUncheck(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem lang in Languages.Items)
            {
                if (lang.IsChecked == true)
                {
                    return;
                }
            }
            (sender as MenuItem).IsChecked = true;
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            InitTable((byte[])backup.Clone());
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "em_rsiz",
                DefaultExt = ".dtt_rsz",
                Filter = "Monster Size File | *.dtt_rsz",
                InitialDirectory = Properties.Settings.Default.SaveDirectory
            };
            if (dlg.ShowDialog() == true)
            {
                Open(dlg.FileName);
            }
        }

        private void Open(string filename)
        {

            Properties.Settings.Default.SaveDirectory = Path.GetDirectoryName(filename);
            Properties.Settings.Default.Save();
            byte[] data = File.ReadAllBytes(filename);
            backup = (byte[])data.Clone();

            InitTable(data);
        }

        private async void SaveFile(object sender, RoutedEventArgs e)
        {
            foreach (SizeTable st in sizeTable)
            {
                if (st.Total != 100)
                {
                    MessageBox.Show((string)Application.Current.FindResource("errorSum"), "Error");
                    return;
                }
            }

            Stream fs;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = "em_rsiz",
                DefaultExt = ".dtt_rsz",
                Filter = "Monster Size File | *.dtt_rsz",
                InitialDirectory = Properties.Settings.Default.SaveDirectory
            };

            if (dlg.ShowDialog() == true)
            {
                if ((fs = dlg.OpenFile()) != null)
                {
                    Properties.Settings.Default.SaveDirectory = Path.GetDirectoryName(dlg.FileName);
                    Properties.Settings.Default.Save();

                    List<byte> t = new List<byte>();
                    t.AddRange(header.Singnature);
                    t.AddRange(BitConverter.GetBytes(header.EmCount));
                    foreach (EmSize es in emSize)
                    {
                        t.AddRange(BitConverter.GetBytes(es.EmID));
                        t.AddRange(BitConverter.GetBytes(es.Small));
                        t.AddRange(BitConverter.GetBytes(es.Silver));
                        t.AddRange(BitConverter.GetBytes(es.Big));
                        t.AddRange(BitConverter.GetBytes(es.Size));

                    }
                    t.AddRange(BitConverter.GetBytes(header.TableCount));
                    foreach (SizeTable st in sizeTable)
                    {
                        t.AddRange(st.Type);
                        for (int i=0;i<SizeCount;i++)
                        {
                            t.AddRange(BitConverter.GetBytes(st.Size[i]));
                            t.AddRange(BitConverter.GetBytes(st.Chance[i]));
                        }
                    }
                    ResourceDictionary dic = Application.Current.Resources;

                    StreamWriter writer = new StreamWriter(string.Format("{0}.xaml", (Languages.Items.GetItemAt(Properties.Settings.Default.Language) as MenuItem).Tag));
                    XamlWriter.Save(dic, writer);
                    writer.Close();

                    byte[] output = t.ToArray();

                    await fs.WriteAsync(output, 0, output.Length);

                    fs.Close();
                }
            }
        }

        private void ShowPath(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("..\\Steam\\steamapps\\common\\Monster Hunter World\\\r\n\tnativePC\\common\\em\\em_rsiz.dtt_rsz\r\n..\\Monster Hunter World\\nativePC\\common\\em\\em_rsiz.dtt_rsz", "Path");
        }

        private void DropOpen(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                //Debug.Print(Path.GetExtension(files[0]));
                if (Path.GetExtension(files[0]) == ".dtt_rsz")
                    Open(files[0]);
            }
        }

        private void Modify(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (int.TryParse(((TextBox)e.EditingElement).Text, out int n)) 
            {
                if (ProbEdit.IsSelected)
                {
                    if (e.Column.DisplayIndex > 2) ((SizeTable)e.Row.Item).Chance[e.Column.DisplayIndex - 3] = n;
                    ((SizeTable)e.Row.Item).ProbRefresh();
                    
                }
                else if (SizeEdit.IsSelected)
                {
                    if (e.Column.DisplayIndex > 1) ((SizeTable)e.Row.Item).Size[e.Column.DisplayIndex - 2] = n;
                    ((SizeTable)e.Row.Item).SizeRefresh();
                }
            }
        }

        private void SingleFix(object sender, RoutedEventArgs e)
        {
            if (ProbEdit.IsSelected)
            {
                if (int.TryParse(fixSize.Text, out int n) && n >= 88 && n <= 125)
                {
                    foreach (DataGridCellInfo c in ProbT.SelectedCells)
                    {
                        Array.Clear(((SizeTable)c.Item).Chance, 0, ((SizeTable)c.Item).Chance.Length);
                        ((SizeTable)c.Item).Chance[n - 88] = 100;
                        ((SizeTable)c.Item).ProbRefresh();
                    }
                }
            }
            else
            {
                if (int.TryParse(fixSize.Text, out int n) && n >= 0)
                {
                    foreach (DataGridCellInfo c in SizeT.SelectedCells)
                    {
                        for (int i = 0; i < SizeCount; i++)
                            ((SizeTable)c.Item).Size[i] = n;
                        ((SizeTable)c.Item).SizeRefresh();
                    }
                }
            }

        }

        private void Fill(object sender, RoutedEventArgs e)
        {
            if (sizeTable != null && int.TryParse(prob.Text, out int n))
            {
                if (ProbEdit.IsSelected)
                {
                    foreach (DataGridCellInfo c in ProbT.SelectedCells)
                    {
                        if (c.Column.DisplayIndex > 2) ((SizeTable)c.Item).Chance[c.Column.DisplayIndex - 3] = n;
                        ((SizeTable)c.Item).ProbRefresh();
                    }
                }
                else
                {
                    foreach (DataGridCellInfo c in SizeT.SelectedCells)
                    {
                        if (c.Column.DisplayIndex > 1) ((SizeTable)c.Item).Size[c.Column.DisplayIndex - 2] = n;
                        ((SizeTable)c.Item).SizeRefresh();
                    }
                }
            }
        }

        private void AllZero(object sender, RoutedEventArgs e)
        {
            if (SizeEdit.IsSelected)
            {
                foreach (DataGridCellInfo c in SizeT.SelectedCells)
                {
                    Array.Clear(((SizeTable)c.Item).Size, 0, ((SizeTable)c.Item).Size.Length);
                    ((SizeTable)c.Item).SizeRefresh();
                }
            } 
            else
            {
                foreach (DataGridCellInfo c in ProbT.SelectedCells)
                {
                    Array.Clear(((SizeTable)c.Item).Chance, 0, ((SizeTable)c.Item).Chance.Length);
                    ((SizeTable)c.Item).ProbRefresh();
                }
            }
        }

        private void TabChange(object sender, SelectionChangedEventArgs e)
        {
            if (CrownEdit.IsSelected)
            {
                BtnPanel.Visibility = Visibility.Hidden;
            } else
            {
                BtnPanel.Visibility = Visibility.Visible;
            }
        }
    }

    public class Header
    {
        public Header(byte[] data)
        {
            this.Singnature = data.Take(8).ToArray();
            this.EmCount = BitConverter.ToInt32(data, 8);
        }

        public byte[] Singnature { get; set; }
        public int EmCount { get; set; }
        public int TableCount { get; set; }
    }

    public class EmSize : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public EmSize(byte[] data)
        {
            this.EmID = BitConverter.ToInt32(data, 0);
            this.Small = BitConverter.ToInt32(data, 4);
            this.Silver = BitConverter.ToInt32(data, 8);
            this.Big = BitConverter.ToInt32(data, 12);
            this.Size = BitConverter.ToSingle(data, 16);

            LangChange();
        }
        public int Key { get; set; }
        public int EmID { get; set; }
        public int Small { get; set; }
        public int Silver { get; set; }
        public int Big { get; set; }
        public float Size { get; set; }
        public string EmName { get; set; }

        public void LangChange()
        {
            try
            {
                this.EmName = (string)Application.Current.FindResource("em" + EmID.ToString().PadLeft(3, '0'));
            }
            catch (ResourceReferenceKeyNotFoundException)
            {
                this.EmName = string.Format("Em ID {0}", EmID);
            }
            OnPropertyChanged("EmName");


        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SizeTable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SizeTable(byte[] data, int index)
        {
            byte[] temp = data.Skip(index).ToArray();
            int len = Array.IndexOf(temp, (byte)0) + 1;

            this.Type = temp.Take(len).ToArray();
            this.Size = new int[MainWindow.SizeCount];
            this.Chance = new int[MainWindow.SizeCount];

            index += len;
            for (int i=0;i< MainWindow.SizeCount;i++)
            {
                this.Size[i] = BitConverter.ToInt32(data, index);
                index += 4;
                this.Chance[i] = BitConverter.ToInt32(data, index);
                index += 4;
            }
            LangChange();
            CalcTotal();
        }
        public int Key { get; set; }
        public byte[] Type { get; set; }
        public string Description { get; set; }
        public int[] Size { get; set; }
        public int[] Chance { get; set; }
        public int Total { get; set; }

        public void LangChange()
        {
            try
            {
                this.Description = (string)Application.Current.FindResource(Encoding.UTF8.GetString(Type, 0, Type.Length - 1));
            }
            catch (ResourceReferenceKeyNotFoundException)
            {
                this.Description = "UNKN";
            }
            OnPropertyChanged("Description");
        }

        public void CalcTotal()
        {
            Total = Chance.Sum();
            OnPropertyChanged("Total");
        }

        public void SizeRefresh()
        {
            OnPropertyChanged("Size");
        }

        public void ProbRefresh()
        {
            CalcTotal();
            OnPropertyChanged("Chance");
        }

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
