using System;
using AnalaizerClassLibrary;                       // тестований клас AnalaizerClass
using ErrorLibrary;                                // тексти помилок ErrorsExpression
using Microsoft.VisualStudio.TestTools.UnitTesting; // фреймворк модульного тестування MSTest

namespace AnalizerFormatTests
{
    /// <summary>
    /// Модульні тести методу AnalaizerClass.Format() (лабораторна робота №1, варіант 21).
    /// Основні тестові дані зберігаються в таблиці dbo.FormatTestCases бази CalculatorTestDB.
    /// </summary>
    [TestClass]   // позначає клас, що містить тести
    public class FormatTests
    {
        // Рядок підключення до локальної бази SQL Server LocalDB з тестовими даними
        private const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CalculatorTestDB;Integrated Security=True";

        /// <summary>
        /// Контекст тесту. MSTest заповнює його автоматично;
        /// через TestContext.DataRow доступний поточний рядок таблиці з даними.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Виконується перед КОЖНИМ тестом.
        /// Поле expression статичне (спільне для всіх тестів), тому його очищаємо,
        /// щоб тести не впливали один на одного. ShowMessage = false вимикає MessageBox,
        /// інакше тест зупинився б в очікуванні натискання кнопки.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            AnalaizerClass.ShowMessage = false;
            AnalaizerClass.expression = "";
        }

        /// <summary>
        /// Data-driven тест: запускається для кожного рядка таблиці dbo.FormatTestCases.
        /// Колонки таблиці: Id, Expression (вхідний вираз), ExpectedResult (очікуваний рядок),
        /// ErrorCode (номер помилки або NULL), ErrorPosition (позиція помилки або NULL), Description (опис).
        /// </summary>
        [TestMethod]
        [DataSource("System.Data.SqlClient", ConnectionString, "FormatTestCases", DataAccessMethod.Sequential)]
        public void Format_DataDriven_FromDatabase()
        {
            // Arrange: зчитуємо дані поточного тестового випадку з БД
            var row = TestContext.DataRow;
            int id = (int)row["Id"];
            string expression = (string)row["Expression"];
            string description = (string)row["Description"];

            // Якщо ErrorCode = NULL, очікується коректний результат з колонки ExpectedResult,
            // інакше формуємо очікуване повідомлення про помилку
            string expected = row["ErrorCode"] == DBNull.Value
                ? (string)row["ExpectedResult"]
                : BuildExpectedError(Convert.ToInt32(row["ErrorCode"]), row["ErrorPosition"]);

            // Act: викликаємо тестований метод
            AnalaizerClass.expression = expression;
            string actual = AnalaizerClass.Format();

            // Assert: порівнюємо очікуване з фактичним; при провалі виводиться Id та опис випадку
            Assert.AreEqual(expected, actual, $"Id={id}: {description}");
        }

        /// <summary>
        /// Format() змінює поле expression: після виклику в ньому немає пробілів.
        /// Перевіряється побічний ефект методу, а не значення, яке він повертає.
        /// </summary>
        [TestMethod]
        public void Format_RemovesSpacesFromExpressionField()
        {
            // Arrange: вираз із зайвими пробілами
            AnalaizerClass.expression = " 1 +  ( 2 * 3 ) ";

            // Act
            AnalaizerClass.Format();

            // Assert: у полі expression пробілів не лишилось
            Assert.AreEqual("1+(2*3)", AnalaizerClass.expression);
        }

        /// <summary>
        /// Помилка повертається з префіксом '&', як вимагає специфікація.
        /// </summary>
        [TestMethod]
        public void Format_ErrorResult_StartsWithAmpersand()
        {
            // Arrange: вираз закінчується оператором, отже помилка (ERROR_05)
            AnalaizerClass.expression = "1+";

            // Act
            string actual = AnalaizerClass.Format();

            // Assert: результат починається з символу '&'
            StringAssert.StartsWith(actual, "&");
        }

        /// <summary>
        /// Метод не перевіряє expression на null.
        /// Виклик expression.Replace(...) для null призводить до NullReferenceException.
        /// Тест вважається пройденим, якщо виникає саме цей виняток.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Format_NullExpression_ThrowsNullReferenceException()
        {
            // Arrange
            AnalaizerClass.expression = null;

            // Act (очікується виняток)
            AnalaizerClass.Format();
        }

        /// <summary>
        /// Формує очікуване повідомлення про помилку за її номером і позицією з БД.
        /// Текст помилки береться через рефлексію з поля ErrorsExpression.ERROR_XX,
        /// тому тексти помилок не дублюються в базі даних.
        /// </summary>
        /// <param name="code">номер помилки (1 → ERROR_01, 4 → ERROR_04 ...)</param>
        /// <param name="position">позиція помилки у виразі або DBNull, якщо позиції немає</param>
        /// <returns>рядок помилки з префіксом '&', як його повертає Format()</returns>
        private static string BuildExpectedError(int code, object position)
        {
            // "00" додає ведучий нуль: 4 → "04", отже шукаємо поле "ERROR_04"
            string message = (string)typeof(ErrorsExpression)
                .GetField("ERROR_" + code.ToString("00"))
                .GetValue(null);   // null, бо поле статичне

            // Помилка без позиції (наприклад, ERROR_03, ERROR_05)
            if (position == DBNull.Value)
                return "&" + message;

            // Помилка з позицією (наприклад, ERROR_02 — невідомий символ на позиції i)
            return "&" + ErrorsExpression.GetFullStringError(message, Convert.ToInt32(position));
        }
    }
}