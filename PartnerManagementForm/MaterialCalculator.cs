using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace PartnerManagementForm
{
    // Класс расчёта стоимости материалов
    public static class MaterialCalculator
    {
        public static int CalculateMaterialRequired(int productTypeId, int materialTypeId, int productCount, double param1, double param2, string connectionString)
        {
            try
            {
                // Проверка входных параметров
                if (productTypeId <= 0 || materialTypeId <= 0 || productCount <= 0 || param1 <= 0 || param2 <= 0)
                {
                    return -1;
                }

                // Установка культуры для корректного парсинга чисел
                CultureInfo culture = CultureInfo.InvariantCulture;
                double productCoefficient = 0;
                double defectPercentage = 0;

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем коэффициент для типа продукции (текстовое поле)
                    using (var cmd = new NpgsqlCommand("SELECT product_type_coefficient FROM product_type WHERE id_product_type = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", productTypeId);
                        var result = cmd.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(result) || !double.TryParse(result, NumberStyles.Any, culture, out productCoefficient) || productCoefficient <= 0)
                        {
                            MessageBox.Show($"Ошибка: Некорректный коэффициент для типа продукции ID {productTypeId}");
                            return -1;
                        }
                    }

                    // Получаем процент брака для типа материала (текстовое поле)
                    using (var cmd = new NpgsqlCommand("SELECT brak_percent FROM material_type WHERE id_material_type = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", materialTypeId);
                        var result = cmd.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(result) || !double.TryParse(result, NumberStyles.Any, culture, out defectPercentage) || defectPercentage < 0)
                        {
                            MessageBox.Show($"Ошибка: Некорректный процент брака для материала ID {materialTypeId}");
                            return -1;
                        }
                    }
                }

                // Расчет с проверкой переполнения
                checked
                {
                    double materialPerUnit = param1 * param2 * productCoefficient;
                    double totalMaterial = materialPerUnit * productCount;
                    double totalWithDefect = totalMaterial * (1 + defectPercentage / 100);

                    if (totalWithDefect <= 0)
                    {
                        return -1;
                    }

                    return (int)Math.Ceiling(totalWithDefect);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
    }
}