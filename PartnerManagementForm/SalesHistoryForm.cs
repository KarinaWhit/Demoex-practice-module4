using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PartnerManagementForm
{
    // Форма истории продаж
    public class SalesHistoryForm : Form
    {
        private readonly int _partnerId;
        private readonly string _partnerName;
        private readonly string _connectionString;

        public SalesHistoryForm(int partnerId, string partnerName, string connectionString)
        {
            _partnerId = partnerId;
            _partnerName = partnerName;
            _connectionString = connectionString;

            InitializeComponents();
            LoadSalesData();
        }

        // Инициализация элементов формы
        private void InitializeComponents()
        {
            this.Text = $"История продаж партнёра: {_partnerName}";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9);

            // Таблица данных истории продаж
            var dataGridView = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(740, 400),
                BackgroundColor = Color.FromArgb(244, 232, 211),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Кнопка закрытия
            var closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(340, 430),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(103, 186, 128),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            closeButton.Click += (s, e) => this.Close();

            this.Controls.Add(dataGridView);
            this.Controls.Add(closeButton);
        }

        // Подгрузка данных истории продаж из базы данных
        private void LoadSalesData()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"SELECT pr.product_name AS ""Наименование продукции"", sh.quantity AS ""Количество"", 
                        sh.sale_date AS ""Дата продажи"", (sh.quantity * pr.min_cost_for_partner) AS ""Сумма"" 
                        FROM sales_history sh join product pr ON sh.product_id = pr.id_product 
                        WHERE sh.partner_id = @partnerId ORDER BY sh.sale_date DESC;";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@partnerId", _partnerId);

                        // Упорядочивание выходной информации в виде таблицы
                        var adapter = new NpgsqlDataAdapter(command);
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        ((DataGridView)this.Controls[0]).DataSource = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории продаж: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Форма расчёта мтоимости материалов
    public class MaterialCalculationForm : Form
    {
        private readonly string _connectionString;

        public MaterialCalculationForm(string connectionString)
        {
            _connectionString = connectionString;

            InitializeComponents();
            LoadProductTypes();
            LoadMaterialTypes();
        }

        // Инициализация элементов формы
        private void InitializeComponents()
        {
            this.Text = "Расчёт материалов";
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9);

            // Элементы формы
            var productTypeLabel = new Label { Text = "Тип продукции: ", Location = new Point(20, 20) };
            var productTypeComboBox = new ComboBox { Location = new Point(150, 20), Size = new Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var materialTypeLabel = new Label { Text = "Тип материала:", Location = new Point(20, 60) };
            var materialTypeComboBox = new ComboBox { Location = new Point(150, 60), Size = new Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var productCountLabel = new Label { Text = "Количество продукции:", Location = new Point(20, 100) };
            var productCountTextBox = new TextBox { Location = new Point(150, 100), Size = new Size(100, 25) };

            var param1Label = new Label { Text = "Параметр 1:", Location = new Point(20, 140) };
            var param1TextBox = new TextBox { Location = new Point(150, 140), Size = new Size(100, 25) };

            var param2Label = new Label { Text = "Параметр 2:", Location = new Point(20, 180) };
            var param2TextBox = new TextBox { Location = new Point(150, 180), Size = new Size(100, 25) };

            var resultLabel = new Label { Text = "Результат:", Location = new Point(20, 220) };
            var resultTextBox = new TextBox { Location = new Point(150, 220), Size = new Size(100, 25), ReadOnly = true };

            var calculateButton = new Button
            {
                Text = "Рассчитать",
                Location = new Point(280, 180),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(103, 186, 128),
                ForeColor = Color.White
            };
            calculateButton.Click += (s, e) => CalculateMaterial(
                productTypeComboBox, materialTypeComboBox,
                productCountTextBox, param1TextBox, param2TextBox, resultTextBox);

            var closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(280, 220),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(103, 186, 128),
                ForeColor = Color.White
            };
            closeButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                productTypeLabel, productTypeComboBox,
                materialTypeLabel, materialTypeComboBox,
                productCountLabel, productCountTextBox,
                param1Label, param1TextBox,
                param2Label, param2TextBox,
                resultLabel, resultTextBox,
                calculateButton, closeButton
            });
        }

        // Запрос типов продукции из базы данных
        private void LoadProductTypes()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    var command = new NpgsqlCommand("SELECT id_product_type, product_type FROM product_type", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ((ComboBox)this.Controls[1]).Items.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                        }
                    }

                    ((ComboBox)this.Controls[1]).DisplayMember = "Value";
                    ((ComboBox)this.Controls[1]).ValueMember = "Key";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов продукции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка типов материалов из базы данных
        private void LoadMaterialTypes()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    var command = new NpgsqlCommand("SELECT id_material_type, material_type FROM material_type", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ((ComboBox)this.Controls[3]).Items.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                        }
                    }

                    ((ComboBox)this.Controls[3]).DisplayMember = "Value";
                    ((ComboBox)this.Controls[3]).ValueMember = "Key";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов материалов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Расчёт стоимости
        private void CalculateMaterial(ComboBox productTypeComboBox, ComboBox materialTypeComboBox, TextBox productCountTextBox, TextBox param1TextBox, TextBox param2TextBox, TextBox resultTextBox)
        {
            try
            {
                // Проверка ввода
                if (productTypeComboBox.SelectedIndex < 0 || materialTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип продукции и тип материала", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(productCountTextBox.Text, out int productCount) || productCount <= 0)
                {
                    MessageBox.Show("Введите корректное количество продукции", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!double.TryParse(param1TextBox.Text, out double param1) || param1 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 1", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!double.TryParse(param2TextBox.Text, out double param2) || param2 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 2", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Получаем выбранные ID
                int productTypeId = ((KeyValuePair<int, string>)productTypeComboBox.SelectedItem).Key;
                int materialTypeId = ((KeyValuePair<int, string>)materialTypeComboBox.SelectedItem).Key;

                // Выполнение расчёта
                int result = MaterialCalculator.CalculateMaterialRequired(productTypeId, materialTypeId, productCount, param1, param2, _connectionString);

                if (result == -1)
                {
                    MessageBox.Show("Ошибка расчёта. Проверьте входные данные", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                resultTextBox.Text = result.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчёте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}