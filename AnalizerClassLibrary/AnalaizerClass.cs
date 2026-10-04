using CalcClassBr;
using ErrorLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnalaizerClassLibrary
{
    /// <summary>
    /// Аналізатор арифметичних виразів.
    /// Порядок обробки: CheckCurrency (дужки) → ReplaceUnaryPlusMinus (унарні +/-) →
    /// Format (синтаксис) → CreateStack (зворотний польський запис) → RunEstimate (обчислення).
    /// Помилки повертаються рядком, що починається з символу '&'.
    /// </summary>
    public static class AnalaizerClass
    {
        // Символи, з яких може складатися вираз
        private const char SYMBOL_CLOSE_BRACKET = ')';
        private const char SYMBOL_OPEN_BRACKET = '(';
        private const char SYMBOL_OPERATOR_ADD = '+';
        private const char SYMBOL_OPERATOR_SUB = '-';
        private const char SYMBOL_OPERATOR_DIV = '/';
        private const char SYMBOL_OPERATOR_MUL = '*';
        private const char SYMBOL_OPERATOR_MOD = '%';
        // Унарні плюс і мінус позначаються окремими символами, щоб не плутати їх з бінарними
        private const char SYMBOL_UNARY_PLUS = 'p';
        private const char SYMBOL_UNARY_MINUS = 'm';


        private static readonly char[] _operators = new char[]  // бінарні операції
            {
                SYMBOL_OPERATOR_ADD,
                SYMBOL_OPERATOR_SUB,
                SYMBOL_OPERATOR_MUL,
                SYMBOL_OPERATOR_DIV,
                SYMBOL_OPERATOR_MOD
            };

        private static readonly char[] _unary_operators = new char[]  // унарні операції
            {
                SYMBOL_UNARY_MINUS,
                SYMBOL_UNARY_PLUS
            };

        private static readonly char[] _brackets = new char[]  // дужки для розділу виразів
            {
                SYMBOL_OPEN_BRACKET,
                SYMBOL_CLOSE_BRACKET
            };

        /// <summary> 
        /// максимальна глибина вкладеності
        /// </summary> 
        private const int MAX_DEPTH_BRACKET = 3;

        /// <summary> 
        /// максимальна довжина виразу (символів)
        /// </summary> 
        private const int MAX_LENGHT_EXPRESSION = 65536;

        /// <summary> 
        /// максимальна кількість операторів та чисел у виразі
        /// </summary> 
        private const int MAX_COUNT_OPERANDS = 30;


        /// <summary> 
        /// позиція виразу, на якій знайдена синтаксична помилка 
        /// (у випадку відловлення на рівні виконання - не визначається) 
        /// </summary>     
        private static int erposition = 0;

        /// <summary>
        /// Вхідний вираз
        /// </summary>        
        public static string expression = "";

        /// <summary>
        /// Показує, чи є необхідність у виведенні повідомлень про помилки.
        /// У разі консольного запуску програми це значення - false.
        /// </summary>
        public static bool ShowMessage = false;


        /// <summary>
        /// Перевірка коректності структури в дужках вхідного виразу
        /// </summary>
        /// <returns> true - якщо все нормально, false - якщо є помилка </returns>
        /// метод біжить по вхідному виразу, символ за символом аналізуючи його, і рахуючи кількість дужок.
        /// У разі виникнення помилки повертає false, а в erposition записує позицію, на якій виникла помилка.
        public static bool CheckCurrency()
        {
            erposition = 0;

            // Стек позицій відкритих дужок: '(' кладемо, ')' забираємо
            Stack<int> openBracket = new Stack<int>();

            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i] == SYMBOL_OPEN_BRACKET)
                {
                    openBracket.Push(i);

                    // Кількість елементів у стеку дорівнює поточній глибині вкладеності
                    if (openBracket.Count > MAX_DEPTH_BRACKET)
                    {
                        erposition = i;
                        if (ShowMessage)
                            MessageBox.Show
                                ($"expression: '{expression}'\nerposition: {erposition}\nError in expression: Maximum depth bracket {MAX_DEPTH_BRACKET}",
                                 "Error",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Error);
                        return false;
                    }
                }
                else
                {
                    if (expression[i] == ')')
                    {
                        if (openBracket.Count == 0) // закриваюча дужка використана без відкриваючої
                        {
                            erposition = i;
                            if (ShowMessage)
                                MessageBox.Show
                                    ($"expression: '{expression}'\nerposition: {erposition}\nError in expression: '{SYMBOL_CLOSE_BRACKET}' used without '{SYMBOL_OPEN_BRACKET}'",
                                     "Error",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Error);
                            return false;
                        }
                        else
                            openBracket.Pop(); // пару знайдено, прибираємо '(' зі стеку
                    }
                }
            }

            if (openBracket.Count > 0) // відкриваючих дужок більше, ніж закриваючих
            {
                erposition = openBracket.Peek(); // позиція останньої незакритої '('
                if (ShowMessage)
                    MessageBox.Show
                        ($"expression: '{expression}'\nerposition: {erposition}\nError in expression: '{SYMBOL_OPEN_BRACKET}' used without '{SYMBOL_CLOSE_BRACKET}'",
                         "Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error);
                return false;
            }

            return true;
        }


        ///<summary>
        /// Форматує вхідний вираз, видаляючи пропуски,
        /// а також знаходить нерозпізнані оператори, стежить за кінцем рядка 
        /// а також знаходить помилки в кінці рядка 
        /// </summary>
        ///<returns> кінцевий рядок або повідомлення про помилку, що починаються з спец. символу &</returns>
        public static string Format()
        {
            // Видаляємо всі пробіли (змінюється саме поле expression)
            expression = expression.Replace(" ", "");


            // Перевірка на максимальну довжину виразу
            if (expression.Length > MAX_LENGHT_EXPRESSION)
                return "&" + ErrorsExpression.ERROR_07;

            // Порожній вираз не є помилкою, повертаємо порожній рядок
            if (expression == "") return "";

            // Перший прохід: у виразі дозволені лише цифри, оператори, дужки та m/p
            for (int i = 0; i < expression.Length; i++)
            {
                char currentSymbol = expression[i];

                // перевірка на невідомий символ оператора
                if (char.IsDigit(currentSymbol) ||
                    _operators.Contains(currentSymbol) ||
                    _brackets.Contains(currentSymbol) ||
                    _unary_operators.Contains(currentSymbol))
                    continue;

                return "&" + ErrorsExpression.GetFullStringError(ErrorsExpression.ERROR_02, i);
            }

            char startSymbol = expression[0];
            // перевірка на невірний початок виразу (допустимі: цифра, '(', m, p)
            if (!char.IsDigit(startSymbol) &&
                startSymbol != SYMBOL_OPEN_BRACKET &&
                startSymbol != SYMBOL_UNARY_MINUS &&
                startSymbol != SYMBOL_UNARY_PLUS)
                return "&" + ErrorsExpression.ERROR_03;


            char endSymbol = expression[expression.Length - 1];
            // перевірка на закінчення всього виразу (допустимі: цифра або ')')
            if (!char.IsDigit(endSymbol) &&
                endSymbol != SYMBOL_CLOSE_BRACKET)
                return "&" + ErrorsExpression.ERROR_05;


            // Другий прохід: перевірка, чи допустимий символ, що стоїть після поточного.
            // Умова i < expression.Length - 1 захищає від виходу за межі рядка
            for (int i = 0; i < expression.Length; i++)
            {
                char currentSymbol = expression[i];

                // після цифри не може стояти '(' або унарний оператор, наприклад "2(3)"
                if (char.IsDigit(currentSymbol))
                {
                    if (i < expression.Length - 1)
                    {
                        char nextSymbol = expression[i + 1];
                        if (nextSymbol == SYMBOL_OPEN_BRACKET || nextSymbol == SYMBOL_UNARY_MINUS || nextSymbol == SYMBOL_UNARY_PLUS)
                        {
                            return "&" + ErrorsExpression.GetFullStringError(ErrorsExpression.ERROR_01, i + 1);
                        }
                    }
                }


                // перевірка на два оператори підряд, наприклад "2*/3"
                if (_operators.Contains(currentSymbol))
                {
                    if (i < expression.Length - 1)
                    {
                        char nextSymbol = expression[i + 1];
                        if (_operators.Contains(nextSymbol))
                            return "&" + ErrorsExpression.GetFullStringError(ErrorsExpression.ERROR_04, i + 1);

                        // після оператора має бути число, '(' або унарний оператор
                        if (!char.IsDigit(nextSymbol) && nextSymbol != SYMBOL_OPEN_BRACKET && nextSymbol != SYMBOL_UNARY_MINUS && nextSymbol != SYMBOL_UNARY_PLUS)
                            return "&" + ErrorsExpression.ERROR_03;
                    }
                }

                // після '(' не може бути ')' або бінарного оператора, наприклад "()" чи "(*2)"
                if (currentSymbol == SYMBOL_OPEN_BRACKET)
                {
                    if (i < expression.Length - 1)
                    {
                        char nextSymbol = expression[i + 1];
                        if (nextSymbol == SYMBOL_CLOSE_BRACKET || _operators.Contains(nextSymbol))
                            return "&" + ErrorsExpression.ERROR_03;
                    }
                }

                // після ')' може бути тільки бінарний оператор або ще одна ')'
                if (currentSymbol == SYMBOL_CLOSE_BRACKET)
                {
                    if (i < expression.Length - 1)
                    {
                        char nextSymbol = expression[i + 1];
                        if (!_operators.Contains(nextSymbol) && nextSymbol != SYMBOL_CLOSE_BRACKET)
                            return "&" + ErrorsExpression.ERROR_03;
                    }
                }

                // після унарного оператора має бути число або '('; в кінці виразу він стояти не може
                if (_unary_operators.Contains(currentSymbol))
                {
                    if (i < expression.Length - 1)
                    {
                        char nextSymbol = expression[i + 1];
                        if (nextSymbol == SYMBOL_CLOSE_BRACKET || _unary_operators.Contains(nextSymbol) || _operators.Contains(nextSymbol))
                            return "&" + ErrorsExpression.ERROR_03;
                    }
                    else
                        return "&" + ErrorsExpression.ERROR_05;
                }

            }

            // Помилок не знайдено, повертаємо очищений вираз
            return expression;

        }


        /// <summary>
        /// метод визначає, чи є рядок оператором або дужкою
        /// </summary> 
        /// <param name="s"></param>
        /// <returns> true - якщо символ є оператором, false - якщо символ не є оператором </returns>        
        private static bool IsOperator(string s)
        {
            // Оператор завжди складається з одного символу; числа мають довжину 1 і більше
            if (s.Length == 1)
            {
                char c = s[0];
                if (_operators.Contains(c) || _brackets.Contains(c) || _unary_operators.Contains(c))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// метод визначає, чи є символ розділювачем (у програмі не використовується)
        /// </summary>
        /// <param name="c"></param>
        /// <returns> true - символ є пробілом, інакше false</returns>        
        private static bool IsDelimeter(char c)
        {
            return (c == ' ' ? true : false);
        }

        /// <summary>
        /// метод повертає пріоритет оператора (більше число - виконується раніше)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>         
        static private byte GetPriority(string s)
        {
            switch (s)
            {
                case "(":
                case ")":
                    return 0;   // дужки мають найнижчий пріоритет у стеку
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                    return 2;
                case "%":
                    return 3;
                case "m":
                case "p":
                    return 4;   // унарні операції виконуються першими
                default:
                    return 5;
            }
        }

        /// <summary>
        /// Розбиває вираз на токени (числа, оператори, дужки).
        /// Наприклад: "12+m(3*45)" → "12", "+", "m", "(", "3", "*", "45", ")"
        /// </summary>
        public static IEnumerable<string> Separate(string input)
        {
            int pos = 0;
            while (pos < input.Length)
            {
                string s = string.Empty + input[pos];

                // Якщо символ не оператор і не дужка, це початок числа: додаємо наступні цифри
                if (!_operators.Union(_brackets).Union(_unary_operators).Contains(input[pos]))
                {
                    if (Char.IsDigit(input[pos]))
                        for (int i = pos + 1;
                            i < input.Length && Char.IsDigit(input[i]);
                            i++)
                            s += input[i];
                    else if (Char.IsLetter(input[pos]))   // для слів; після Format() не спрацьовує
                        for (int i = pos + 1; i < input.Length &&
                            (Char.IsLetter(input[i]) || Char.IsDigit(input[i])); i++)
                            s += input[i];
                }
                yield return s;      // повертаємо токени по одному
                pos += s.Length;     // переходимо до наступного токена
            }
        }

        ///<summary>
        /// Формує масив, в якому розташовуються оператори і символи 
        /// представлені в зворотному польському записі (без дужок)
        /// На цьому ж етапі відшукується решта всіх помилок (див. код).
        /// По суті - це компіляція.
        /// </summary>
        /// <returns> масив зворотного польського запису </returns>
        /// Використовується алгоритм «сортувальної станції» Дейкстри:
        /// числа одразу йдуть у результат, оператори тимчасово зберігаються у стеку
        /// і виштовхуються з нього з урахуванням пріоритету.
        /// Приклад: "2+3*4" → 2 3 4 * +
        public static ArrayList CreateStack()
        {
            ArrayList result = new ArrayList();         // вихідна послідовність (польський запис)
            Stack<string> stack = new Stack<string>();  // стек операторів

            foreach (string c in Separate(expression))
            {
                if (IsOperator(c))
                {
                    if (stack.Count > 0 && !c.Equals(SYMBOL_OPEN_BRACKET.ToString()))
                    {
                        if (c.Equals(SYMBOL_CLOSE_BRACKET.ToString()))
                        {
                            // ')' — переносимо оператори в результат до найближчої '('; саму '(' відкидаємо
                            string s = stack.Pop();
                            while (s != SYMBOL_OPEN_BRACKET.ToString())
                            {
                                result.Add(s);
                                s = stack.Pop();
                            }
                        }
                        else
                            if (GetPriority(c) > GetPriority(stack.Peek()))
                            stack.Push(c);   // пріоритет вищий за верхній оператор, просто кладемо в стек
                        else
                        {
                            // виштовхуємо оператори з вищим або рівним пріоритетом, потім кладемо поточний
                            while (stack.Count > 0 && GetPriority(c) <= GetPriority(stack.Peek()))
                                result.Add(stack.Pop());
                            stack.Push(c);
                        }
                    }
                    else
                        stack.Push(c);   // стек порожній або це '('
                }
                else
                    result.Add(c);       // число одразу йде в результат
            }

            // Залишок операторів переносимо в результат (foreach по Stack іде від вершини)
            if (stack.Count > 0)
                foreach (string c in stack)
                    result.Add(c);

            return result;

        }


        ///<summary>
        /// Обчислення зворотного польського запису
        /// </summary>
        /// <returns> результат обчислень, або повідомлення про помилку </returns>
        /// Числа кладуться в стек; коли трапляється оператор, зі стеку береться
        /// один (унарний) або два (бінарний) операнди, а результат кладеться назад.
        public static string RunEstimate()
        {
            Stack<string> stack = new Stack<string>();   // стек операндів
            Queue<string> queue = new Queue<string>();   // черга токенів польського запису
            foreach (var item in CreateStack())
            {
                queue.Enqueue((string)item);
            }

            // Вираз з одного числа повертаємо без обчислень
            if (queue.Count == 1)
            {
                return queue.Dequeue();
            }
            else
            {
                if (queue.Count > MAX_COUNT_OPERANDS)
                    return "&" + ErrorsExpression.ERROR_08;
            }

            string str = queue.Dequeue();

            // Умова завжди істинна; вихід з циклу через break, коли черга порожня
            while (queue.Count >= 0)
            {
                if (!IsOperator(str))
                {
                    stack.Push(str);         // операнд кладемо в стек
                    str = queue.Dequeue();
                }
                else
                {
                    long res = 0;
                    try
                    {
                        // Для бінарних операцій першим зі стеку виходить ДРУГИЙ операнд (b)
                        switch (str)
                        {
                            case "+":
                                {
                                    long b = Convert.ToInt64(stack.Pop());
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.Add(a, b);
                                    break;
                                }
                            case "-":
                                {
                                    long b = Convert.ToInt64(stack.Pop());
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.Sub(a, b);
                                    break;
                                }
                            case "*":
                                {
                                    long b = Convert.ToInt64(stack.Pop());
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.Mult(a, b);
                                    break;
                                }
                            case "/":
                                {
                                    long b = Convert.ToInt64(stack.Pop());
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.Div(a, b);
                                    break;
                                }
                            case "%":
                                {
                                    long b = Convert.ToInt64(stack.Pop());
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.Mod(a, b);
                                    break;
                                }
                            case "m":   // унарний мінус: один операнд
                                {
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.IABS(a);
                                    break;
                                }
                            case "p":   // унарний плюс: один операнд
                                {
                                    long a = Convert.ToInt64(stack.Pop());
                                    res = CalcClass.ABS(a);
                                    break;
                                }
                        }
                    }

                    // CalcClass кидає виняток при переповненні або діленні на 0,
                    // а текст помилки зберігає у lastError
                    catch
                    {
                        return "&" + CalcClass.lastError;
                    }


                    stack.Push(res.ToString());   // проміжний результат повертаємо в стек
                    if (queue.Count > 0)
                        str = queue.Dequeue();
                    else
                        break;
                }
            }

            // Після обробки всіх токенів у стеку лишається результат
            return stack.Pop();


        }

        /// <summary>
        /// Замінює символ на позиції position на symbol (незважаючи на назву, не вставляє новий символ)
        /// </summary>
        private static string InsertSymbol(string input, char symbol, int position)
        {
            string res = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (i == position) res += symbol;
                else res += input[i];
            }
            return res;
        }

        /// <summary>
        /// Знаходить унарні '+' і '-' та замінює їх на 'p' і 'm'.
        /// Знак вважається унарним, якщо перед ним початок рядка, оператор або '(',
        /// а після нього число або '('. Приклад: "-5+(-3)" → "m5+(m3)"
        /// </summary>
        public static string ReplaceUnaryPlusMinus(string input)
        {
            string res = input.Replace(" ", "");

            for (int i = 0; i < res.Length; i++)
            {
                char currentSymbol = res[i];
                if (currentSymbol == SYMBOL_OPERATOR_ADD)
                {
                    // Для першого символу рядка вважаємо, що перед ним стоїть оператор
                    char previosSymbol = SYMBOL_OPERATOR_MUL;

                    if (i > 0) previosSymbol = res[i - 1];

                    if (i < res.Length - 1)
                    {
                        char nextSymbol = res[i + 1];
                        if ((nextSymbol == SYMBOL_OPEN_BRACKET || char.IsDigit(nextSymbol)) && (_operators.Contains(previosSymbol) || previosSymbol == SYMBOL_OPEN_BRACKET))
                            res = InsertSymbol(res, SYMBOL_UNARY_PLUS, i);
                    }
                }

                // Аналогічна перевірка для мінуса
                if (currentSymbol == SYMBOL_OPERATOR_SUB)
                {
                    char previosSymbol = SYMBOL_OPERATOR_MUL;

                    if (i > 0) previosSymbol = res[i - 1];

                    if (i < res.Length - 1)
                    {
                        char nextSymbol = res[i + 1];
                        if ((nextSymbol == SYMBOL_OPEN_BRACKET || char.IsDigit(nextSymbol)) && (_operators.Contains(previosSymbol) || previosSymbol == SYMBOL_OPEN_BRACKET))
                            res = InsertSymbol(res, SYMBOL_UNARY_MINUS, i);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Метод, який організовує обчислення. 
        /// По черзі запускає CheckCurrency, ReplaceUnaryPlusMinus, Format і RunEstimate (який викликає CreateStack)
        /// </summary>
        /// <returns> результат обчислення або повідомлення про помилку (з '&' на початку) </returns>
        public static string Estimate()
        {
            // 1. Перевірка дужок
            if (!CheckCurrency())
                return "&" + ErrorsExpression.GetFullStringError(ErrorsExpression.ERROR_01, erposition);

            // 2. Заміна унарних плюса і мінуса на p/m
            expression = ReplaceUnaryPlusMinus(expression);

            // 3. Синтаксична перевірка
            string format = Format();

            if (format == "") return "";

            if (format.StartsWith("&"))
                return format;

            // 4. Переведення у польський запис і обчислення
            return RunEstimate();

        }

    }
}