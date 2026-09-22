using System;
using AnalaizerClassLibrary;
using ErrorLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnalizerFormatTests
{
    /// <summary>
    /// Модульні тести методу AnalaizerClass.Format() (лабораторна робота №1, варіант 21).
    /// Тестові дані зберігаються в БД CalculatorTestDB (скрипт Database\CreateCalculatorTestDB.sql).
    /// </summary>
    [TestClass]
    public class FormatTests
    {
        private const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CalculatorTestDB;Integrated Security=True";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            AnalaizerClass.ShowMessage = false;
            AnalaizerClass.expression = "";
        }

        /// <summary>
        /// Data-driven тест: запускається для кожного рядка таблиці dbo.FormatTestCases.
        /// </summary>
        [TestMethod]
        [DataSource("System.Data.SqlClient", ConnectionString, "FormatTestCases", DataAccessMethod.Sequential)]
        public void Format_DataDriven_FromDatabase()
        {
            var row = TestContext.DataRow;
            int id = (int)row["Id"];
            string expression = (string)row["Expression"];
            string description = (string)row["Description"];

            string expected = row["ErrorCode"] == DBNull.Value
                ? (string)row["ExpectedResult"]
                : BuildExpectedError(Convert.ToInt32(row["ErrorCode"]), row["ErrorPosition"]);

            AnalaizerClass.expression = expression;
            string actual = AnalaizerClass.Format();

            Assert.AreEqual(expected, actual, $"Id={id}: {description}");
        }

        /// <summary>
        /// Format() змінює поле expression: після виклику в ньому немає пробілів.
        /// </summary>
        [TestMethod]
        public void Format_RemovesSpacesFromExpressionField()
        {
            AnalaizerClass.expression = " 1 +  ( 2 * 3 ) ";

            AnalaizerClass.Format();

            Assert.AreEqual("1+(2*3)", AnalaizerClass.expression);
        }

        /// <summary>
        /// Помилка повертається з префіксом '&', як вимагає специфікація.
        /// </summary>
        [TestMethod]
        public void Format_ErrorResult_StartsWithAmpersand()
        {
            AnalaizerClass.expression = "1+";

            string actual = AnalaizerClass.Format();

            StringAssert.StartsWith(actual, "&");
        }

        /// <summary>
        /// Метод не перевіряє expression на null.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Format_NullExpression_ThrowsNullReferenceException()
        {
            AnalaizerClass.expression = null;

            AnalaizerClass.Format();
        }

        private static string BuildExpectedError(int code, object position)
        {
            string message = (string)typeof(ErrorsExpression)
                .GetField("ERROR_" + code.ToString("00"))
                .GetValue(null);

            if (position == DBNull.Value)
                return "&" + message;

            return "&" + ErrorsExpression.GetFullStringError(message, Convert.ToInt32(position));
        }
    }
}
