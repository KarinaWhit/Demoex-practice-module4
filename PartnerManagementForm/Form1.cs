using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace PartnerManagementForm
{
    // Главная форма
    public partial class Form1 : Form
    {
        // Стиль согласно руководству
        private readonly Color PrimaryBackgroundColor = Color.White;
        private readonly Color SecondaryBackgroundColor = Color.FromArgb(244, 232, 211); // #F4E8D3
        private readonly Color AccentColor = Color.FromArgb(103, 186, 128); // 67BA80
        private new readonly Font DefaultFont = new Font("Segoe UI", 9);
        private readonly Font HeaderFont = new Font("Segoe UI", 10, FontStyle.Bold);

        // Элементы интерфейса
        private Panel partnersPanel;
        private PictureBox logoPictureBox;
        private Button addButton, calcMaterialButton;

        // Подключение к базе данных
        private const string ConnectionString = "Host = localhost; Port = 5432; Database = demoex_practice; Username = postgres; Password = 123098";

        public Form1()
        {
            InitializeUIComponents();
            ConfigureFormAppearance();
            LoadPartnerData();
        }

        // Инициализация компонентов интерфейса
        private void InitializeUIComponents()
        {
            // Настройка логотипа
            logoPictureBox = new PictureBox
            {
                Image = Image.FromFile("D:/СПБКТ/Демоэкзамен практика/PartnerManagementForm/Resources/logo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(250, 80),
                Location = new Point(30, 20)
            };

            // Кнопка добавления
            addButton = new Button
            {
                Text = "Добавить партнёра",
                Location = new Point(30, 110),
                Size = new Size(150, 30),
                BackColor = AccentColor,
                ForeColor = Color.White,
                Font = DefaultFont
            };
            addButton.Click += AddButton_Click;

            // Кнопка расчёта материалов
            calcMaterialButton = new Button
            {
                Text = "Расчёт материалов",
                Location = new Point(200, 110),
                Size = new Size(150, 30),
                BackColor = AccentColor,
                ForeColor = Color.White,
                Font = DefaultFont
            };
            calcMaterialButton.Click += CalcMaterialButton_Click;

            // Настройка панели партнёров
            partnersPanel = new Panel
            {
                BackColor = SecondaryBackgroundColor,
                AutoScroll = true,
                Location = new Point(30, 150),
                Size = new Size(840, 470),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Добавление элементов на форму
            this.Controls.Add(logoPictureBox);
            this.Controls.Add(addButton);
            this.Controls.Add(calcMaterialButton);
            this.Controls.Add(partnersPanel);
        }

        // Настройка внешнего вида формы согласно руководству
        private void ConfigureFormAppearance()
        {
            this.Text = "Система управления партнёрами компании";
            this.Size = new Size(900, 700);
            this.BackColor = PrimaryBackgroundColor;
            this.Icon = new Icon("D:/СПБКТ/Демоэкзамен практика/PartnerManagementForm/Resources/app_icon.ico");
            this.Font = DefaultFont;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        // Загрузка данных о партнёрах из базы данных
        private void LoadPartnerData()
        {
            try
            {
                List<Partner> partners = RetrievePartnersFromDatabase();
                DisplayPartners(partners);
            }
            catch (NpgsqlException ex)
            {
                ShowErrorMessage($"Ошибка подключения к базе данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Неожиданная ошибка: {ex.Message}");
            }
        }

        // Получение списка партнёров из PostgreSQL
        private List<Partner> RetrievePartnersFromDatabase()
        {
            var partners = new List<Partner>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // SQL-запрос для получения данных о партнёрах и их продажах
                string query = @"SELECT p.id_partner, pt.partner_type, p.partner_name, p.director, p.phone, p.rating,
                    COALESCE((SELECT SUM(w.quantity * pr.min_cost_for_partner) FROM warehouse w JOIN product pr ON 
                    w.product_id = pr.id_product WHERE w.partner_id = p.id_partner), 0) AS total_sales from
                    partner p JOIN partner_type pt ON p.type_id = pt.id_partner_type ORDER BY p.partner_name";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        partners.Add(new Partner
                        {
                            Id = reader.GetInt32(0),
                            Type = reader.GetString(1),
                            Name = reader.GetString(2),
                            Director = reader.GetString(3),
                            Phone = reader.GetString(4),
                            Rating = reader.GetString(5),
                            TotalSales = Convert.ToDecimal(reader.GetDouble(6))
                        });
                    }
                }
            }

            return partners;
        }

        // Отображение списка партнёров на форме
        private void DisplayPartners(List<Partner> partners)
        {
            partnersPanel.Controls.Clear();

            int verticalPosition = 20;
            foreach (var partner in partners)
            {
                Panel partnerCard = CreatePartnerCard(partner, verticalPosition);
                partnersPanel.Controls.Add(partnerCard);
                verticalPosition += partnerCard.Height + 15;
            }
        }

        // Создание карточки партнёра
        private Panel CreatePartnerCard(Partner partner, int topPosition)
        {
            var card = new Panel
            {
                BackColor = PrimaryBackgroundColor,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(790, 120),
                Location = new Point(20, topPosition),
                Tag = partner.Id
            };

            // Расчёт скидки для партнёра
            int discountPercentage = CalculateDiscountPercentage(partner.TotalSales);

            // Заголовки карточки (Тип | Наименование)
            var headerLabel = new Label
            {
                Text = $"{partner.Type} | {partner.Name}",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = HeaderFont,
                ForeColor = AccentColor
            };

            // Процент скидки
            var discountLabel = new Label
            {
                Text = $"{discountPercentage}%",
                Location = new Point(700, 15),
                AutoSize = true,
                Font = HeaderFont,
                ForeColor = AccentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Директор
            var directorLabel = new Label
            {
                Text = $"{partner.Director}",
                Location = new Point(15, 40),
                AutoSize = true,
                Font = DefaultFont
            };

            // Телефон
            var phoneLabel = new Label
            {
                Text = $"{partner.Phone}",
                Location = new Point(15, 65),
                AutoSize = true,
                Font = DefaultFont
            };

            // Рейтинг
            var ratingLabel = new Label
            {
                Text = $"Рейтинг: {partner.Rating}",
                Location = new Point(15, 90),
                AutoSize = true,
                Font = DefaultFont
            };

            // Редактирование
            var editButton = new Button
            {
                Text = "Редактировать",
                Location = new Point(650, 70),
                Size = new Size(120, 30),
                Tag = partner.Id,
                BackColor = AccentColor,
                ForeColor = Color.White
            };
            editButton.Click += EditButton_Click;

            // История продаж
            var historyButton = new Button
            {
                Text = "История продаж",
                Location = new Point(500, 70),
                Size = new Size(120, 30),
                Tag = partner.Id,
                BackColor = AccentColor,
                ForeColor= Color.White
            };
            historyButton.Click += HistoryButton_Click;

            // Добавление элементов на карточку
            card.Controls.Add(headerLabel);
            card.Controls.Add(discountLabel);
            card.Controls.Add(directorLabel);
            card.Controls.Add(phoneLabel);
            card.Controls.Add(ratingLabel);
            card.Controls.Add(editButton);
            card.Controls.Add(historyButton);

            return card;
        }

        // Добавление партнёра
        private void AddButton_Click(object sender, EventArgs e)
        {
            var editForm = new PartnerEditForm(null, ConnectionString);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadPartnerData();
            }
        }

        // Редактирование партнёра
        private void EditButton_Click(Object sender, EventArgs e)
        {
            var button = (Button)sender;
            int partnerId = (int)button.Tag;

            var editForm = new PartnerEditForm(partnerId, ConnectionString);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadPartnerData();
            }
        }

        // Вывод истории продаж
        private void HistoryButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            int partnerId = (int)button.Tag;
            var partnerCard = (Panel)button.Parent;
            string partnerName = string.Empty;

            // Получение имени партнёра из карточки
           foreach (Control control in partnerCard.Controls)
            {
                if (control is Label label && label.Text.Contains("|"))
                {
                    partnerName = label.Text.Split('|')[1].Trim(); break;
                }
            }

            var historyForm = new SalesHistoryForm(partnerId, partnerName, ConnectionString);
            historyForm.ShowDialog();
        }

        // Вывод расчёта материалов
        private void CalcMaterialButton_Click(object sender, EventArgs e)
        {
            var calcForm = new MaterialCalculationForm(ConnectionString);
            calcForm.ShowDialog();
        }

        // Подтверждение выхода из программы
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (MessageBox.Show("Закрыть приложение?", "Пожтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        // Засчёт процента скидки на основе объёма продаж
        private int CalculateDiscountPercentage(decimal totalSales)
        {
            if (totalSales > 300000m) return 15;
            if (totalSales > 50000m) return 10;
            if (totalSales > 10000m) return 5;
            return 0;
        }

        // Показать сообщение об ошибке
        private void ShowErrorMessage(String message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Форма редактирования партнёров
    public class PartnerEditForm : Form
    {
        private readonly int? _partnerId;
        private readonly string _connectionString;

        // Элементы формы
        private TextBox nameTextBox, ratingTextBox, directorTextBox, phoneTextBox, emailTextBox, addressTextBox, innTextBox;
        private ComboBox typeComboBox;
        private Button saveButton, cancelButton;

        public PartnerEditForm(int? partnerId, string connectionString)
        {
            _partnerId = partnerId;
            _connectionString = connectionString;

            InitializeComponents();
            LoadPartnerTypes();

            // Проверка наличия партнёра в базе данных
            if (_partnerId.HasValue)
            {
                Text = "Редактирование партнёра";
                LoadPartnerData();
            }
            else
            {
                Text = "Добавление нового партнёра";
            }
        }

        // Инициализация компонентов интерфейса
        private void InitializeComponents()
        {
            Size = new Size(500, 440);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // Название
            var nameLabel = new Label { Text = "Наименование: ", Location = new Point(20, 20) };
            nameTextBox = new TextBox { Location = new Point(150, 20), Size = new Size(300, 25) };

            // Тип
            var typeLabel = new Label { Text = "Тип: ", Location = new Point(20, 60) };
            typeComboBox = new ComboBox { Location = new Point(150, 60), Size = new Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            // Рейтинг
            var ratingLabel = new Label { Text = "Рейтинг: ", Location = new Point(20, 100) };
            ratingTextBox = new TextBox { Location = new Point(150, 100), Size = new Size(100, 25) };

            // Директор
            var directorLabel = new Label { Text = "ФИО: ", Location = new Point(20, 140) };
            directorTextBox = new TextBox { Location = new Point(150, 140), Size = new Size(300, 25) };

            // Телефон
            var phoneLabel = new Label { Text = "Телефон: ", Location = new Point(20, 180) };
            phoneTextBox = new TextBox { Location = new Point(150, 180), Size = new Size(300, 25) };

            // Email
            var emailLabel = new Label { Text = "Email: ", Location = new Point(20, 220) };
            emailTextBox = new TextBox { Location = new Point(150, 220), Size = new Size(300, 25) };

            // Адрес
            var addressLabel = new Label { Text = "Адрес: ", Location = new Point(20, 260) };
            addressTextBox = new TextBox { Location = new Point(150, 260), Size = new Size(300, 25) };

            // ИНН
            var innLabel = new Label { Text = "ИНН: ", Location = new Point(20, 300) };
            innTextBox = new TextBox { Location = new Point(150, 300), Size = new Size(100, 25) };

            // Кнопки
            saveButton = new Button { Text = "Сохранить", Location = new Point(150, 350), Size = new Size(100, 30) };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button { Text = "Отмена", Location = new Point(270, 350), Size = new Size(100, 30) };
            cancelButton.Click += (s,e) => DialogResult = DialogResult.Cancel;

            // Добавление элементов на форму
            Controls.AddRange(new Control[] {
                nameLabel, nameTextBox,
                typeLabel, typeComboBox,
                ratingLabel, ratingTextBox,
                directorLabel, directorTextBox,
                phoneLabel, phoneTextBox,
                emailLabel, emailTextBox,
                addressLabel, addressTextBox,
                innLabel, innTextBox,
                saveButton, cancelButton
            });
        }

        // Загрузка типов партнёров
        private void LoadPartnerTypes()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand("SELECT id_partner_type, partner_type FROM partner_type ORDER BY partner_type", connection);

                    // Загрузка значений из таблицы в список по виду "Ключ (id_partner_type) - Значение (partner_type)"
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            typeComboBox.Items.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                        }
                    }

                    typeComboBox.DisplayMember = "Value";
                    typeComboBox.ValueMember = "Key";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов партнёров: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка информации о партнёре
        private void LoadPartnerData()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand("SELECT partner_name, type_id, director, phone, email, address, inn, rating " + 
                        "FROM partner WHERE id_partner = @id", connection);

                    // Приравниваем значение id из таблицы к значению переменной _partnerId
                    command.Parameters.AddWithValue("@id", _partnerId.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            nameTextBox.Text = reader.GetString(0);
                            // Перенос значений из таблицы в ComboBox на основе отношений "Ключ - Зачение", описанных в коде выше
                            for (int i = 0; i < typeComboBox.Items.Count; i++)
                            {
                                var item = (KeyValuePair<int, string>)typeComboBox.Items[i];
                                if (item.Key == reader.GetInt32(1))
                                {
                                    typeComboBox.SelectedIndex = i; break;
                                }
                            }

                            directorTextBox.Text = reader.GetString(2);
                            phoneTextBox.Text = reader.GetString(3);
                            emailTextBox.Text = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            addressTextBox.Text = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            if (innTextBox != null)
                            {
                                innTextBox.Text = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            }
                            ratingTextBox.Text = reader.GetString(7);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных партнёра: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
            }
        }

        // Настройка кнопки сохранения
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (_partnerId.HasValue)
                            {
                                UpdatePartner(connection, transaction);
                                MessageBox.Show("Данные партнера обновлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                AddPartner(connection, transaction);
                                MessageBox.Show("Новый партнер успешно добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                            // Сохранение изменений в базе данных
                            transaction.Commit();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                                          "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка соединения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Проверка валидности входных данных
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Введите наименование партнера", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Установка курсора на nameTextBox
                nameTextBox.Focus();
                return false;
            }

            if (typeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип партнера", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                typeComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(directorTextBox.Text))
            {
                MessageBox.Show("Введите ФИО директора", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                directorTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(phoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                phoneTextBox.Focus();
                return false;
            }

            return true;
        }

        // Добавление партнёра
        private void AddPartner(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            try
            {
                // Получаем максимальный id_partner и увеличиваем на 1
                var getMaxIdCommand = new NpgsqlCommand(
                    "SELECT COALESCE(MAX(id_partner), 0) FROM partner",
                    connection, transaction);

                int maxId = Convert.ToInt32(getMaxIdCommand.ExecuteScalar());
                int newId = maxId + 1;

                // Вставляем нового партнера с новым id_partner
                var insertCommand = new NpgsqlCommand(
                    "INSERT INTO partner (id_partner, partner_name, type_id, director, phone, email, address, inn, rating) " +
                    "VALUES (@id_partner, @name, @type, @director, @phone, @email, @address, @inn, @rating)",
                    connection, transaction);

                // Добавляем параметры
                insertCommand.Parameters.AddWithValue("@id_partner", newId);
                FillCommandParameters(insertCommand);

                int rowsAffected = insertCommand.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception("Не удалось добавить нового партнера");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при добавлении партнера: {ex.Message}");
            }
        }

        // Настройка обновления данных партнёра
        private void UpdatePartner(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var command = new NpgsqlCommand(
                "UPDATE partner SET " +
                "partner_name = @name, type_id = @type, director = @director, " +
                "phone = @phone, email = @email, address = @address, " +
                "inn = @inn, rating = @rating " +
                "WHERE id_partner = @id",
                connection, transaction);

            FillCommandParameters(command);
            command.Parameters.AddWithValue("@id", _partnerId.Value);

            int affectedRows = command.ExecuteNonQuery();

            if (affectedRows == 0)
                throw new Exception("Не удалось обновить данные партнера. Возможно, запись была удалена.");
        }

        // Настройка параметров комманд
        private void FillCommandParameters(NpgsqlCommand command)
        {
            if (typeComboBox.SelectedItem == null)
                throw new Exception("Не выбран тип партнера");

            var typePair = (KeyValuePair<int, string>)typeComboBox.SelectedItem;

            // Проверка правильноти имеён параметров, соответствующих именам столбцов в базе данныхрамме
            command.Parameters.AddWithValue("@name", nameTextBox.Text.Trim());
            command.Parameters.AddWithValue("@type", typePair.Key);
            command.Parameters.AddWithValue("@director", directorTextBox.Text.Trim());
            command.Parameters.AddWithValue("@phone", phoneTextBox.Text.Trim());
            command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(emailTextBox.Text) ? DBNull.Value : (object)emailTextBox.Text.Trim());
            command.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(addressTextBox.Text) ? DBNull.Value : (object)addressTextBox.Text.Trim());
            command.Parameters.AddWithValue("@inn", string.IsNullOrWhiteSpace(innTextBox.Text) ? DBNull.Value : (object)innTextBox.Text.Trim());
            command.Parameters.AddWithValue("@rating", ratingTextBox.Text.Trim());
        }
    }

    // Класс для хранения данных о партнёре
    public class Partner
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Director { get; set; }
        public string Phone { get; set; }
        public string Rating { get; set; }
        public decimal TotalSales { get; set; }
    }
}
