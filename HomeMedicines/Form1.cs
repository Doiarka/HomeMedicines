using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Globalization;
using System.Resources;
using System.Drawing.Imaging;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Printing;
using System.Collections;

namespace HomeMedicines
{
    public partial class MainForm : Form
    {
        /*--------------------------------------------------------------------------------------------------Глобальные переменные-----------*/
        private String dbFileName;
        private SQLiteConnection connection;
        private SQLiteCommand command;
        private DataTable dt;
        private DataTable buylistdt;
        private SQLiteDataAdapter da;
        private bool medpic;
        private string ImagesPath = Convert.ToString(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Images\\");
        private List<string> PrescribingList = new List<string>();

        //Заглавный первый символ
        private string flutext(string input)
        {
            string output = input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
            return output;
        }

        //Текст к маленьким и к тексту
        private string flltext(string input)
        {
            string output = input.ToLower();
            return output;
        }

        /*--------------------------------------------------------------------------------------------------Инициализация главной формы-----------*/
        public MainForm()
        {
            InitializeComponent();
        }

        //При загрузке главной формы
        private void MainForm_Load(object sender, EventArgs e)
        {
            connection = new SQLiteConnection();
            command = new SQLiteCommand();
            dbFileName = "database.sqlite";
            Message(0);

            if (!File.Exists(dbFileName))
            {
                SQLiteConnection.CreateFile(dbFileName);
            }

            if (!Directory.Exists(ImagesPath))
            {
                Directory.CreateDirectory(ImagesPath);
            }

            try
            {
                connection = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                connection.Open();
                command.Connection = connection;

                //Создание таблиц в базе данных
                //Пользователи
                command.CommandText = "CREATE TABLE IF NOT EXISTS Users (id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)";
                command.ExecuteNonQuery();
                //Аптечка
                command.CommandText = "CREATE TABLE IF NOT EXISTS MedKit (id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, " +
                    "Count REAL, BuyDate INTEGER, BestBefore INTEGER)";
                command.ExecuteNonQuery();
                //Лекарства
                command.CommandText = "CREATE TABLE IF NOT EXISTS BasicMeds (id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, " +
                    "Prescribing TEXT, DosageForm TEXT, Description TEXT, PharmEffect TEXT, IndicationsForUse TEXT, " +
                    "Contraindications TEXT, Dosing TEXT, StorageConditions TEXT, Recipe TEXT)";
                command.ExecuteNonQuery();
                //История покупок
                command.CommandText = "CREATE TABLE IF NOT EXISTS BuyHistory (id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, " +
                    "Pharmacy TEXT, Address TEXT, Count REAL, Price REAL, BuyDate INTEGER)";
                command.ExecuteNonQuery();
                //История употребления лекарств пользователями
                command.CommandText = "CREATE TABLE IF NOT EXISTS UseHistory (id INTEGER PRIMARY KEY AUTOINCREMENT, User TEXT, " +
                    "MedName TEXT, Prescribing TEXT, Count INTEGER, UseDate INTEGER)";
                command.ExecuteNonQuery();

                CheckBestBefore();

                Message(1);
            }
            catch (SQLiteException ex)
            {
                Message(2);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Спрятать все панели
        private void HidePanels()
        {
            if (MedInfoPanel.Enabled == true)
            {
                EditMedOff();
            }

            foreach (Control c in MainPanel.Controls)
            {
                if (c is Panel) c.Visible = false;
                if (c is Panel) c.Enabled = false;
            }

            CheckBestBefore();
        }

        /*--------------------------------------------------------------------------------------------------Файл-----------*/
        //Файл - Аптечка
        private void KitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();
            FilterComboBox.Items.Clear();

            try
            {
                MedKitPanel.Enabled = true;
                MedKitPanel.Visible = true;
                GetPrescribing();
                FilterComboBox.Items.Add("Все");

                foreach (string item in PrescribingList)
                {
                    FilterComboBox.Items.Add(flutext(item));
                }

                FilterComboBox.SelectedItem = "Все";
                SortComboBox.SelectedItem = SortComboBox.Items[0];
                SearchMedInKitTextBox.Focus();

                Message(3);
            }
            catch (SQLiteException ex)
            {
                Message(4);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Файл - Добавить лекарство
        private void AddMedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();

            PharmacyName();
            PharmacyAddress();

            PanelON(AddMedPanel);
            AddMedTextBox1.Focus();

            Message(5);
        }

        //Создание списка названий аптек
        private void PharmacyName()
        {
            PharmacyComboBox.Items.Clear();

            command.CommandText = "Select Pharmacy from BuyHistory where Pharmacy <> '' group by Pharmacy";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                PharmacyComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }
        }

        //Создание списка адресов аптек
        private void PharmacyAddress()
        {
            AddressComboBox.Items.Clear();

            command.CommandText = "Select Address from BuyHistory where Address <> '' group by Address";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                AddressComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }
        }

        //Файл - Выход
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /*--------------------------------------------------------------------------------------------------Кнопки-----------*/
        //Создание пользователя
        private void CreateUserButton_Click(object sender, EventArgs e)
        {
            try
            {
                command.CommandText = "SELECT * FROM Users WHERE Name = '" + flltext(NewUserTextBox.Text) + "'";
                command.ExecuteNonQuery();
                dt = new DataTable();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);

                if (dt.Rows.Count > 0
                    && String.IsNullOrEmpty(NewUserTextBox.Text)
                    && String.IsNullOrWhiteSpace(NewUserTextBox.Text)
                    )
                {
                    Message(6);
                    return;
                }

                try
                {
                    command.CommandText = "INSERT INTO Users (Name) VALUES ('" + flltext(NewUserTextBox.Text) + "')";
                    command.ExecuteNonQuery();
                    Message(7);
                    NewUserTextBox.Text = null;
                    UsersToolStripMenuItem.PerformClick();
                }
                catch (SQLiteException ex)
                {
                    Message(8);
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
            catch (SQLiteException ex)
            {
                Message(9);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Поиск лекарств при занесении лекарства
        private void SearchMedButton_Click(object sender, EventArgs e)
        {
            try
            {
                CreateBasicMedPanel.Enabled = false;
                CreateBasicMedPanel.Visible = false;

                PrescribingBasicMedComboBox.Items.Clear();
                DosageFormBasicMedComboBox.Items.Clear();

                FoundMedsListBox.Items.Clear();
                string searchTxt = flltext(AddMedTextBox1.Text);

                command.CommandText = "SELECT Name FROM BasicMeds WHERE Name like '%" + searchTxt + "%' ORDER BY Name";
                command.ExecuteNonQuery();
                dt = new DataTable();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string item = flutext(dt.Rows[i][dt.Columns[0]].ToString());
                        FoundMedsListBox.Items.Add(item);
                    }
                }
                else
                {
                    ShowPrescribing(PrescribingBasicMedComboBox);
                    ShowDosageForm(DosageFormBasicMedComboBox);

                    Message(10);
                    PanelON(CreateBasicMedPanel);
                }
            }
            catch (SQLiteException ex)
            {
                Message(11);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Занесение леарства в аптечку
        private void AddMedButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (PriceTextBox.Text.Contains("."))
                {
                    PriceTextBox.Text = PriceTextBox.Text.Replace(".", ",");
                }

                string rPrice = "";

                if (PriceTextBox.Text != "")
                {
                    double value = Convert.ToDouble(PriceTextBox.Text);
                    rPrice = value.ToString("0.00", CultureInfo.InvariantCulture);
                }
                
                //Преобразование даты из календаря даты покупки в инт
                DateTime DTBuyDate = BuyDateMonthCalendar.SelectionStart.Date;
                DTBuyDate = DateTime.SpecifyKind(DTBuyDate, DateTimeKind.Utc);
                DateTimeOffset DTOBuyDate = DTBuyDate;
                int iBuyDate = Convert.ToInt32(DTOBuyDate.ToUnixTimeSeconds());

                //Преобразование даты из календаря употребить до в инт
                DateTime DTBestBefore = BestBeforeMonthCalendar.SelectionStart.Date;
                DTBestBefore = DateTime.SpecifyKind(DTBestBefore, DateTimeKind.Utc);
                DateTimeOffset DTOBestBefore = DTBestBefore;
                int iBestBefore = Convert.ToInt32(DTOBestBefore.ToUnixTimeSeconds());

                if (iBuyDate > iBestBefore)
                {
                    Message(12);
                    return;
                }

                bool bo1 = FoundMedsListBox.SelectedItem == null;
                bool bo2 = CountTextBox.Text == "";

                if (bo1)
                {
                    Message(29);
                    return;
                }
                else if(bo2)
                {
                    Message(30);
                    return;
                }

                command.CommandText = "INSERT INTO MedKit(Name, Count, BuyDate, BestBefore) " +
                    "VALUES('" + flltext(FoundMedsListBox.SelectedItem.ToString()) +
                    "', '" + CountTextBox.Text +
                    "', '" + iBuyDate +
                    "', '" + iBestBefore + "')";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO BuyHistory(Name, Pharmacy, Address, Count, Price, BuyDate) " +
                    "VALUES('" + flltext(FoundMedsListBox.SelectedItem.ToString()) +
                    "', '" + flltext(PharmacyComboBox.Text) +
                    "', '" + flltext(AddressComboBox.Text) +
                    "', '" + CountTextBox.Text +
                    "', '" + rPrice +
                    "', '" + iBuyDate + "')";
                command.ExecuteNonQuery();

                Message(13);
                FoundMedsListBox.Items.Clear();
                AddMedTextBox1.Clear();
                //PharmacyComboBox.Items.Clear();
                //AddressComboBox.Items.Clear();
                CountTextBox.Clear();
                PriceTextBox.Clear();
                
                BuyDateMonthCalendar.SetDate(DateTime.Today);
                BestBeforeMonthCalendar.SetDate(DateTime.Today);
                CheckBestBefore();
            }
            catch (SQLiteException ex)
            {
                Message(14);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Создание нового типового лекарства
        private void CreateBasicMedButton_Click(object sender, EventArgs e)
        {
            command.CommandText = "SELECT * FROM BasicMeds WHERE Name = '" + flltext(NameBasicMedTextBox.Text) + "'";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                Message(26);
                return;
            }

            bool bo = !(
                !String.IsNullOrEmpty(NameBasicMedTextBox.Text)
                && !String.IsNullOrWhiteSpace(NameBasicMedTextBox.Text)
                && !String.IsNullOrEmpty(PrescribingBasicMedComboBox.Text)
                && !String.IsNullOrWhiteSpace(PrescribingBasicMedComboBox.Text)
                && !String.IsNullOrEmpty(DosageFormBasicMedComboBox.Text)
                && !String.IsNullOrWhiteSpace(DosageFormBasicMedComboBox.Text)
                && dt.Rows.Count <= 0
                );

            if (bo)
            {
                Message(27);
                return;
            }

            string NameBasicMed = flltext(NameBasicMedTextBox.Text);
            string PrescribingBasicMed = flltext(PrescribingBasicMedComboBox.Text);
            string DosageFormBasicMed = flltext(DosageFormBasicMedComboBox.Text);
            string DescriptionBasicMed = DescriptionBasicMedTextBox.Text;
            string PharmEffectBasicMed = PharmEffectBasicMedTextBox.Text;
            string IndicationsForUseBasicMed = IndicationsForUseBasicMedTextBox.Text;
            string ContraindicationsBasicMed = ContraindicationsBasicMedTextBox.Text;
            string DosingBasicMed = DosingBasicMedTextBox.Text;
            string StorageConditionsBasicMed = StorageConditionsBasicMedTextBox.Text;
            string RecipeBasicMed = RecipeBasicMedTextBox.Text;

            string imageName = NameBasicMedTextBox.Text;

            string PictureMedFullPath = null;

            if (CreateBasicMedPictureBox.Image != null)
            {
                PictureMedFullPath = ImagesPath + imageName + ".jpg";

                this.CreateBasicMedPictureBox.Image.Tag = imageName;

                try
                {
                    CreateBasicMedPictureBox.Image.Save(PictureMedFullPath, ImageFormat.Jpeg);
                }
                catch (System.Runtime.InteropServices.ExternalException ex)
                {
                    MessageBox.Show(PictureMedFullPath + " - Ошибка сохранения картинки при создании типового лекарства: " + ex.Message);
                }
            }

            try
            {
                command.CommandText = "INSERT INTO BasicMeds(Name, Prescribing, DosageForm, Description, PharmEffect, IndicationsForUse, " +
                    "Contraindications, Dosing, StorageConditions, Recipe) VALUES('" + NameBasicMed + "', '" + PrescribingBasicMed +
                    "', '" + DosageFormBasicMed + "', '" + DescriptionBasicMed + "', '" + PharmEffectBasicMed + "', '" +
                    IndicationsForUseBasicMed + "', '" + ContraindicationsBasicMed + "', '" + DosingBasicMed + "', '" +
                    StorageConditionsBasicMed + "', '" + RecipeBasicMed + "')";
                command.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Ошибка занесения типового лекарства в базу данных: " + ex.Message);

                return;
            }

            AddMedTextBox1.Text = NameBasicMed;
            SearchMedButton.PerformClick();
            FoundMedsListBox.Select();
            FoundMedsListBox.SetSelected(FoundMedsListBox.Items.IndexOf(flutext(NameBasicMed)), true);

            Message(15);
            NameBasicMedTextBox.Clear();
            PrescribingBasicMedComboBox.Text = "";
            DosageFormBasicMedComboBox.Text = "";
            DescriptionBasicMedTextBox.Clear();
            PharmEffectBasicMedTextBox.Clear();
            IndicationsForUseBasicMedTextBox.Clear();
            ContraindicationsBasicMedTextBox.Clear();
            DosingBasicMedTextBox.Clear();
            StorageConditionsBasicMedTextBox.Clear();
            RecipeBasicMedTextBox.Clear();
            CreateBasicMedPictureBox.Image = null;
            CreateBasicMedPanel.Visible = false;
            CreateBasicMedPanel.Enabled = false;
        }

        //Добавить картинку для лекарства при создании типового лекарства
        private void CreateBasicMedPictureBox_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Image Files|*.jpg";
            openFileDialog1.Title = "Select a Image (jpg) File";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StreamReader sr = new
                StreamReader(openFileDialog1.FileName);
                Image img = Image.FromFile(openFileDialog1.FileName);

                if (MedInfoPanel.Enabled is true)
                {
                    SelectedMedPictureBox.Image = img;
                    SelectedMedPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    medpic = true;
                    sr.Close();
                    //img.Dispose();

                    return;
                }

                CreateBasicMedPictureBox.Image = img;
                CreateBasicMedPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                sr.Close();
                //img.Dispose();
            }
        }

        //Действие при нажатии карточки лекарства в аптечке
        private void MedPictureBox_Click(object sender, EventArgs e)
        {
            UseUserComboBox.Items.Clear();

            PictureBox pb = sender as PictureBox;
            string MedName = flltext(pb.Image.Tag.ToString());
            string MedCount;

            SelectedMedPrescribingComboBox.Items.Clear();
            SelectedMedDosageFormComboBox.Items.Clear();

            ShowPrescribing(SelectedMedPrescribingComboBox);
            ShowDosageForm(SelectedMedDosageFormComboBox);

            command.CommandText = "SELECT Name, sum(Count) as Count, (SELECT Prescribing FROM BasicMeds where name = MedKit.Name) as Prescribing, (SELECT DosageForm FROM BasicMeds where name = MedKit.Name) as DosageForm,  (SELECT Description FROM BasicMeds where name = MedKit.Name) as Description,  (SELECT PharmEffect FROM BasicMeds where name = MedKit.Name) as PharmEffect,  (SELECT IndicationsForUse FROM BasicMeds where name = MedKit.Name) as IndicationsForUse,  (SELECT Contraindications FROM BasicMeds where name = MedKit.Name) as Contraindications,  (SELECT Dosing FROM BasicMeds where name = MedKit.Name) as Dosing,  (SELECT StorageConditions FROM BasicMeds where name = MedKit.Name) as StorageConditions,  (SELECT Recipe FROM BasicMeds where name = MedKit.Name) as Recipe FROM MedKit WHERE Name = '" + MedName + "'";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        SelectedMedNameLabel.Text = flutext(dt.Rows[0][dt.Columns[i]].ToString());
                        SelectedMedPictureBox.Image = pb.Image;
                        SelectedMedPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        break;
                    case 1:
                        MedCount = dt.Rows[0][dt.Columns[i]].ToString();
                        SelectedMedCountLabel.Text = "Количество: " + MedCount;
                        break;
                    case 2:
                        SelectedMedPrescribingLabel.Text = flutext(dt.Rows[0][dt.Columns[i]].ToString());
                        break;
                    case 3:
                        SelectedDosageFormLabel.Text = flutext(dt.Rows[0][dt.Columns[i]].ToString());
                        break;
                    case 4:
                        SelectedMedDescriptionTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 5:
                        SelectedMedPharmEffectTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 6:
                        SelectedMedIndicationsForUseTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 7:
                        SelectedMedContraindicationTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 8:
                        SelectedMedDosingTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 9:
                        SelectedMedStorageConditionsTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                    case 10:
                        SelectedMedRecipeTextBox.Text = dt.Rows[0][dt.Columns[i]].ToString();
                        break;
                }

            }

