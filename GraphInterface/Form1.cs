using AnalaizerClassLibrary;
using CalcClassBr;
using ErrorLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphInterface
{
    public partial class Form1 : Form
    {
        public static string expression = null;
        int memory = 0;          // значення пам'яті (MR, M+, MC)
        string result = "0";     // останній результат з поля Result

        public Form1()
        {
            InitializeComponent();

            // Після натискання будь-якої кнопки фокус повертається в поле виразу
            foreach (Button b in GetAllButtons(this))
                b.Click += (s, ev) => FocusExpression();
        }

        /// <summary>
        /// Повертає всі кнопки форми, включно з кнопками всередині груп
        /// </summary>
        private static IEnumerable<Button> GetAllButtons(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button)
                    yield return (Button)c;
                foreach (Button inner in GetAllButtons(c))
                    yield return inner;
            }
        }

        /// <summary>
        /// Фокус у поле виразу, курсор у кінець рядка
        /// </summary>
        private void FocusExpression()
        {
            textBoxExpression.Focus();
            textBoxExpression.SelectionStart = textBoxExpression.Text.Length;
        }

        /// <summary>
        /// Обробка клавіш до того, як їх отримає кнопка чи поле.
        /// Пробіл ігнорується, Enter обчислює вираз.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space)
                return true;

            if (keyData == Keys.Enter)
            {
                buttonEqual_Click(this, EventArgs.Empty);
                FocusExpression();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Кнопки цифр: дописують цифру в кінець виразу
        private void button1_Click(object sender, EventArgs e) { textBoxExpression.Text += "1"; }
        private void button2_Click(object sender, EventArgs e) { textBoxExpression.Text += "2"; }
        private void button3_Click(object sender, EventArgs e) { textBoxExpression.Text += "3"; }
        private void button4_Click(object sender, EventArgs e) { textBoxExpression.Text += "4"; }
        private void button5_Click(object sender, EventArgs e) { textBoxExpression.Text += "5"; }
        private void button6_Click(object sender, EventArgs e) { textBoxExpression.Text += "6"; }
        private void button7_Click(object sender, EventArgs e) { textBoxExpression.Text += "7"; }
        private void button8_Click(object sender, EventArgs e) { textBoxExpression.Text += "8"; }
        private void button9_Click(object sender, EventArgs e) { textBoxExpression.Text += "9"; }
        private void button0_Click(object sender, EventArgs e) { textBoxExpression.Text += "0"; }

        /// <summary>
        /// Кнопка +/-: змінює знак останнього числа у виразі
        /// </summary>
        private void buttonABS_Click(object sender, EventArgs e)
        {
            string text = textBoxExpression.Text;

            // Поле порожнє або в кінці не число: ставимо мінус перед майбутнім числом
            if (text == "" || !isNumber(text[text.Length - 1]))
            {
                textBoxExpression.Text += "-";
                return;
            }

            // Знаходимо початок останнього числа
            int start = text.Length - 1;
            while (start > 0 && isNumber(text[start - 1]))
                start--;

            string number = text.Substring(start);      // останнє число
            string before = text.Substring(0, start);   // усе, що перед ним
            char prev = before.Length > 0 ? before[before.Length - 1] : ' ';

            if (prev == '-')
            {
                string beforeMinus = before.Substring(0, before.Length - 1);
                char prevPrev = beforeMinus.Length > 0 ? beforeMinus[beforeMinus.Length - 1] : ' ';

                if (beforeMinus == "" || prevPrev == '(' || "+-*/%".IndexOf(prevPrev) >= 0)
                    textBoxExpression.Text = beforeMinus + number;        // унарний мінус: "-5" → "5"
                else
                    textBoxExpression.Text = beforeMinus + "+" + number;  // бінарний: "2-5" → "2+5"
            }
            else if (prev == '+')
            {
                textBoxExpression.Text = before.Substring(0, before.Length - 1) + "-" + number; // "2+5" → "2-5"
            }
            else if (prev != ')')
            {
                textBoxExpression.Text = before + "-" + number;           // "5" → "-5", "2*5" → "2*-5"
            }
        }

        /// <summary>
        /// Перевірка, чи є символ цифрою 0-9
        /// </summary>
        private bool isNumber(char number)
        {
            return number >= '0' && number <= '9';
        }

        // Кнопки операцій
        private void buttonDiv_Click(object sender, EventArgs e) { textBoxExpression.Text += "/"; }
        private void buttonMult_Click(object sender, EventArgs e) { textBoxExpression.Text += "*"; }
        private void buttonSub_Click(object sender, EventArgs e) { textBoxExpression.Text += "-"; }
        private void buttonAdd_Click(object sender, EventArgs e) { textBoxExpression.Text += "+"; }
        private void buttonMod_Click(object sender, EventArgs e) { textBoxExpression.Text += "%"; }

        /// <summary>
        /// MR: вставляє значення пам'яті у вираз (якщо перед ним не стоїть цифра)
        /// </summary>
        private void buttonMR_Click(object sender, EventArgs e)
        {
            string text = textBoxExpression.Text;
            if (text == "" || !isNumber(text[text.Length - 1]))
                textBoxExpression.Text += memory.ToString();

            label2.Text = "Memory";
            textBoxResult.Text = memory.ToString();
        }

        /// <summary>
        /// M+: обчислює вираз і додає результат до пам'яті з перевіркою на переповнення
        /// </summary>
        private void buttonMPlus_Click(object sender, EventArgs e)
        {
            buttonEqual_Click(sender, e);

            if (result == "")
                return;

            int checkResult;
            // TryParse перетворює рядок на int і повертає false, якщо це не число (наприклад, текст помилки)
            if (int.TryParse(result, out checkResult))
            {
                long sum = (long)memory + checkResult;
                if (sum > int.MaxValue || sum < int.MinValue)
                    MessageBox.Show(ErrorsExpression.ERROR_06);
                else
                    memory = (int)sum;
            }
            else
                MessageBox.Show("Error code can't be written into memory!");
        }

        /// <summary>
        /// MC: очищення пам'яті
        /// </summary>
        private void buttonMC_Click(object sender, EventArgs e)
        {
            memory = 0;
            textBoxResult.Text = "";
        }

        /// <summary>
        /// "=": обчислення виразу через AnalaizerClass.Estimate()
        /// </summary>
        private void buttonEqual_Click(object sender, EventArgs e)
        {
            label2.Text = "Result";

            // Прибираємо всі пробільні символи (пробіл, табуляція, нерозривний пробіл)
            string text = new string(textBoxExpression.Text.Where(c => !char.IsWhiteSpace(c)).ToArray());

            AnalaizerClass.expression = text;
            string results = AnalaizerClass.Estimate();
            bool isError = results.StartsWith("&");   // '&' на початку означає помилку

            if (isError)
            {
                // Помилка: червоний дрібний шрифт
                textBoxResult.ForeColor = Color.Red;
                textBoxResult.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(204)));
                textBoxResult.TextAlign = HorizontalAlignment.Left;
            }
            else
            {
                // Результат: синій жирний шрифт
                textBoxResult.ForeColor = Color.Blue;
                textBoxResult.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(204)));
                textBoxResult.TextAlign = HorizontalAlignment.Right;
            }

            // Службовий символ '&' користувачу не показуємо
            textBoxResult.Text = isError ? results.Substring(1) : results;
        }

        /// <summary>
        /// "(": якщо перед дужкою число або ')', автоматично додається множення: 2( → 2*(
        /// </summary>
        private void buttonOpenBracket_Click(object sender, EventArgs e)
        {
            string text = textBoxExpression.Text;
            if (text != "" && (isNumber(text[text.Length - 1]) || text[text.Length - 1] == ')'))
                textBoxExpression.Text += "*(";
            else
                textBoxExpression.Text += "(";
        }

        private void buttonCloseBracket_Click(object sender, EventArgs e)
        {
            textBoxExpression.Text += ")";
        }

        /// <summary>
        /// Backspace: видалення останнього символу
        /// </summary>
        private void buttonBS_Click(object sender, EventArgs e)
        {
            if (textBoxExpression.Text.Length > 0)
                textBoxExpression.Text = textBoxExpression.Text.Substring(0, textBoxExpression.Text.Length - 1);
        }

        /// <summary>
        /// C: очищення обох полів
        /// </summary>
        private void buttonC_Click(object sender, EventArgs e)
        {
            textBoxExpression.Text = string.Empty;
            textBoxResult.Text = string.Empty;
        }

        /// <summary>
        /// Зміна тексту виразу: зайвий нуль на початку числа прибирається ("05" → "5")
        /// </summary>
        private void textBoxExpression_TextChanged(object sender, EventArgs e)
        {
            expression = textBoxExpression.Text;
            string text = textBoxExpression.Text;
            if (text.Length > 1 && text[0] == '0' && isNumber(text[1]))
                textBoxExpression.Text = text.Substring(1);
        }

        /// <summary>
        /// Натискання клавіші: фокус у поле виразу, Esc закриває програму
        /// </summary>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            textBoxExpression.Focus();
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void textBoxResult_TextChanged(object sender, EventArgs e)
        {
            result = textBoxResult.Text;
        }

        /// <summary>
        /// Фільтр клавіатури: дозволені лише цифри, + - * / % ( )
        /// і службові клавіші (Backspace, Ctrl+C/V/A)
        /// </summary>
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;
            bool allowed = isNumber(c) || "+-*/%()".IndexOf(c) >= 0 || char.IsControl(c);
            if (!allowed)
                e.Handled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
        }
    }
}