            MedKitPanel.Enabled = false;
            MedKitPanel.Visible = false;

            SelectedMedDescriptionTextBox.Height = tabControl1.TabPages[0].Height / 3;
            SelectedMedStorageConditionsTextBox.Height = tabControl1.TabPages[0].Height / 3;
            SelectedMedRecipeTextBox.Height = tabControl1.TabPages[0].Height / 3;
            UseMedDateTimePicker.Value = DateTime.Now;

            command.CommandText = "SELECT Name FROM Users";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                UseUserComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }

            PanelON(MedInfoPanel);
        }

        /*--------------------------------------------------------------------------------------------------Горячие клавиши-----------*/
        //Поиск лекарства в таблице стандартных лекарств по нажатию Enter в textBox'е
        private void AddMedTextBox1_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchMedButton.PerformClick();
            }
        }

        /*--------------------------------------------------------------------------------------------------Функции-----------*/
        //Параметры создаваемой карточки лекарства в аптечке
        private void MedPanel(string name, string count, Bitmap picture)
        {
            Label label1 = new Label();
            Label label2 = new Label();
            PictureBox pb = new PictureBox();
            Panel panel = new Panel();

            //Название лекарства на карточке
            label1.Location = new Point(0, 0);
            label1.BackColor = Color.Transparent;
            if (name.Length >= 22)
            {
                label1.Text = name.Remove(22) + "...";
            }
            else
            {
                label1.Text = name;
            }
            label1.Size = new Size(300, 25);
            label1.Font = new Font("Calibri", 14.0F, FontStyle.Bold);
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Name = "qwe";

            //Количество лекарства на карточке
            label2.Location = new Point(0, 25);
            label2.BackColor = Color.Transparent;
            label2.Text = "Количество: " + count;
            label2.Size = new Size(300, 25);
            label2.Font = new Font("Calibri", 12.0F);
            label2.TextAlign = ContentAlignment.MiddleCenter;

            //Картинка карточки
            pb.Image = new Bitmap(picture);
            pb.Location = new Point(0, 50);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Height = 250;
            pb.Width = 300;
            pb.Image.Tag = name;
            pb.Click += new EventHandler(MedPictureBox_Click);

            //Панель карточки
            panel.Location = new Point(0, 0);
            panel.Height = 300;
            panel.Width = 300;


            //Название, количество и картинка на панель карточки и добавить на панель карточек
            this.Controls.Add(panel);
            panel.Controls.Add(label1);
            panel.Controls.Add(label2);
            panel.Controls.Add(pb);
            MedsFromKitFlowLayoutPanel.Controls.Add(panel);
        }

        //Зполнить панель карточками лекарств в аптечке
        private void FillMedsFromKitFlowLayoutPanel(string fi, int or)
        {
            while (MedsFromKitFlowLayoutPanel.Controls.Count > 0)
            {
                foreach (Control panel in MedsFromKitFlowLayoutPanel.Controls)
                {
                    MedsFromKitFlowLayoutPanel.Controls.Remove(panel);
                }
            }

            string filter = null;

            if (fi != "0")
            {
                filter = "WHERE Prescribing = '" + flltext(this.FilterComboBox.GetItemText(this.FilterComboBox.Items[Convert.ToInt32(fi)]).ToString()) + "'";
            }

            string order = null;
            if (or < 0)
            {
                or = 0;
            }
            switch (or)
            {
                case 0:
                    order = "Name";
                    break;
                case 1:
                    order = "Name DESC";
                    break;
                case 2:
                    order = "Count";
                    break;
                case 3:
                    order = "Count DESC";
                    break;
            }

            command.CommandText = "SELECT Name, sum(Count) as Count, (select Prescribing from BasicMeds WHERE Name = MedKit.Name) as Prescribing FROM MedKit " + filter + "group by Name order by " + order;
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);
            //int MedsCount = dt.Rows.Count;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string name = flutext(dt.Rows[i][dt.Columns[0]].ToString());
                string count = dt.Rows[i][dt.Columns[1]].ToString();
                string pic = ImagesPath + name + ".jpg";

                if (File.Exists(pic))
                {
                    Image img = Image.FromFile(pic, true);
                    Bitmap picture = new Bitmap(img);
                    MedPanel(name, count, picture);
                }
                else
                {
                    Bitmap picture = new Bitmap(Properties.Resources.DefaultMedPic);
                    MedPanel(name, count, picture);
                }
            }
        }

        //Закрыть карточку лекарства и вернуться в аптечку
        private void FromMediInfoToMedKitButton_Click(object sender, EventArgs e)
        {
            EditMedOff();
            KitToolStripMenuItem.PerformClick();
        }

        //При изменении главной формы программы
        private void MainForm_Resize(object sender, EventArgs e)
        {
            /*if (MedInfoPanel.Enabled == true)
            {
                SelectedMedDescriptionTextBox.Height = tabControl1.TabPages[0].Height / 3;
                SelectedMedStorageConditionsTextBox.Height = tabControl1.TabPages[0].Height / 3;
                SelectedMedRecipeTextBox.Height = tabControl1.TabPages[0].Height / 3;
            }*/
        }

        //Включить редактирование карточки
        private void EditMedOn()
        {
            //SelectedMedPrescribingComboBox.Items.Clear();
            //SelectedMedDosageFormComboBox.Items.Clear();

            EditMedInfoButton.Text = "Сохранить";
            //Назначение
            SelectedMedPrescribingComboBox.Visible = true;
            SelectedMedPrescribingComboBox.Enabled = true;
            SelectedMedPrescribingComboBox.Text = SelectedMedPrescribingLabel.Text;
            SelectedMedPrescribingLabel.Visible = false;
            SelectedMedPrescribingLabel.Enabled = false;
            //Лекарственная форма
            SelectedMedDosageFormComboBox.Enabled = true;
            SelectedMedDosageFormComboBox.Visible = true;
            SelectedMedDosageFormComboBox.Text = SelectedDosageFormLabel.Text;
            SelectedDosageFormLabel.Enabled = false;
            SelectedDosageFormLabel.Visible = false;
            //Описание
            SelectedMedDescriptionTextBox.ReadOnly = false;
            //Фармакологическое действие
            SelectedMedPharmEffectTextBox.ReadOnly = false;
            //Показания к применению
            SelectedMedIndicationsForUseTextBox.ReadOnly = false;
            //Противопоказания
            SelectedMedContraindicationTextBox.ReadOnly = false;
            //Способ применения и дозы
            SelectedMedDosingTextBox.ReadOnly = false;
            //Условия хранения
            SelectedMedStorageConditionsTextBox.ReadOnly = false;
            //Условия отпуска
            SelectedMedRecipeTextBox.ReadOnly = false;
            //Картинка
            SelectedMedPictureBox.Enabled = true;
            //Употребление
            UseLabel.Visible = false;
            UseUserComboBox.Visible = false;
            UseMedCountTextBox.Visible = false;
            UseMedDateTimePicker.Visible = false;
            UseMedButton.Visible = false;
            UseLabel.Enabled = false;
            UseUserComboBox.Enabled = false;
            UseMedCountTextBox.Enabled = false;
            UseMedDateTimePicker.Enabled = false;
            UseMedButton.Enabled = false;
        }

        //Выключить редактирование карточки
        private void EditMedOff()
        {
            EditMedInfoButton.Text = "Редактировать";
            //Описание
            SelectedMedDescriptionTextBox.ReadOnly = true;
            //Условия отпуска
            SelectedMedRecipeTextBox.ReadOnly = true;
            //Назначение
            SelectedMedPrescribingLabel.Visible = true;
            SelectedMedPrescribingLabel.Enabled = true;
            SelectedMedPrescribingLabel.Text = SelectedMedPrescribingComboBox.Text;
            SelectedMedPrescribingComboBox.Visible = false;
            SelectedMedPrescribingComboBox.Enabled = false;
            //Лекарственная форма
            SelectedDosageFormLabel.Enabled = true;
            SelectedDosageFormLabel.Visible = true;
            SelectedDosageFormLabel.Text = SelectedMedDosageFormComboBox.Text;
            SelectedMedDosageFormComboBox.Enabled = false;
            SelectedMedDosageFormComboBox.Visible = false;
            //Фармакологическое действие
            SelectedMedPharmEffectTextBox.ReadOnly = true;
            //Показания к применению
            SelectedMedIndicationsForUseTextBox.ReadOnly = true;
            //Противопоказания
            SelectedMedContraindicationTextBox.ReadOnly = true;
            //Способ применения и дозы
            SelectedMedDosingTextBox.ReadOnly = true;
            //Условия хранения
            SelectedMedStorageConditionsTextBox.ReadOnly = true;
            //Картинка
            SelectedMedPictureBox.Enabled = false;
            //Употребление
            UseLabel.Visible = true;
            UseLabel.Enabled = true;
            UseUserComboBox.Visible = true;
            UseUserComboBox.Enabled = true;
            UseMedCountTextBox.Visible = true;
            UseMedCountTextBox.Enabled = true;
            UseMedDateTimePicker.Visible = true;
            UseMedDateTimePicker.Enabled = true;
            UseMedButton.Visible = true;
            UseMedButton.Enabled = true;
        }

        //Редактировать данные типового лекарства
        private void EditMedInfoButton_Click(object sender, EventArgs e)
        {
            if (EditMedInfoButton.Text == "Редактировать")
            {
                EditMedOn();
                return;
            }

            DialogResult dr;
            dr = MessageBox.Show("Сохранить изменения?", "Редактирование типового лекарства", MessageBoxButtons.YesNo);

            try
            {
                if (dr == DialogResult.Yes)
                {
                    string imageName = SelectedMedNameLabel.Text;
                    string PictureMedFullPath = ImagesPath + imageName + ".jpg";

                    if (medpic)
                    {
                        System.IO.File.Delete(imageName + ".jpg");
                        try
                        {
                            SelectedMedPictureBox.Image.Save(PictureMedFullPath, ImageFormat.Jpeg);
                        }
                        catch (System.Runtime.InteropServices.ExternalException ex)
                        {
                            Message(16);
                            MessageBox.Show("Ошибка при сохранении картинки: " + ex.Message);
                        }
                    }

                    this.SelectedMedPictureBox.Image.Tag = imageName;

                    this.command.CommandText = "UPDATE BasicMeds SET Prescribing = '" + SelectedMedPrescribingComboBox.Text +
                    "', DosageForm = '" + SelectedMedDosageFormComboBox.Text +
                    "', Description = '" + SelectedMedDescriptionTextBox.Text +
                    "', PharmEffect = '" + SelectedMedPharmEffectTextBox.Text +
                    "', IndicationsForUse = '" + SelectedMedIndicationsForUseTextBox.Text +
                    "', Contraindications = '" + SelectedMedContraindicationTextBox.Text +
                    "', Dosing = '" + SelectedMedDosingTextBox.Text +
                    "', StorageConditions = '" + SelectedMedStorageConditionsTextBox.Text +
                    "', Recipe = '" + SelectedMedRecipeTextBox.Text +
                    "' WHERE Name = '" + flltext(SelectedMedNameLabel.Text) + "'";
                    this.command.ExecuteNonQuery();
                    Message(17);
                }
                else
                {
                    Message(25);
                }

            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                Message(18);
                throw;
            }

            EditMedOff();

            medpic = false;
        }

        //При нажатии картинки в карточке лекарства
        private void SelectedMedPictureBox_Click(object sender, EventArgs e)
        {
            CreateBasicMedPictureBox_Click(SelectedMedPictureBox, null);
        }

        //Создание списка назначения
        private void GetPrescribing()
        {
            PrescribingList.Clear();

            command.CommandText = "select Prescribing from BasicMeds group by Prescribing order by Prescribing";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                PrescribingList.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }
        }

        //При изменении фильтра
        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterAndSort();
        }

        //При изменении сортировки
        private void SortComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterAndSort();
        }

        //Передать изменения о новом фильтре и/или сортировке
        private void FilterAndSort()
        {
            SearchMedInKitTextBox.Clear();
            string fi = this.FilterComboBox.GetItemText(this.FilterComboBox.SelectedIndex);
            int or = this.SortComboBox.SelectedIndex;
            FillMedsFromKitFlowLayoutPanel(fi, or);
        }

        //Поиск лекартсв в аптечке
        private void SearchMedInKitTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchtext = flltext(SearchMedInKitTextBox.Text);

            foreach (Control panel in MedsFromKitFlowLayoutPanel.Controls)
            {
                ((Control)panel).Hide();
                foreach (Control name in panel.Controls)
                {
                    if (name == panel.Controls.Find("qwe", true)[0] && flltext(name.Text).Contains(searchtext))
                    {
                        ((Control)panel).Show();
                    }
                }
            }
        }

        //Сброс поиска, фильтра и сортировки
        private void ClearFilterAndSoftButton_Click(object sender, EventArgs e)
        {
            SearchMedInKitTextBox.Clear();
            FilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            FilterAndSort();
        }

        //Удалить пользователя
        private void DeleteUserButton_Click(object sender, EventArgs e)
        {
            string deluser = flltext(UsersListBox.SelectedItem.ToString());
            command.CommandText = "delete from Users where name = '" + deluser + "'";
            command.ExecuteNonQuery();

            UsersListBox.Items.RemoveAt(UsersListBox.SelectedIndex);
            UsersToolStripMenuItem.PerformClick();
        }

        //Данные - Пользователи
        private void UsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();
            PanelON(UsersPanel);

            try
            {
                dt = new DataTable();
                UsersListBox.Items.Clear();
                command.CommandText = "SELECT Name FROM Users ORDER BY Name";
                command.ExecuteNonQuery();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string t = flutext(dt.Rows[i][dt.Columns[0]].ToString());
                    UsersListBox.Items.Add(t);
                }
                NewUserTextBox.Focus();
                Message(19);
            }
            catch (SQLiteException ex)
            {
                Message(20);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Употребить лекарство
        private void UseMedButton_Click(object sender, EventArgs e)
        {
            int count = Convert.ToInt32(SelectedMedCountLabel.Text.Remove(0, 12));

            bool bo = !(!String.IsNullOrEmpty(UseUserComboBox.Text)
                && !String.IsNullOrWhiteSpace(UseUserComboBox.Text)
                && !String.IsNullOrEmpty(UseMedCountTextBox.Text)
                && !String.IsNullOrWhiteSpace(UseMedCountTextBox.Text)
                && count > 0
                && count >= Convert.ToInt32(UseMedCountTextBox.Text)
                );

            if (bo)
            {
                Message(21);
                return;
            }

            try
            {
                //Преобразование даты употребления в инт
                DateTime DTUseDate = UseMedDateTimePicker.Value;
                DTUseDate = DateTime.SpecifyKind(DTUseDate, DateTimeKind.Utc);
                DateTimeOffset DTOUseDate = DTUseDate;
                int iUseDate = Convert.ToInt32(DTOUseDate.ToUnixTimeSeconds());

                string name = flltext(SelectedMedNameLabel.Text);
                string user = flltext(UseUserComboBox.Text);
                int usecount = Convert.ToInt32(UseMedCountTextBox.Text);
                command.CommandText = "INSERT INTO UseHistory (User, MedName, Prescribing, Count, UseDate) VALUES ('" + user + "', '" + name + "', '" + flltext(SelectedMedPrescribingLabel.Text) + "', '" + usecount.ToString() + "', '" + iUseDate + "')";
                command.ExecuteNonQuery();
                
                for (int i = 0; i < Convert.ToInt32(usecount); i++)
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = "select Count from MedKit where Name = '" + name + "' ORDER BY BestBefore, Id LIMIT 1";
                    dt = new DataTable();
                    da = new SQLiteDataAdapter(command);
                    da.Fill(dt);

                    int cellValue = Convert.ToInt32(dt.Rows[0][dt.Columns[0]]);

                    if (cellValue != 0)
                    {
                        command.CommandText = "update MedKit Set Count = (select Count from MedKit where Name = '" + name + "' ORDER BY BestBefore, Id LIMIT 1) - 1 where Id = (select Id from MedKit where Name = '" + name + "' ORDER BY BestBefore, Id LIMIT 1)";
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        command.CommandText = "delete from MedKit where Id = (select Id from MedKit where Name = '" + name + "' ORDER BY BestBefore, Id LIMIT 1)";
                        command.ExecuteNonQuery();
                        i--;
                    }
                }

                command.CommandText = "delete from MedKit where Count = 0";
                command.ExecuteNonQuery();
                MessageLabel.Text = "Пользователь \"" + flutext(user) + "\" употребил лекарство в размере " + usecount.ToString() + " ед.";

                if (usecount == count)
                {
                    KitToolStripMenuItem.PerformClick();
                }

                SelectedMedCountLabel.Text = "Количество: " + (count - usecount);

                UseUserComboBox.Text = null;
                UseMedCountTextBox.Text = null;
                UseMedDateTimePicker.Value = DateTime.Now;

            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                return;
            }
        }

        //Очистка базы данных и удаление картинок лекарств
        private void ClearDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dr;
            dr = MessageBox.Show("При нажатии ДА данные удалятся и программа будет закрыта. Вы уверены, что хотите очистить базу данных программы?", "Очистка базы данных", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                command.CommandText = "select 'drop table ' || name || ';' from sqlite_master where type = 'table';";
                command.ExecuteNonQuery();
                dt = new DataTable();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][dt.Columns[0]].ToString() != "drop table sqlite_sequence;")
                    {
                        command.CommandText = dt.Rows[i][dt.Columns[0]].ToString();
                        command.ExecuteNonQuery();
                    }
                }

                System.IO.DirectoryInfo di = new DirectoryInfo("Images");

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                this.Close();
            }
        }

        //Окрашивание в цвет срока годности в меню
        private void SelectColorBestBefore(int sw)
        {
            switch (sw)
            {
                case 0:
                    BestBeforeToolStripMenuItem1.BackColor = Color.LightGreen;
                    break;
                case 1:
                    BestBeforeToolStripMenuItem1.BackColor = Color.Orange;
                    break;
                case 2:
                    BestBeforeToolStripMenuItem1.BackColor = Color.Red;
                    break;
            }
        }

        //Проверка срока годности лекарств в аптечке
        private void CheckBestBefore()
        {
            command.CommandText = "SELECT Name, BestBefore FROM MedKit";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int g = 0;
            int o = 0;
            int r = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int result = Convert.ToInt32(dt.Rows[i][dt.Columns[1]].ToString()) - todayint;

                if (result < 0)
                {
                    r++;
                }
                else if (result < 2629743 && result > 0)
                {
                    o++;
                }
                else
                {
                    g++;
                }
            }

            if (r > 0)
            {
                SelectColorBestBefore(2);
            }
            else if (o > 0)
            {
                SelectColorBestBefore(1);
            }
            else
            {
                SelectColorBestBefore(0);
            }
        }

        //Меню - Срок годности
        private void BestBeforeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            HidePanels();

            try
            {
                FillBBPanel();
                PanelON(BBPanel);

                Message(22);
            }
            catch (SQLiteException ex)
            {
                Message(23);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Заполнение срока годности карточками лекарств
        private void FillBBPanel()
        {
            while (BBFlowLayoutPanel.Controls.Count > 0)
            {
                foreach (Control panel in BBFlowLayoutPanel.Controls)
                {
                    BBFlowLayoutPanel.Controls.Remove(panel);
                }
            }

            command.CommandText = "SELECT Id, Name, Count, BestBefore FROM MedKit";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string bbs = dt.Rows[i][dt.Columns[3]].ToString();
                int bb = Convert.ToInt32(bbs);
                int result = bb - todayint;
                string name = flutext(dt.Rows[i][dt.Columns[1]].ToString());
                string pic = ImagesPath + name + ".jpg";
                int id = Convert.ToInt32(dt.Rows[i][dt.Columns[0]].ToString());
                string count = dt.Rows[i][dt.Columns[2]].ToString();

                DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                dateTime = dateTime.AddSeconds(bb);
                string bestbefore = dateTime.ToShortDateString();
                
                if (File.Exists(pic) && result < 2629743)
                {
                    Image img = Image.FromFile(pic, true);
                    Bitmap picture = new Bitmap(img);
                    BBMedPanel(name, result, picture, id, count, bestbefore);
                }
                else if (result < 2629743)
                {
                    Bitmap picture = new Bitmap(Properties.Resources.DefaultMedPic);
                    BBMedPanel(name, result, picture, id, count, bestbefore);
                }
            }
        }

        //Настройка карточки срока годности
        private void BBMedPanel(string name, int result, Bitmap picture, int id, string count, string bestbefore)
        {
            Label label1 = new Label();
            Label label2 = new Label();
            Label label3 = new Label();
            Button button1 = new Button();
            PictureBox pb = new PictureBox();
            Panel panel = new Panel();

            //Название лекарства на карточке
            label1.Location = new Point(0, 0);
            label1.BackColor = Color.Transparent;
            if (name.Length >= 22)
            {
                label1.Text = name.Remove(22) + "...";
            }
            else
            {
                label1.Text = name;
            }
            label1.Size = new Size(300, 25);
            label1.Font = new Font("Calibri", 14.0F, FontStyle.Bold);
            label1.TextAlign = ContentAlignment.MiddleCenter;

            //Количество лекарства на карточке
            label2.Location = new Point(0, 25);
            label2.BackColor = Color.Transparent;
            label2.Text = "Количество: " + count;
            label2.Size = new Size(300, 25);
            label2.Font = new Font("Calibri", 12.0F, FontStyle.Bold);
            label2.TextAlign = ContentAlignment.MiddleCenter;

            //Годен до на карточке
            label3.Location = new Point(0, 50);
            label3.BackColor = Color.Transparent;
            label3.Text = "Годен до: " + bestbefore;
            label3.Size = new Size(300, 25);
            label3.Font = new Font("Calibri", 12.0F, FontStyle.Bold);
            label3.TextAlign = ContentAlignment.MiddleCenter;

            //Картинка карточки
            pb.Image = new Bitmap(picture);
            pb.Location = new Point(0, 75);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Height = 300;
            pb.Width = 300;

            //Кнопка карточки
            button1.Location = new Point(0, 375);
            button1.BackColor = Color.Transparent;
            button1.Size = new Size(300, 45);
            button1.Font = new Font("Calibri", 14.0F, FontStyle.Bold);
            button1.Text = "Удалить";
            button1.Click += new EventHandler(DelMedFromBBButton_Click);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Tag = id;

            if (result <= 0)
            {
                button1.BackColor = Color.Red;
            }
            else if (result > 0 && result < 2629743)
            {
                button1.BackColor = Color.Orange;
            }
            else
            {
                button1.BackColor = Color.LightGreen;
            }

            //Панель карточки
            panel.Location = new Point(0, 0);
            panel.Height = 420;
            panel.Width = 300;

            //Название, количество и картинка на панель карточки и добавить на панель карточек
            this.Controls.Add(panel);
            panel.Controls.Add(label1);
            panel.Controls.Add(label2);
            panel.Controls.Add(label3);
            panel.Controls.Add(button1);
            panel.Controls.Add(pb);
            BBFlowLayoutPanel.Controls.Add(panel);
        }

        //Списание лекарства из аптечке из-за срока годности
        private void DelMedFromBBButton_Click(object sender, EventArgs e)
        {
            Button button1 = sender as Button;
            string id = button1.Tag.ToString();

            DialogResult dr;
            dr = MessageBox.Show("Удалить?", "Удаление лекарства", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                try
                {
                    command.CommandText = "select Name, Count from MedKit where Id = '" + id + "'";
                    command.ExecuteNonQuery();
                    dt = new DataTable();
                    da = new SQLiteDataAdapter(command);
                    da.Fill(dt);
                    
                    string woffmed = flltext(dt.Rows[0][dt.Columns[0]].ToString());
                    int cnt = Convert.ToInt32(dt.Rows[0][dt.Columns[1]].ToString());

                    command.CommandText = "select Prescribing from BasicMeds where Name = '" + woffmed + "'";
                    command.ExecuteNonQuery();
                    dt = new DataTable();
                    da = new SQLiteDataAdapter(command);
                    da.Fill(dt);

                    string prescr = flltext(dt.Rows[0][dt.Columns[0]].ToString());
                    
                    int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    command.CommandText = "INSERT INTO UseHistory (User, MedName, Prescribing, Count, UseDate)" +
                        "VALUES ('списано', '" + woffmed + "', '" + prescr + "', '" + cnt + "', '" + todayint + "')";
                    command.ExecuteNonQuery();

                    command.CommandText = "delete from MedKit where Id = '" + id + "'";
                    command.ExecuteNonQuery();

                    Message(24);

                    CheckBestBefore();
                    FillBBPanel();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Ошибка удаления лекарства: " + ex.Message);
                    return;
                }
            }
        }

        //Назначение в создании лекарства
        private void ShowPrescribing(ComboBox box)
        {
            command.CommandText = "select Prescribing from BasicMeds group by Prescribing";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                box.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }
        }

        //Лекарственная форма в создании лекарства
        private void ShowDosageForm(ComboBox box)
        {
            command.CommandText = "select DosageForm from BasicMeds group by DosageForm";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                box.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }
        }
        
        //Сделать панель видимой
        private void PanelON(Panel p)
        {
            p.Visible = true;
            p.Enabled = true;
        }

        //Текстбокс - цифры && . || ,
        private void InputOnlyNumbersDotTextBox(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && (e.KeyChar != '.')
                && (e.KeyChar != ','))
            {
                e.Handled = true;
            }
            
            if (((e.KeyChar == '.') || (e.KeyChar == ',')) && (((sender as TextBox).Text.IndexOf('.') > -1) || ((sender as TextBox).Text.IndexOf(',') > -1)))
            {
                e.Handled = true;
            }
        }

        //Текстбокс - только цифры и ноль не может быть первым символом
        private void InputOnlyNumbersTextBox(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        //Ввод количества лекарства в аптечку
        private void CountTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputOnlyNumbersTextBox(CountTextBox, e);
        }

        //Ввод стоимости лекарства в аптечку
        private void PriceTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (PriceTextBox.Text.StartsWith(".") || PriceTextBox.Text.StartsWith(","))
            {
                PriceTextBox.Text = PriceTextBox.Text.Insert(0, "0");
                PriceTextBox.SelectionStart = PriceTextBox.Text.Length;
            }
            
            InputOnlyNumbersDotTextBox(PriceTextBox, e);
        }

        //Текст сопроводительного сообщения в MessageLabel
        private void Message(int i)
        {
            string output = null;
            switch (i)
            {
                case 0:
                    output = "Не подключено";
                    break;
                case 1:
                    output = "Программа успешно запущена";
                    break;
                case 2:
                    output = "Программа не запущена корректно. Закройте программу.";
                    break;
                case 3:
                    output = "Аптечка успешно открыта";
                    break;
                case 4:
                    output = "Не удалось загрузить данные из аптечки";
                    break;
                case 5:
                    output = "Форма добавления лекарства открыта";
                    break;
                case 6:
                    output = "Пользователь существует или не введено имя нового пользователя";
                    break;
                case 7:
                    output = "Пользователь создан";
                    break;
                case 8:
                    output = "Не удалось создать пользователя";
                    break;
                case 9:
                    output = "Не удалось создать пользователя";
                    break;
                case 10:
                    output = "Лекарства не найдены. Создайте новое типовое лекарство";
                    break;
                case 11:
                    output = "Лекарства не найдены";
                    break;
                case 12:
                    output = "Дата покупки не может быть позднее срока годности";
                    break;
                case 13:
                    output = "Лекарство добавлено в аптечку";
                    break;
                case 14:
                    output = "Не удалось добавить лекарство в аптечку";
                    break;
                case 15:
                    output = "Лекарство создано";
                    break;
                case 16:
                    output = "Не удалось сохранить картинку лекарства";
                    break;
                case 17:
                    output = "Изменения сохранены";
                    break;
                case 18:
                    output = "Изменения не сохранены";
                    break;
                case 19:
                    output = "Cписок пользователей cформирован";
                    break;
                case 20:
                    output = "Не удалось получить список пользователей";
                    break;
                case 21:
                    output = "Не заполнено обязательное поле или введено количество больше чем в аптечке";
                    break;
                case 22:
                    output = "Срок годности лекарств успешно открыт";
                    break;
                case 23:
                    output = "Не удалось загрузить данные из аптечки";
                    break;
                case 24:
                    output = "Лекарство списано";
                    break;
                case 25:
                    output = "Изменения отменены";
                    break;
                case 26:
                    output = "Лекарство с таким именем уже существует";
                    break;
                case 27:
                    output = "Не заполнено обязательное поле";
                    break;
                case 28:
                    output = "Типовое лекарство удалено";
                    break;
                case 29:
                    output = "Не выбрано лекарство";
                    break;
                case 30:
                    output = "Не указано количество";
                    break;
                default:
                    output = null;
                    break;
            }

            MessageLabel.Text = output;
        }

        //Ограничение ввода стоимости до двух символов после разделителя
        private void PriceTextBox_TextChanged(object sender, EventArgs e)
        {
            int i = PriceTextBox.Text.IndexOf(".");
            if ((i != -1) && (i == PriceTextBox.Text.Length - 4))
            {
                PriceTextBox.Text = PriceTextBox.Text.Substring(0, PriceTextBox.Text.Length - 1);
                PriceTextBox.SelectionStart = PriceTextBox.Text.Length;
            }

            int j = PriceTextBox.Text.IndexOf(",");
            if ((j != -1) && (j == PriceTextBox.Text.Length - 4))
            {
                PriceTextBox.Text = PriceTextBox.Text.Substring(0, PriceTextBox.Text.Length - 1);
                PriceTextBox.SelectionStart = PriceTextBox.Text.Length;
            }
        }

        //Запрет начинать название типового лекарства с пробела
        private void NameBasicMedTextBox_TextChanged(object sender, EventArgs e)
        {
            NameBasicMedTextBox.Text = NameBasicMedTextBox.Text.TrimStart();
        }

        //Удалить из названия типового лекарства пробелы в конце
        private void NameBasicMedTextBox_Leave(object sender, EventArgs e)
        {
            NameBasicMedTextBox.Text = NameBasicMedTextBox.Text.TrimEnd();
        }

        //Нажатие ентер при создании пользователя
        private void NewUserTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateUserButton.PerformClick();
                NewUserTextBox.Select();
            }
        }

        //Только цифры в употреблении
        private void UseMedCountTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputOnlyNumbersTextBox(UseMedCountTextBox, e);
        }

        //Убирать ноль в начале количества употребления
        private void UseMedCountTextBox_Leave(object sender, EventArgs e)
        {
            if (UseMedCountTextBox.Text.StartsWith("0"))
            {
                UseMedCountTextBox.Text.Remove(0, 1);
            }

            UseMedCountTextBox.Text = UseMedCountTextBox.Text.TrimEnd();
        }
        
        //Запрет начинать назначение с пробела
        private void PrescribingBasicMedComboBox_TextChanged(object sender, EventArgs e)
        {
            PrescribingBasicMedComboBox.Text = PrescribingBasicMedComboBox.Text.TrimStart();
        }

        //Удалить из назначения пробелы в конце
        private void PrescribingBasicMedComboBox_Leave(object sender, EventArgs e)
        {
            PrescribingBasicMedComboBox.Text = PrescribingBasicMedComboBox.Text.TrimEnd();
        }

        //Запрет начинать лекарственную форму с пробела
        private void DosageFormBasicMedComboBox_TextChanged(object sender, EventArgs e)
        {
            DosageFormBasicMedComboBox.Text = DosageFormBasicMedComboBox.Text.TrimStart();
        }

        //Удалить из лекарственной формы пробелы в конце
        private void DosageFormBasicMedComboBox_Leave(object sender, EventArgs e)
        {
            DosageFormBasicMedComboBox.Text = DosageFormBasicMedComboBox.Text.TrimEnd();
        }

        //Создать новое лекарство
        private void NewMedButton_Click(object sender, EventArgs e)
        {
            PrescribingBasicMedComboBox.Items.Clear();
            DosageFormBasicMedComboBox.Items.Clear();

            PanelON(CreateBasicMedPanel);
            ShowPrescribing(PrescribingBasicMedComboBox);
            ShowDosageForm(DosageFormBasicMedComboBox);
            NameBasicMedTextBox.Focus();
        }

        //Удаление стандартного лекарства
        private void DelBasicMedButton_Click(object sender, EventArgs e)
        {
            string delname = flltext(FoundMedsListBox.SelectedItem.ToString());
            int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int cnt;

            DialogResult dr;
            dr = MessageBox.Show("Удалить типивое лекарство из базы данных?","Удаление лекарства", MessageBoxButtons.YesNo);

            if (dr == DialogResult.No)
            {
                return;
            }

            command.CommandText = "select sum(Count) from MedKit where Name = '" + delname + "'";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            if (dt.Rows[0][dt.Columns[0]].ToString() != "")
            {
                cnt = Convert.ToInt32(dt.Rows[0][dt.Columns[0]].ToString());

                command.CommandText = "select Prescribing from BasicMeds where Name = '" + delname + "'";
                command.ExecuteNonQuery();
                dt = new DataTable();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);
                string prescr = dt.Rows[0][dt.Columns[0]].ToString();

                command.CommandText = "INSERT INTO UseHistory (User, MedName, Prescribing, Count, UseDate)" +
                "VALUES ('списано', '" + delname + "', '" + prescr + "', '" + cnt + "', '" + todayint + "')";
                command.ExecuteNonQuery();

                command.CommandText = "delete from MedKit where Name = '" + delname + "'";
                command.ExecuteNonQuery();
            }
            
            command.CommandText = "delete from BasicMeds where Name = '" + delname + "'";
            command.ExecuteNonQuery();

            CheckBestBefore();

            SearchMedButton.PerformClick();

            Message(28);
        }

        //Кнопка списать
        private void WrittenOffButton_Click(object sender, EventArgs e)
        {
            DialogResult dr;
            dr = MessageBox.Show("Списать остаток лекарства из аптечки?", "Списание лекарства", MessageBoxButtons.YesNo);

            if (dr == DialogResult.No)
            {
                return;
            }

            string woffmed = flltext(SelectedMedNameLabel.Text);
            string prescr = flltext(SelectedMedPrescribingLabel.Text);
            int cnt = Convert.ToInt32(SelectedMedCountLabel.Text.Remove(0, 12));
            int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            command.CommandText = "INSERT INTO UseHistory (User, MedName, Prescribing, Count, UseDate)" +
                "VALUES ('списано', '" + woffmed + "', '" + prescr + "', '" + cnt + "', '" + todayint + "')";
            command.ExecuteNonQuery();

            command.CommandText = "delete from MedKit where Name = '" + woffmed + "'";
            command.ExecuteNonQuery();

            KitToolStripMenuItem.PerformClick();
        }

        //Файл - Помощь
        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();

            PanelON(HelpPanel);


        }

        //Отчеты - Употребление лекарств
        private void UsingStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();

            USFillComboBoxes();

            PanelON(UsingStatsPanel);
            USClearFilterButton.PerformClick();
        }

        //Заполнение списков в употреблении
        private void USFillComboBoxes()
        {
            USUsersComboBox.Items.Clear();
            USMedsComboBox.Items.Clear();
            USPrescribingComboBox.Items.Clear();

            command.CommandText = "Select User, MedName, Prescribing from UseHistory";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            USUsersComboBox.Items.Add("Все");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!USUsersComboBox.Items.Contains(flutext(dt.Rows[i][dt.Columns[0]].ToString()))
                    && dt.Rows[i][dt.Columns[0]].ToString() != "списано")
                {
                    USUsersComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
                }
            }

            USMedsComboBox.Items.Add("Все");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!USMedsComboBox.Items.Contains(flutext(dt.Rows[i][dt.Columns[1]].ToString())))
                {
                    USMedsComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[1]].ToString()));
                }
            }

            USPrescribingComboBox.Items.Add("Все");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!USPrescribingComboBox.Items.Contains(flutext(dt.Rows[i][dt.Columns[2]].ToString())))
                {
                    USPrescribingComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[2]].ToString()));
                }
            }
        }

        //Сбросить фильтр статистики употребления
        private void USClearFilterButton_Click(object sender, EventArgs e)
        {
            USUsersComboBox.SelectedIndex = 0;
            USMedsComboBox.SelectedIndex = 0;
            USPrescribingComboBox.SelectedIndex = 0;
            USPeriodStartDateTimePicker.Value = new DateTime(1970, 1, 1);
            USPeriodEndDateTimePicker.Value = DateTime.Today;
            
            USFilter();
        }

        //Построение графика употребления
        private void USChartBuild(string sUser, string sMedName, string sPrescribing, DateTime start, DateTime end)
        {
            StatsUsingChart.Series.Clear();

            string andUser = "And User = '" + sUser + "'";
            string andMedName = "And MedName = '" + sMedName + "'";
            string andPrescribing = "And Prescribing = '" + sPrescribing + "'";

            if (sUser == "все")
            {
                andUser = null;
            }
            if (sMedName == "все")
            {
                andMedName = null;
            }
            if (sPrescribing == "все")
            {
                andPrescribing = null;
            }

            int day = 86400;

            //Преобразование даты начала периода в инт
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            DateTimeOffset DTOstart = start;
            int istart = Convert.ToInt32(DTOstart.ToUnixTimeSeconds()) / day * day;
            
            //Преобразование даты конца периода в инт
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
            DateTimeOffset DTOend = end;
            int iend = Convert.ToInt32(DTOend.ToUnixTimeSeconds()) / day * day + day - 1;
            
            command.CommandText = "Select Count, UseDate, User, Prescribing, MedName from UseHistory Where UseDate >= '" + istart + "' And UseDate <= '" + iend +
                "' " + andUser + " " + andMedName + " " + andPrescribing + " ORDER BY UseDate";
            //command.Parameters[0].Value='f'
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            DataTable dt2 = new DataTable();
            dt2.Columns.Add("UseDate", typeof(DateTime));
            dt2.Columns.Add("Count", typeof(int));

            int sumCount = 0;
            

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i][dt.Columns[2]].ToString() != "списано")
                {
                    int iUseDate = Convert.ToInt32(dt.Rows[i][dt.Columns[1]].ToString());
                    int iCount = Convert.ToInt32(dt.Rows[i][dt.Columns[0]].ToString());

                    DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                    dateTime = dateTime.AddSeconds(iUseDate);
                    dt2.Rows.Add(dateTime, iCount);
                    sumCount += Convert.ToInt32(dt.Rows[i][dt.Columns[0]].ToString());
                }
            }
            string serName = "Результат";
            
            if(sUser != "" && sMedName != "" && sPrescribing != "")
            {
                serName = flutext(sUser) + " + " + flutext(sPrescribing) + " + " + flutext(sMedName);
                string uc = "Пользователь \"" + flutext(sUser) + "\" употребил ";
                string um = " ед. лекарства " + flutext(sMedName);
                string up = " по направлению \"" + flutext(sPrescribing) + "\"";
                if (sUser == "все")
                {
                    uc = "Все пользователи употребили ";
                }
                if (sMedName == "все")
                {
                    um = " ед. лекарств";
                }
                if (sPrescribing == "все")
                {
                    up = "";
                }
                CurrentUserLabel.Text = uc + sumCount + um + up + " за выбранный период.";
            }

            for (int i = 0; i < 3; i++)
            {
                string groupby = "";
                switch (i)
                {
                    case 0:
                        groupby = "User";
                        break;
                    case 1:
                        groupby = "MedName";
                        break;
                    case 2:
                        groupby = "Prescribing";
                        break;
                }
                command.CommandText = "Select sum(Count) as SumCount, UseDate, User, MedName, Prescribing from UseHistory Where User <> 'списано' group by " + groupby + " order by SumCount DESC LIMIT 1";
                command.ExecuteNonQuery();
                dt = new DataTable();
                da = new SQLiteDataAdapter(command);
                da.Fill(dt);
                AllUserTextBox.Height = (groupBox2.Height - 9) / 4;
                AllMedTextBox.Height = (groupBox2.Height - 9) / 4;
                AllPrescribingTextBox.Height = (groupBox2.Height - 9) / 4;
                switch (i)
                {
                    /*case 0:
                        AllUserTextBox.Text = flutext(dt.Rows[0][dt.Columns[2]].ToString()) + " употребил больше всех лекарств: " + dt.Rows[0][dt.Columns[0]].ToString() + " ед. за все время.";
                        break;*/
                    case 1:
                        AllMedTextBox.Text = "Больше всего употреблено лекарства \"" + flutext(dt.Rows[0][dt.Columns[3]].ToString()) + "\" в размере " + dt.Rows[0][dt.Columns[0]].ToString() + " ед. за все время.";
                        break;
                    case 2:
                        AllPrescribingTextBox.Text = "Самым употребляемым направлением является \"" + flutext(dt.Rows[0][dt.Columns[4]].ToString()) + "\". Употреблено: " + dt.Rows[0][dt.Columns[0]].ToString() + " ед. за все время.";
                        break;
                }
            }

            StatsUsingChart.Series.Add(serName);
            StatsUsingChart.Series[serName].ChartType = SeriesChartType.Column;
            StatsUsingChart.Series[serName].IsXValueIndexed = true;
            StatsUsingChart.Series[serName].XValueMember = "UseDate";
            StatsUsingChart.Series[serName].YValueMembers = "Count";

            StatsUsingChart.DataSource = dt2;
            StatsUsingChart.DataBind();
        }

        //Передать изменения о новых условиях статистики употребления
        private void USFilter()
        {
            string sUser = flltext(USUsersComboBox.Text);
            string sMedName = flltext(USMedsComboBox.Text);
            string sPrescribing = flltext(USPrescribingComboBox.Text);
            DateTime start = USPeriodStartDateTimePicker.Value;
            DateTime end = USPeriodEndDateTimePicker.Value;
            USChartBuild(sUser, sMedName, sPrescribing, start, end);
        }
        
        private void USUsersComboBox_TextChanged(object sender, EventArgs e)
        {
            USFilter();
        }

        private void USPrescribingComboBox_TextChanged(object sender, EventArgs e)
        {
            USFilter();
        }

        private void USMedsComboBox_TextChanged(object sender, EventArgs e)
        {
            USFilter();
        }

        private void USPeriodStartDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            USFilter();
        }

        private void USPeriodEndDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            USFilter();
        }

        //Запрет начинать название аптеки с пробела
        private void PharmacyComboBox_TextChanged(object sender, EventArgs e)
        {
            PharmacyComboBox.Text = PharmacyComboBox.Text.TrimStart();
        }

        //Удалить из навзания аптеки пробелы в конце
        private void PharmacyComboBox_Leave(object sender, EventArgs e)
        {
            PharmacyComboBox.Text = PharmacyComboBox.Text.TrimEnd();
        }

        //Запрет начинать адрес аптеки с пробела
        private void AddressComboBox_TextChanged(object sender, EventArgs e)
        {
            AddressComboBox.Text = AddressComboBox.Text.TrimStart();
        }

        //Удалить из адреса аптеки пробелы в конце
        private void AddressComboBox_Leave(object sender, EventArgs e)
        {
            AddressComboBox.Text = AddressComboBox.Text.TrimEnd();
        }

        private void MapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();

            PanelON(MapPanel);
        }

        private void MapSearchButton_Click(object sender, EventArgs e)
        {
            string mapPharmacy = MapPharmacyTextBox.Text;
            string mapAddress = MapAddressTextBox.Text;

            try
            {
                StringBuilder querydata = new StringBuilder();
                querydata.Append("http://maps.google.com/maps?q=");

                if (mapPharmacy != String.Empty)
                {
                    querydata.Append(mapPharmacy + ",+");
                }
                if (mapAddress != String.Empty)
                {
                    querydata.Append(mapAddress + ",+");
                }
                
                //string MyHTML = File.ReadAllText(Convert.ToString(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)) + "\\Map.html");
                string MyHTML = File.ReadAllText(@"C:\Users\smol\Documents\Visual Studio 2017\Projects\HomeMedicines\HomeMedicines\Map.html");
                DisplayHtml(MyHTML);

                //webBrowser1.Navigate(querydata.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message.ToString());
            }
        }

        private void DisplayHtml(string html)
        {
            webBrowser1.Navigate("about:blank");
            if (webBrowser1.Document != null)
            {
                webBrowser1.Document.Write(string.Empty);
            }
            webBrowser1.DocumentText = html;
        }

        //Взять данные и создать график "пирог"
        private void FinanceChartFill()
        {
            try
            {
                string sPrescribing = MPrescribingComboBox.Text;
                string sName = MMedComboBox.Text;
                int group = 0;
                if (sPrescribing != "Все")
                {
                    group = 1;
                }
                DateTime start = MStartDateTimePicker.Value;
                DateTime end = MEndDateTimePicker.Value;
                SumChartBuild(sPrescribing, sName, group, start, end);
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                return;
            }
        }

        //Отчеты - Расходы на лекарства
        private void FinanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();

            try
            {
                PanelON(FinancePanel);

                ClearFinanceButton.PerformClick();
                //FinanceDataGridViewFill();
                //FinanceChartFill();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                return;
            }
        }

        //Заполнить таблицу покупок лекарств
        private void FinanceDataGridViewFill(string sPrescribing, string sName, DateTime start, DateTime end)
        {
            string andPrescribing = " And BasicMeds.Prescribing = '" + flltext(sPrescribing) + "'";
            string andName = " And BuyHistory.Name = '" + flltext(sName) + "'";

            if (sPrescribing == "Все")
            {
                andPrescribing = null;
            }
            if (sName == "Все")
            {
                andName = null;
            }

            int day = 86400;

            //Преобразование даты начала периода в инт
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            DateTimeOffset DTOstart = start;
            int istart = Convert.ToInt32(DTOstart.ToUnixTimeSeconds()) / day * day;

            //Преобразование даты конца периода в инт
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
            DateTimeOffset DTOend = end;
            int iend = Convert.ToInt32(DTOend.ToUnixTimeSeconds()) / day * day + day - 1;
            
            command.CommandText = "SELECT BasicMeds.Prescribing as 'Назначение', BuyHistory.Name as 'Лекарство', BuyHistory.Pharmacy as 'Аптека', " +
                "BuyHistory.Address as 'Адрес', BuyHistory.Count as 'Кол-во', BuyHistory.Price as 'Сумма', " +
                "(Select ROUND(BuyHistory.Price / BuyHistory.Count,2)) as 'Цена за ед.', " +
                "BuyHistory.BuyDate as 'Дата покупки' from BuyHistory " +
                "INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name " +
                "Where BuyHistory.BuyDate >= " + istart + " And BuyHistory.BuyDate <= " + iend +
                "" + andPrescribing + "" + andName + " ORDER BY BuyHistory.BuyDate DESC";

            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            //Клонирование таблицы, изменения типа данных колоник дата, занесение данных из исходной
            DataTable dtCloned = dt.Clone();
            dtCloned.Columns[7].DataType = typeof(String);
            foreach (DataRow row in dt.Rows)
            {
                dtCloned.ImportRow(row);
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    //Сделать заглавными Лекарства, Аптека, Адрес
                    if (j != 4 || j != 5)
                    {
                        if (dt.Rows[i][dt.Columns[j]].ToString() != "")
                        {
                            dtCloned.Rows[i].SetField(j, flutext(dt.Rows[i][dt.Columns[j]].ToString()));
                        }
                    }

                    //Конвертация даты покупки из инт в дату
                    if (j == 7)
                    {
                        DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                        dateTime = dateTime.AddSeconds(Convert.ToInt32(dt.Rows[i][dt.Columns[j]].ToString()));
                        string bestbefore = dateTime.ToShortDateString();

                        dtCloned.Rows[i].SetField(j, bestbefore);
                    }
                }
            }

            BindingSource bs = new BindingSource();
            bs.DataSource = dtCloned;

            //Заполнение таблицы данными
            FinanceDataGridView.DataSource = bs;
            FinanceDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        
        //Заполнить Назначение в Расходах на лекарства
        private void MPrescribingComboBoxFill()
        {
            MPrescribingComboBox.Items.Clear();
            
            command.CommandText = "select Prescribing from BasicMeds, BuyHistory where BasicMeds.Name = BuyHistory.Name group by Prescribing";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            MPrescribingComboBox.Items.Add("Все");

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                MPrescribingComboBox.Items.Add(flutext(dt.Rows[i][dt.Columns[0]].ToString()));
            }

            MPrescribingComboBox.SelectedItem = MPrescribingComboBox.Items[0];
        }

        //Заполнить Лекарства в Расходах на лекарства
        private void MMedComboBoxFill()
        {
            MMedComboBox.Items.Clear();

            string presc = "";
            
            if (MPrescribingComboBox.SelectedItem.ToString() != "Все")
            {
                presc = "where Presc = '" + flltext(MPrescribingComboBox.SelectedItem.ToString()) + "'";
            }
            
            command.CommandText = "Select BuyHistory.Name, (select BasicMeds.Prescribing from BasicMeds where BasicMeds.Name = BuyHistory.Name) as Presc from BuyHistory, BasicMeds " + presc + " group by BuyHistory.Name"; 
            command.ExecuteNonQuery();
            
            DataTable dt2 = new DataTable();
            SQLiteDataAdapter da2 = new SQLiteDataAdapter(command);
            dt2.Clear();
            da2.Fill(dt2);
            
            MMedComboBox.Items.Add("Все");
            
            for (int i = 0; i < dt2.Rows.Count; i++)
            {
                MMedComboBox.Items.Add(flutext(dt2.Rows[i][dt2.Columns[0]].ToString()));
            }

            MMedComboBox.SelectedItem = MMedComboBox.Items[0];
        }

        //Выбор Назначения в Расходах на лекарства
        private void MPrescribingComboBox_TextChanged(object sender, EventArgs e)
        {
            MMedComboBoxFill();
            FinanceFilterChanged();
        }

        //Выбор Лекарства в Расходах на лекарства
        private void MMedComboBox_TextChanged(object sender, EventArgs e)
        {
            FinanceFilterChanged();
        }

        //Финансы данные для фильтрации
        private void FinanceFilterChanged()
        {
            string sPrescribing = MPrescribingComboBox.Text;
            string sName = MMedComboBox.Text;
            DateTime sStart = MStartDateTimePicker.Value;
            DateTime sEnd = MEndDateTimePicker.Value;
            int group = 0;
            if (sPrescribing != "Все")
            {
                group = 1;
            }
            SumChartBuild(sPrescribing, sName, group, sStart, sEnd);
            FinanceDataGridViewFill(sPrescribing, sName, sStart, sEnd);
        }

        //Построение графика затрат "Пирог"
        private void SumChartBuild(string sPrescribing, string sName, int group, DateTime start, DateTime end)
        {
            SumChart.Series.Clear();

            string andPrescribing = " And BasicMeds.Prescribing = '" + flltext(sPrescribing) + "'";
            //string andName = " And BuyHistory.Name = '" + sName + "'";
            string iGroup = "";

            if (sPrescribing == "Все")
            {
                andPrescribing = null;
            }
            /*if (sName == "Все")
            {
                andName = null;
            }*/
            switch (group)
            {
                case 0:
                    iGroup = "BasicMeds.Prescribing";
                    break;
                case 1:
                    iGroup = "BuyHistory.Name";
                    break;
            }
            int day = 86400;

            //Преобразование даты начала периода в инт
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            DateTimeOffset DTOstart = start;
            int istart = Convert.ToInt32(DTOstart.ToUnixTimeSeconds()) / day * day;

            //Преобразование даты конца периода в инт
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
            DateTimeOffset DTOend = end;
            int iend = Convert.ToInt32(DTOend.ToUnixTimeSeconds()) / day * day + day - 1;
            command.CommandText = "SELECT BasicMeds.Prescribing, BuyHistory.Name, sum(BuyHistory.Price) as Price1, BuyHistory.BuyDate from BuyHistory " +
                "INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name Where BuyHistory.BuyDate >= " + istart + " And BuyHistory.BuyDate <= " + iend +
                "" + andPrescribing + " GROUP BY " + iGroup + " ORDER BY BuyHistory.BuyDate"; //" + andName + "
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            DataTable dt2 = new DataTable();
            dt2.Columns.Add("Label", typeof(string));
            dt2.Columns.Add("Price", typeof(double));
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string iLabel = "";
                if (group == 0)
                {
                    iLabel = flutext(dt.Rows[i][dt.Columns[0]].ToString());
                }
                else
                {
                    iLabel = flutext(dt.Rows[i][dt.Columns[1]].ToString());
                }
                 
                double iPrice = Convert.ToDouble(dt.Rows[i][dt.Columns[2]].ToString());
                dt2.Rows.Add(iLabel, iPrice);
            }
            string serName = "Итого";
            
            SumChart.Series.Add(serName);
            SumChart.Series[serName].ChartType = SeriesChartType.Pie;
            SumChart.Series[serName].IsXValueIndexed = true;
            SumChart.Series[serName].XValueMember = "Label";
            SumChart.Series[serName].YValueMembers = "Price";
            
            SumChart.DataSource = dt2;
            SumChart.DataBind();

            //SumChart.Series[serName].Label = dt2.Rows[cnt]["Price"].ToString(); //"#PERCENT{0.00 %}"
            SumChart.Series[serName].LegendText = "#VALX";

            for (int cnt = 0; cnt < SumChart.Series[serName].Points.Count; cnt++)
            {
                SumChart.Series[serName].Label = "#VALY"; //"#PERCENT{0.00 %}"
                SumChart.Series[serName].Points[cnt].ToolTip = "#PERCENT{0.00 %}"; // dt2.Rows[cnt]["Price"].ToString();
            }
        }

        //Очистить фильтр финансов
        private void ClearFinanceButton_Click(object sender, EventArgs e)
        {
            MPrescribingComboBoxFill();
            //MStartDateTimePicker.Value = DateTime.Today.AddDays(-365);
            MEndDateTimePicker.Value = DateTime.Today;
            MStartDateTimePicker.Value = DateTime.Today.AddDays(-30);
            if (FinanceToggleCheckBox.Text == "Месяц")
            {
                FinanceToggleCheckBox.Checked = false;
            }
        }

        //При изменении даты начала периода в финансах
        private void MStartDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            FinanceFilterChanged();
        }

        //При изменении даты конца периода в финансах
        private void MEndDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            FinanceFilterChanged();
        }

        //Выбор периода финансов год или месяц
        private void FinanceToggleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (FinanceToggleCheckBox.Text == "Год")
            {
                MStartDateTimePicker.Value = DateTime.Today.AddDays(-356);
                MEndDateTimePicker.Value = DateTime.Today;
                FinanceToggleCheckBox.Text = "Месяц";
            }
            else
            {
                MStartDateTimePicker.Value = DateTime.Today.AddDays(-30);
                MEndDateTimePicker.Value = DateTime.Today;
                FinanceToggleCheckBox.Text = "Год";
            }
        }

        //Отчеты - Цены на лекарства
        private void PriceMedsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HidePanels();
            PanelON(PriceMedsPanel);
            

            PrescribingPriceMedsComboBox.Items.Clear();

            try
            {
                GetPrescribing();
                PrescribingPriceMedsComboBox.Items.Add("Все");

                foreach (string item in PrescribingList)
                {
                    PrescribingPriceMedsComboBox.Items.Add(flutext(item));
                }

                PrescribingPriceMedsComboBox.SelectedItem = "Все";
                SearchPriceMedsTextBox.Focus();
            }
            catch (SQLiteException ex)
            {
                Message(4);
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        //Параметры создаваемой карточки лекарства в аптечке
        private void PricePanel(string id, string MedName, string MinYearOnePrice, string MidYearOnePrice, string MaxYearOnePrice,
            string Min3MCount, string Min3MOnePrice, string Min3MPrice, string Pharmacy, string Address, Bitmap picture)
        {
            Label lMedName = new Label();
            Label lMinYearOnePrice = new Label();
            Label lMidYearOnePrice = new Label();
            Label lMaxYearOnePrice = new Label();
            Label lMin3MCount = new Label();
            Label lMin3MOnePrice = new Label();
            Label lMin3MPrice = new Label();
            Label lPharmacy = new Label();
            Label lAddress = new Label();
            Button button1 = new Button();
            PictureBox pb = new PictureBox();
            Panel panel = new Panel();

            //Название лекарства на карточке
            lMedName.Location = new Point(150, 0);
            lMedName.BackColor = Color.Transparent;
            if (MedName.Length >= 22)
            {
                lMedName.Text = flutext(MedName.Remove(22)) + "...";
            }
            else
            {
                lMedName.Text = flutext(MedName);
            }
            lMedName.Size = new Size(300, 50);
            lMedName.Font = new Font("Calibri", 14.0F, FontStyle.Bold);
            lMedName.TextAlign = ContentAlignment.MiddleCenter;
            lMedName.Name = "qwe";

            //Мин цена штуки за год
            lMinYearOnePrice.Location = new Point(0, 175);
            lMinYearOnePrice.BackColor = Color.Transparent;
            lMinYearOnePrice.Text = "Мин: " + MinYearOnePrice + " руб/шт";
            //lMinYearOnePrice.Text = "Мин.руб./шт.: " + MinYearOnePrice;
            lMinYearOnePrice.Size = new Size(150, 25);
            lMinYearOnePrice.Font = new Font("Calibri", 12.0F);
            lMinYearOnePrice.TextAlign = ContentAlignment.MiddleCenter;

            //Сред цена штуки за год
            lMidYearOnePrice.Location = new Point(150, 175);
            lMidYearOnePrice.BackColor = Color.Transparent;
            lMidYearOnePrice.Text = "Сред: " + MidYearOnePrice + " руб/шт";
            //lMidYearOnePrice.Text = "Сред.руб./шт.: " + MidYearOnePrice;
            lMidYearOnePrice.Size = new Size(150, 25);
            lMidYearOnePrice.Font = new Font("Calibri", 12.0F);
            lMidYearOnePrice.TextAlign = ContentAlignment.MiddleCenter;

            //Макс цена штуки за год
            lMaxYearOnePrice.Location = new Point(300, 175);
            lMaxYearOnePrice.BackColor = Color.Transparent;
            lMaxYearOnePrice.Text = "Макс: " + MaxYearOnePrice + " руб/шт";
            //lMaxYearOnePrice.Text = "Макс.руб./шт.: " + MaxYearOnePrice;
            lMaxYearOnePrice.Size = new Size(150, 25);
            lMaxYearOnePrice.Font = new Font("Calibri", 12.0F);
            lMaxYearOnePrice.TextAlign = ContentAlignment.MiddleCenter;

            //Количество выгодной покупки за последние 3 месяца
            lMin3MCount.Location = new Point(0, 150);
            lMin3MCount.BackColor = Color.Transparent;
            lMin3MCount.Text = "Кол: " + Min3MCount + " шт";
            lMin3MCount.Size = new Size(150, 25);
            lMin3MCount.Font = new Font("Calibri", 12.0F);
            lMin3MCount.TextAlign = ContentAlignment.MiddleCenter;

            //Цена штуки выгодной покупки за последние 3 месяца
            lMin3MOnePrice.Location = new Point(150, 150);
            lMin3MOnePrice.BackColor = Color.Transparent;
            lMin3MOnePrice.Text = "Цена: " + Min3MOnePrice + " руб/шт";
            //lMin3MOnePrice.Text = "Мин.руб./шт.: " + Min3MOnePrice;
            lMin3MOnePrice.Size = new Size(150, 25);
            lMin3MOnePrice.Font = new Font("Calibri", 12.0F);
            lMin3MOnePrice.TextAlign = ContentAlignment.MiddleCenter;

            //Стоимость упаковки выгодной покупки за последние 3 месяца
            lMin3MPrice.Location = new Point(300, 150);
            lMin3MPrice.BackColor = Color.Transparent;
            lMin3MPrice.Text = "Стоим: " + Min3MPrice + " руб";
            //lMin3MPrice.Text = "Мин., руб.: " + Min3MPrice;
            lMin3MPrice.Size = new Size(150, 25);
            lMin3MPrice.Font = new Font("Calibri", 12.0F);
            lMin3MPrice.TextAlign = ContentAlignment.MiddleCenter;

            //Аптека
            lPharmacy.Location = new Point(150, 50);
            lPharmacy.BackColor = Color.Transparent;
            if (String.IsNullOrEmpty(Pharmacy))
            {
                lPharmacy.Text = "Аптека не занесена";
            }
            else
            {
                lPharmacy.Text = "Аптека: " + flutext(Pharmacy);
            }
            lPharmacy.Size = new Size(300, 50);
            lPharmacy.Font = new Font("Calibri", 12.0F);
            lPharmacy.TextAlign = ContentAlignment.MiddleCenter;

            //Адрес аптеки
            lAddress.Location = new Point(150, 100);
            lAddress.BackColor = Color.Transparent;
            if (String.IsNullOrEmpty(Address))
            {
                lAddress.Text = "Адрес не занесен";
            }
            else
            {
                lAddress.Text = "Адрес: " + flutext(Address);
            }
            lAddress.Size = new Size(300, 50);
            lAddress.Font = new Font("Calibri", 12.0F);
            lAddress.TextAlign = ContentAlignment.MiddleCenter;

            //Картинка карточки
            pb.Image = new Bitmap(picture);
            pb.Location = new Point(0, 0);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Height = 150;
            pb.Width = 150;
            pb.Image.Tag = MedName;
            //pb.Click += new EventHandler(MedPictureBox_Click);

            //Кнопка карточки
            button1.Location = new Point(0, 200);
            button1.BackColor = Color.Transparent;
            button1.Size = new Size(450, 30);
            button1.Font = new Font("Calibri", 14.0F, FontStyle.Bold);
            button1.Text = "Добавить в список покупок";
            button1.Click += new EventHandler(AddToBuyList_Click);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Tag = id;

            //Панель карточки
            panel.Location = new Point(0, 0);
            panel.Height = 230;
            panel.Width = 450;


            //Название, количество и картинка на панель карточки и добавить на панель карточек
            this.Controls.Add(panel);
            panel.Controls.Add(lMedName);
            panel.Controls.Add(lMinYearOnePrice);
            panel.Controls.Add(lMidYearOnePrice);
            panel.Controls.Add(lMaxYearOnePrice);
            panel.Controls.Add(lMin3MCount);
            panel.Controls.Add(lMin3MOnePrice);
            panel.Controls.Add(lMin3MPrice);
            panel.Controls.Add(lPharmacy);
            panel.Controls.Add(lAddress);
            panel.Controls.Add(button1);
            panel.Controls.Add(pb);
            PriceMedsFlowLayoutPanel.Controls.Add(panel);
        }

        //Зполнить панель карточками лекарств в аптечке
        private void FillPriceMedsFlowLayoutPanel(string fi)
        {
            while (PriceMedsFlowLayoutPanel.Controls.Count > 0)
            {
                foreach (Control panel in PriceMedsFlowLayoutPanel.Controls)
                {
                    PriceMedsFlowLayoutPanel.Controls.Remove(panel);
                }
            }

            string ipresc = null;

            if (fi != "0")
            {
                ipresc = " Where BasicMeds.Prescribing = '" + flltext(this.PrescribingPriceMedsComboBox.GetItemText(this.PrescribingPriceMedsComboBox.Items[Convert.ToInt32(fi)]).ToString()) + "'";
            }

            int day = 86400;
            string sName = null;
            DateTime year = DateTime.SpecifyKind(DateTime.Today.AddDays(-30), DateTimeKind.Utc);
            DateTimeOffset DTOyear = year;
            int Tmonths = Convert.ToInt32(DTOyear.ToUnixTimeSeconds()) / day * day;
            string sId = null;
            string sCount = null;
            string sMinoneprice = null;
            string sPrice = null;
            string sPharmacy = null;
            string sAddress = null;
            string sMinprice = null;
            string sAvgprice = null;
            string sMaxprice = null;

            command.CommandText = "SELECT BuyHistory.Name, BasicMeds.Prescribing from BuyHistory INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name" + ipresc + " GROUP BY BuyHistory.Name";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                sName = dt.Rows[i][dt.Columns[0]].ToString();
                //MessageBox.Show(sName);
                string pic = ImagesPath + sName + ".jpg";

                command.CommandText = "SELECT id, Count, ROUND(min(Price/Count),2) as MinOnePrice, Price, Pharmacy, Address from BuyHistory " +
                    "Where Name = '" + sName + "' And BuyDate >= " + Tmonths + " GROUP BY Name";
                command.ExecuteNonQuery();
                DataTable dt2 = new DataTable();
                SQLiteDataAdapter da2 = new SQLiteDataAdapter(command);
                da2.Fill(dt2);
                
                //MessageBox.Show(Tmonths.ToString() + " - " + dt2.Rows.Count.ToString());
                //MessageBox.Show(sName + " - " + dt2.Rows[i][dt.Columns[0]].ToString() + " - " + dt2.Rows[i][dt.Columns[1]].ToString() + " - " + dt2.Rows[i][dt.Columns[2]].ToString() + " - " +
                //    dt2.Rows[i][dt.Columns[3]].ToString() + " - " + dt2.Rows[i][dt.Columns[4]].ToString() + " - " + dt2.Rows[i][dt.Columns[5]].ToString());
                
                if (dt2.Rows.Count == 0)
                {
                    command.CommandText = "SELECT id, Count, ROUND(BuyHistory.Price/BuyHistory.Count,2) as MinOnePrice, Price, Pharmacy, Address from BuyHistory " +
                        "Where Name = '" + sName + "' ORDER BY BuyDate DESC LIMIT 1";
                    command.ExecuteNonQuery();
                    DataTable dt4 = new DataTable();
                    SQLiteDataAdapter da4 = new SQLiteDataAdapter(command);
                    da4.Fill(dt4);

                    sId = dt4.Rows[0][dt4.Columns[0]].ToString();
                    sCount = dt4.Rows[0][dt4.Columns[1]].ToString();
                    sMinoneprice = dt4.Rows[0][dt4.Columns[2]].ToString();
                    sPrice = dt4.Rows[0][dt4.Columns[3]].ToString();
                    sPharmacy = dt4.Rows[0][dt4.Columns[4]].ToString();
                    sAddress = dt4.Rows[0][dt4.Columns[5]].ToString();
                }
                else
                {
                    sId = dt2.Rows[0][dt2.Columns[0]].ToString();
                    sCount = dt2.Rows[0][dt2.Columns[1]].ToString();
                    sMinoneprice = dt2.Rows[0][dt2.Columns[2]].ToString();
                    sPrice = dt2.Rows[0][dt2.Columns[3]].ToString();
                    sPharmacy = dt2.Rows[0][dt2.Columns[4]].ToString();
                    sAddress = dt2.Rows[0][dt2.Columns[5]].ToString();
                }

                command.CommandText = "SELECT ROUND(min(Price/Count),2) as MinPrice, ROUND(avg(Price/Count),2) as AvgPrice, ROUND(max(Price/Count),2) as MaxPrice from BuyHistory " +
                    "Where Name = '" + sName + "'";
                command.ExecuteNonQuery();
                DataTable dt3 = new DataTable();
                SQLiteDataAdapter da3 = new SQLiteDataAdapter(command);
                da3.Fill(dt3);

                sMinprice = dt3.Rows[0][dt3.Columns[0]].ToString();
                sAvgprice = dt3.Rows[0][dt3.Columns[1]].ToString();
                sMaxprice = dt3.Rows[0][dt3.Columns[2]].ToString();

                if (File.Exists(pic))
                {
                    Image img = Image.FromFile(pic, true);
                    Bitmap picture = new Bitmap(img);
                    PricePanel(sId, sName, sMinprice, sAvgprice, sMaxprice, sCount, sMinoneprice, sPrice, sPharmacy, sAddress, picture);
                }
                else
                {
                    Bitmap picture = new Bitmap(Properties.Resources.DefaultMedPic);
                    PricePanel(sId, sName, sMinprice, sAvgprice, sMaxprice, sCount, sMinoneprice, sPrice, sPharmacy, sAddress, picture);
                }
            }

            /*string ipresc = null;

            if (fi != "0")
            {
                ipresc = " And BasicMeds.Prescribing = '" + flltext(this.PrescribingPriceMedsComboBox.GetItemText(this.PrescribingPriceMedsComboBox.Items[Convert.ToInt32(fi)]).ToString()) + "'";
            }

            //Задание периода год
            int day = 86400;

            DateTime today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
            DateTimeOffset DTOtoday = today;
            int istart = Convert.ToInt32(DTOtoday.ToUnixTimeSeconds()) / day * day + day - 1;

            DateTime year = DateTime.SpecifyKind(DateTime.Today.AddDays(-365), DateTimeKind.Utc);
            DateTimeOffset DTOyear = year;
            int iend = Convert.ToInt32(DTOyear.ToUnixTimeSeconds()) / day * day;

            //Переменные
            string id = "";
            string name = "";
            string MinPrice = "";
            string AvgPrice = "";
            string MaxPrice = "";
            string pic = "";
            string Min3MCount = "";
            string Min3MOnePrice = "";
            string Min3MPrice = "";
            string Pharmacy = "";
            string Address = "";
            Bitmap picture = new Bitmap(Properties.Resources.DefaultMedPic);

            command.CommandText = "SELECT BuyHistory.Name, ROUND(min(BuyHistory.Price/BuyHistory.Count),2) as MinPrice, " +
                "ROUND(avg(BuyHistory.Price/BuyHistory.Count),2) as AvgPrice, ROUND(max(BuyHistory.Price/BuyHistory.Count),2) as MaxPrice " +
                "from BuyHistory INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name Where BuyHistory.BuyDate >= " + istart + " And " +
                "BuyHistory.BuyDate <= " + iend + "" + ipresc + " GROUP BY BuyHistory.Name";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                name = flutext(dt.Rows[i][dt.Columns[0]].ToString());
                MinPrice = dt.Rows[i][dt.Columns[1]].ToString();
                AvgPrice = dt.Rows[i][dt.Columns[2]].ToString();
                MaxPrice = dt.Rows[i][dt.Columns[3]].ToString();
                pic = ImagesPath + name + ".jpg";

                year = DateTime.SpecifyKind(DateTime.Today.AddDays(-30), DateTimeKind.Utc);
                DTOyear = year;
                iend = Convert.ToInt32(DTOyear.ToUnixTimeSeconds()) / day * day;

                command.CommandText = "SELECT BuyHistory.id, BuyHistory.Name, BuyHistory.Count as MinCount, " +
                    "ROUND(min(BuyHistory.Price/BuyHistory.Count),2) as MinOnePrice, BuyHistory.Price as MinPrice, BuyHistory.Pharmacy, " +
                    "BuyHistory.Address from BuyHistory INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name Where BuyHistory.BuyDate >= " + istart + " And " +
                    "BuyHistory.BuyDate <= " + iend + "" + ipresc + " GROUP BY BuyHistory.Name ORDER BY BuyHistory.Name";
                command.ExecuteNonQuery();
                DataTable dt2 = new DataTable();
                SQLiteDataAdapter da2 = new SQLiteDataAdapter(command);
                da2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        id = dt2.Rows[j][dt.Columns[0]].ToString();
                        Min3MCount = dt2.Rows[j][dt.Columns[2]].ToString();
                        Min3MOnePrice = dt2.Rows[j][dt.Columns[3]].ToString();
                        Min3MPrice = dt2.Rows[j][dt.Columns[4]].ToString();
                        Pharmacy = flutext(dt2.Rows[j][dt.Columns[5]].ToString());
                        Address = flutext(dt2.Rows[j][dt.Columns[6]].ToString());
                    }
                }
                else
                {
                    command.CommandText = "SELECT BuyHistory.id, BuyHistory.Name, BuyHistory.Count as MinCount, " +
                        "ROUND((BuyHistory.Price/BuyHistory.Count),2) as MinOnePrice, BuyHistory.Price as MinPrice, BuyHistory.Pharmacy, BuyHistory.Address, " +
                        "max(BuyHistory.BuyDate) as BDate from BuyHistory INNER JOIN BasicMeds ON BuyHistory.Name = BasicMeds.Name Where " +
                        "BuyHistory.BuyDate <= " + iend + "" + ipresc + " GROUP BY BuyHistory.Name ORDER BY BuyHistory.Name";
                    command.ExecuteNonQuery();
                    dt2 = new DataTable();
                    da2 = new SQLiteDataAdapter(command);
                    da2.Fill(dt2);

                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        id = dt2.Rows[0][dt.Columns[0]].ToString();
                        Min3MCount = dt2.Rows[0][dt.Columns[2]].ToString();
                        Min3MOnePrice = dt2.Rows[0][dt.Columns[3]].ToString();
                        Min3MPrice = dt2.Rows[0][dt.Columns[4]].ToString();
                        Pharmacy = flutext(dt2.Rows[0][dt.Columns[5]].ToString());
                        Address = flutext(dt2.Rows[0][dt.Columns[6]].ToString());
                    }
                    
                }

                if (File.Exists(pic))
                {
                    Image img = Image.FromFile(pic, true);
                    picture = new Bitmap(img);
                }
                else
                {
                    picture = new Bitmap(Properties.Resources.DefaultMedPic);
                }
            }

            PricePanel(id, name, MinPrice, AvgPrice, MaxPrice, Min3MCount, Min3MOnePrice, Min3MPrice, Pharmacy, Address, picture);*/
        }

        //Списание лекарства из аптечке из-за срока годности
        private void AddToBuyList_Click(object sender, EventArgs e)
        {
            Button button1 = sender as Button;
            string id = button1.Tag.ToString();

            DialogResult dr;
            dr = MessageBox.Show("Добавить?", "Добавление лекарства в список покупок", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                try
                {
                    command.CommandText = "SELECT Name, Count, ROUND(BuyHistory.Price/BuyHistory.Count,2) as MinOnePrice, Price, Pharmacy, Address from BuyHistory " +
                        "Where Id = '" + id + "' ORDER BY BuyDate DESC LIMIT 1";
                    command.ExecuteNonQuery();
                    dt = new DataTable();
                    buylistdt = new DataTable();
                    da = new SQLiteDataAdapter(command);
                    da.Fill(dt);
                    
                    //string[] row = new string[] { "1", "Product 1", "1000" };
                    foreach (DataRow dr2 in dt.Rows)
                    {
                        string c5;
                        string c6;
                        if (String.IsNullOrEmpty(dt.Rows[0][dt.Columns[4]].ToString()))
                        {
                            c5 = "Не заполнено";
                        }
                        else
                        {
                            c5 = flutext(dt.Rows[0][dt.Columns[4]].ToString());
                        }

                        if (String.IsNullOrEmpty(dt.Rows[0][dt.Columns[5]].ToString()))
                        {
                            c6 = "Не заполнено";
                        }
                        else
                        {
                            c6 = flutext(dt.Rows[0][dt.Columns[5]].ToString());
                        }

                        string[] row = {
                            flutext(dt.Rows[0][dt.Columns[0]].ToString()),
                            dt.Rows[0][dt.Columns[1]].ToString(),
                            dt.Rows[0][dt.Columns[2]].ToString(),
                            dt.Rows[0][dt.Columns[3]].ToString(),
                            c5,
                            c6,
                        };
                        BuyListDataGridView.Rows.Add(row); //dr2.ItemArray

                    }
                    //BuyListDataGridView.Rows.Add(dt.);
                    //row = new string[] { "2", "Product 2", "2000" };
                    //BuyListDataGridView.Rows.Add(row);

                    /*
                    string woffmed = flltext(dt.Rows[0][dt.Columns[0]].ToString());
                    int cnt = Convert.ToInt32(dt.Rows[0][dt.Columns[1]].ToString());

                    command.CommandText = "select Prescribing from BasicMeds where Name = '" + woffmed + "'";
                    command.ExecuteNonQuery();
                    dt = new DataTable();
                    da = new SQLiteDataAdapter(command);
                    da.Fill(dt);

                    string prescr = flltext(dt.Rows[0][dt.Columns[0]].ToString());

                    int todayint = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    command.CommandText = "INSERT INTO UseHistory (User, MedName, Prescribing, Count, UseDate)" +
                        "VALUES ('списано', '" + woffmed + "', '" + prescr + "', '" + cnt + "', '" + todayint + "')";
                    command.ExecuteNonQuery();

                    command.CommandText = "delete from MedKit where Id = '" + id + "'";
                    command.ExecuteNonQuery();

                    Message(24);

                    CheckBestBefore();
                    FillBBPanel();*/
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Ошибка удаления лекарства: " + ex.Message);
                    return;
                }
            }
        }

        //Передать изменения о новом фильтре и/или сортировке
        private void FinanceFilter()
        {
            SearchPriceMedsTextBox.Clear();
            string fi = this.PrescribingPriceMedsComboBox.GetItemText(this.PrescribingPriceMedsComboBox.SelectedIndex);
            FillPriceMedsFlowLayoutPanel(fi);
        }

        private void PrescribingPriceMedsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FinanceFilter();
        }

        private void ClearPriceMedsButton_Click(object sender, EventArgs e)
        {
            PriceMedsToolStripMenuItem.PerformClick();
        }

        private void SearchPriceMedsTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchtext = flltext(SearchPriceMedsTextBox.Text);

            foreach (Control panel in PriceMedsFlowLayoutPanel.Controls)
            {
                ((Control)panel).Hide();
                foreach (Control name in panel.Controls)
                {
                    if (name == panel.Controls.Find("qwe", true)[0] && flltext(name.Text).Contains(searchtext))
                    {
                        ((Control)panel).Show();
                    }
                }
            }
        }

        //private PrintDocument printDocument1 = new PrintDocument();

        private void PrintButton_Click(object sender, EventArgs e)
        {
            printDocument1.Print();
            //ClsPrint _ClsPrint = new ClsPrint(BuyListDataGridView, "Список покупок");
            //_ClsPrint.PrintForm();

            /*PrintDocument recordDoc;
            // Create the document and name it
            recordDoc = new PrintDocument();
            //recordDoc.DocumentName = "Customer Receipt";
            recordDoc.PrintPage += new PrintPageEventHandler(this.PrintReceiptPage);
            PrintDialog printDialog = new PrintDialog();
            printDialog.Document = recordDoc;
            if (printDialog.ShowDialog() == DialogResult.OK) printDialog.Document.Print();
            //recordDoc.Print();*/
            // Preview document
            //dlgPreview.Document = recordDoc;
            //dlgPreview.ShowDialog();
            // Dispose of document when done printing
            //recordDoc.Dispose();
        }

        private void PrintReceiptPage(object sender, PrintPageEventArgs e)

        {
            //Замените на e.Graphics.DrawImage или любую другую логику
            //e.Graphics.DrawString("Привет", new Font("Arial", 14), Brushes.Black, 0, 0);
            /*
            string message = null;
            int y;
            // Print receipt
            Font myFont = new Font("Times New Roman", 15, FontStyle.Bold);
            y = e.MarginBounds.Y;
            e.Graphics.DrawString(message, myFont, Brushes.DarkRed, e.MarginBounds.X, y);
            */
        }

        private void BackToFinanceButton_Click(object sender, EventArgs e)
        {
            PriceMedsToolStripMenuItem.PerformClick();
        }

        private void BuyListButton_Click(object sender, EventArgs e)
        {
            HidePanels();
            PanelON(BuyListPanel);
            /*command.CommandText = "select * from BuyHistory ORDER BY BuyDate DESC";
            command.ExecuteNonQuery();
            dt = new DataTable();
            da = new SQLiteDataAdapter(command);
            da.Fill(dt);*/
            //BuyListDataGridView.Rows.Clear();
            
            //BuyListDataGridView.DataSource = buylistdt;
            BuyListDataGridView.Refresh();
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            Bitmap bm = new Bitmap(this.BuyListDataGridView.Width, this.BuyListDataGridView.Height);

            BuyListDataGridView.DrawToBitmap(bm, new Rectangle(0, 0, this.BuyListDataGridView.Width, this.BuyListDataGridView.Height));
            e.Graphics.DrawImage(bm, 0, 0);
        }

        private void ClearBuyListButton_Click(object sender, EventArgs e)
        {
            BuyListDataGridView.Rows.Clear();
            BuyListDataGridView.Refresh();
        }
    }

    //Класс для печати датагрид
    /*class ClsPrint
    {
        #region Variables

        int iCellHeight = 0; //Used to get/set the datagridview cell height
        int iTotalWidth = 0; //
        int iRow = 0;//Used as counter
        bool bFirstPage = false; //Used to check whether we are printing first page
        bool bNewPage = false;// Used to check whether we are printing a new page
        int iHeaderHeight = 0; //Used for the header height
        StringFormat strFormat; //Used to format the grid rows.
        ArrayList arrColumnLefts = new ArrayList();//Used to save left coordinates of columns
        ArrayList arrColumnWidths = new ArrayList();//Used to save column widths
        private PrintDocument _printDocument = new PrintDocument();
        private DataGridView gw = new DataGridView();
        private string _ReportHeader;

        #endregion

        public ClsPrint(DataGridView gridview, string ReportHeader)
        {
            _printDocument.PrintPage += new PrintPageEventHandler(_printDocument_PrintPage);
            _printDocument.BeginPrint += new PrintEventHandler(_printDocument_BeginPrint);
            gw = gridview;
            _ReportHeader = ReportHeader;
        }

        public void PrintForm()
        {
            ////Open the print dialog
            //PrintDialog printDialog = new PrintDialog();
            //printDialog.Document = _printDocument;
            //printDialog.UseEXDialog = true;

            ////Get the document
            //if (DialogResult.OK == printDialog.ShowDialog())
            //{
            //    _printDocument.DocumentName = "Test Page Print";
            //    _printDocument.Print();
            //}

            //Open the print preview dialog
            PrintPreviewDialog objPPdialog = new PrintPreviewDialog();
            objPPdialog.Document = _printDocument;
            objPPdialog.ShowDialog();
        }

        
        private void _printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            //try
            //{
            //Set the left margin
            int iLeftMargin = e.MarginBounds.Left;
            //Set the top margin
            int iTopMargin = e.MarginBounds.Top;
            //Whether more pages have to print or not
            bool bMorePagesToPrint = false;
            int iTmpWidth = 0;

            //For the first page to print set the cell width and header height
            if (bFirstPage)
            {
                foreach (DataGridViewColumn GridCol in gw.Columns)
                {
                    iTmpWidth = (int)(Math.Floor((double)((double)GridCol.Width /
                        (double)iTotalWidth * (double)iTotalWidth *
                        ((double)e.MarginBounds.Width / (double)iTotalWidth))));

                    iHeaderHeight = (int)(e.Graphics.MeasureString(GridCol.HeaderText,
                        GridCol.InheritedStyle.Font, iTmpWidth).Height) + 11;

                    // Save width and height of headers
                    arrColumnLefts.Add(iLeftMargin);
                    arrColumnWidths.Add(iTmpWidth);
                    iLeftMargin += iTmpWidth;
                }
            }
            //Loop till all the grid rows not get printed
            while (iRow <= gw.Rows.Count - 1)
            {
                DataGridViewRow GridRow = gw.Rows[iRow];
                //Set the cell height
                iCellHeight = GridRow.Height + 5;
                int iCount = 0;
                //Check whether the current page settings allows more rows to print
                if (iTopMargin + iCellHeight >= e.MarginBounds.Height + e.MarginBounds.Top)
                {
                    bNewPage = true;
                    bFirstPage = false;
                    bMorePagesToPrint = true;
                    break;
                }
                else
                {

                    if (bNewPage)
                    {
                        //Draw Header
                        e.Graphics.DrawString(_ReportHeader,
                            new Font(gw.Font, FontStyle.Bold),
                            Brushes.Black, e.MarginBounds.Left,
                            e.MarginBounds.Top - e.Graphics.MeasureString(_ReportHeader,
                            new Font(gw.Font, FontStyle.Bold),
                            e.MarginBounds.Width).Height - 13);

                        String strDate = "";
                        //Draw Date
                        e.Graphics.DrawString(strDate,
                            new Font(gw.Font, FontStyle.Bold), Brushes.Black,
                            e.MarginBounds.Left +
                            (e.MarginBounds.Width - e.Graphics.MeasureString(strDate,
                            new Font(gw.Font, FontStyle.Bold),
                            e.MarginBounds.Width).Width),
                            e.MarginBounds.Top - e.Graphics.MeasureString(_ReportHeader,
                            new Font(new Font(gw.Font, FontStyle.Bold),
                            FontStyle.Bold), e.MarginBounds.Width).Height - 13);

                        //Draw Columns                 
                        iTopMargin = e.MarginBounds.Top;
                        DataGridViewColumn[] _GridCol = new DataGridViewColumn[gw.Columns.Count];
                        int colcount = 0;
                        //Convert ltr to rtl
                        foreach (DataGridViewColumn GridCol in gw.Columns)
                        {
                            _GridCol[colcount++] = GridCol;
                        }
                        for (int i = (_GridCol.Count() - 1); i >= 0; i--)
                        {
                            e.Graphics.FillRectangle(new SolidBrush(Color.LightGray),
                                new Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                (int)arrColumnWidths[iCount], iHeaderHeight));

                            e.Graphics.DrawRectangle(Pens.Black,
                                new Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                (int)arrColumnWidths[iCount], iHeaderHeight));

                            e.Graphics.DrawString(_GridCol[i].HeaderText,
                                _GridCol[i].InheritedStyle.Font,
                                new SolidBrush(_GridCol[i].InheritedStyle.ForeColor),
                                new RectangleF((int)arrColumnLefts[iCount], iTopMargin,
                                (int)arrColumnWidths[iCount], iHeaderHeight), strFormat);
                            iCount++;
                        }
                        bNewPage = false;
                        iTopMargin += iHeaderHeight;
                    }
                    iCount = 0;
                    DataGridViewCell[] _GridCell = new DataGridViewCell[GridRow.Cells.Count];
                    int cellcount = 0;
                    //Convert ltr to rtl
                    foreach (DataGridViewCell Cel in GridRow.Cells)
                    {
                        _GridCell[cellcount++] = Cel;
                    }
                    //Draw Columns Contents                
                    for (int i = (_GridCell.Count() - 1); i >= 0; i--)
                    {
                        if (_GridCell[i].Value != null)
                        {
                            e.Graphics.DrawString(_GridCell[i].FormattedValue.ToString(),
                                _GridCell[i].InheritedStyle.Font,
                                new SolidBrush(_GridCell[i].InheritedStyle.ForeColor),
                                new RectangleF((int)arrColumnLefts[iCount],
                                (float)iTopMargin,
                                (int)arrColumnWidths[iCount], (float)iCellHeight),
                                strFormat);
                        }
                        //Drawing Cells Borders 
                        e.Graphics.DrawRectangle(Pens.Black,
                            new Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                            (int)arrColumnWidths[iCount], iCellHeight));
                        iCount++;
                    }
                }
                iRow++;
                iTopMargin += iCellHeight;
            }
            //If more lines exist, print another page.
            if (bMorePagesToPrint)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;
            //}
            //catch (Exception exc)
            //{
            //    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK,
            //       MessageBoxIcon.Error);
            //}
        }

        private void _printDocument_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            try
            {
                strFormat = new StringFormat();
                strFormat.Alignment = StringAlignment.Center;
                strFormat.LineAlignment = StringAlignment.Center;
                strFormat.Trimming = StringTrimming.EllipsisCharacter;

                arrColumnLefts.Clear();
                arrColumnWidths.Clear();
                iCellHeight = 0;
                iRow = 0;
                bFirstPage = true;
                bNewPage = true;

                // Calculating Total Widths
                iTotalWidth = 0;
                foreach (DataGridViewColumn dgvGridCol in gw.Columns)
                {
                    iTotalWidth += dgvGridCol.Width;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
    */
}