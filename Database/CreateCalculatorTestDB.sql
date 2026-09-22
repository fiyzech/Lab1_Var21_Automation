/*
    Лабораторна робота №1, варіант 21
    База тестових даних для модульних тестів методу AnalaizerClass.Format()

    Запуск: SSMS -> підключитися до (localdb)\MSSQLLocalDB -> виконати скрипт.
*/
USE master;
GO

IF DB_ID(N'CalculatorTestDB') IS NOT NULL
BEGIN
    ALTER DATABASE CalculatorTestDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CalculatorTestDB;
END
GO

CREATE DATABASE CalculatorTestDB;
GO

USE CalculatorTestDB;
GO

/* Довідник кодів помилок, які може повернути Format() (див. SpecCalc, п. 2.2.3) */
CREATE TABLE dbo.ErrorCodes
(
    Code        TINYINT        NOT NULL CONSTRAINT PK_ErrorCodes PRIMARY KEY,
    HasPosition BIT            NOT NULL,   -- 1: повідомлення містить позицію <i>
    Description NVARCHAR(200)  NOT NULL
);
GO

INSERT INTO dbo.ErrorCodes (Code, HasPosition, Description) VALUES
(1, 1, N'Неправильна структура в дужках, помилка на <i> символі'),
(2, 1, N'Невідомий оператор на <i> символі'),
(3, 0, N'Невірна синтаксична конструкція вхідного виразу'),
(4, 1, N'Два підряд оператори на <i> символі'),
(5, 0, N'Незавершений вираз'),
(7, 0, N'Дуже довгий вираз');
GO

/*
    Тестові випадки.
    Якщо очікується успіх   - заповнене ExpectedResult, ErrorCode = NULL.
    Якщо очікується помилка - ExpectedResult = NULL, заповнені ErrorCode (+ ErrorPosition для кодів з позицією).
*/
CREATE TABLE dbo.FormatTestCases
(
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FormatTestCases PRIMARY KEY,
    Expression     NVARCHAR(MAX)     NOT NULL,
    ExpectedResult NVARCHAR(MAX)     NULL,
    ErrorCode      TINYINT           NULL CONSTRAINT FK_FormatTestCases_ErrorCodes REFERENCES dbo.ErrorCodes(Code),
    ErrorPosition  INT               NULL,
    Category       NVARCHAR(50)      NOT NULL,
    Description    NVARCHAR(250)     NOT NULL,
    CONSTRAINT CK_FormatTestCases_Expected CHECK
    (
        (ExpectedResult IS NOT NULL AND ErrorCode IS NULL AND ErrorPosition IS NULL) OR
        (ExpectedResult IS NULL AND ErrorCode IS NOT NULL)
    )
);
GO

/* ---------- Коректні вирази ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ExpectedResult, Category, Description) VALUES
(N'7',                   N'7',               N'Valid', N'Одне число з однієї цифри'),
(N'1+2',                 N'1+2',             N'Valid', N'Простий вираз без пробілів'),
(N'1 + 2',               N'1+2',             N'Valid', N'Пробіли між операндами видаляються'),
(N'  ( 12 * 3 ) % 5  ',  N'(12*3)%5',        N'Valid', N'Пробіли на початку, в кінці та біля дужок'),
(N'((2+3)*(4-1))/5',     N'((2+3)*(4-1))/5', N'Valid', N'Вкладені дужки, усі бінарні оператори'),
(N'm5+p3',               N'm5+p3',           N'Valid', N'Унарні мінус і плюс на початку та після оператора'),
(N'2*m3',                N'2*m3',            N'Valid', N'Оператор, за яким іде унарний мінус'),
(N'2-(3+4)',             N'2-(3+4)',         N'Valid', N'Оператор, за яким іде відкриваюча дужка'),
(N'(m(1+2))',            N'(m(1+2))',        N'Valid', N'Унарний мінус після "(" і перед "("'),
(N'((1))',               N'((1))',           N'Valid', N'Закриваюча дужка після закриваючої'),
(N'(1',                  N'(1',              N'Valid', N'Баланс дужок Format не перевіряє (це робить CheckCurrency)'),
(N'2147483648',          N'2147483648',      N'Valid', N'Діапазон int Format не перевіряє'),
(N'',                    N'',                N'Empty', N'Порожній вираз'),
(N'     ',               N'',                N'Empty', N'Вираз лише з пробілів');

/* Граничні значення довжини (MAX_LENGHT_EXPRESSION = 65536) */
INSERT INTO dbo.FormatTestCases (Expression, ExpectedResult, Category, Description) VALUES
(REPLICATE(CAST(N'1' AS NVARCHAR(MAX)), 65536),
 REPLICATE(CAST(N'1' AS NVARCHAR(MAX)), 65536),
 N'Length', N'Довжина рівно 65536 символів - допустима'),
(REPLICATE(CAST(N'1' AS NVARCHAR(MAX)), 65536) + N'   ',
 REPLICATE(CAST(N'1' AS NVARCHAR(MAX)), 65536),
 N'Length', N'65539 символів з пробілами: довжина перевіряється після видалення пробілів');

INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, Category, Description) VALUES
(REPLICATE(CAST(N'1' AS NVARCHAR(MAX)), 65537),
 7, N'Length', N'Довжина 65537 символів - Error 07'),
(REPLICATE(CAST(N'1+' AS NVARCHAR(MAX)), 32768) + N'1',
 7, N'Length', N'Довгий вираз з операторами (65537 символів) - Error 07');

/* ---------- Невідомі символи (Error 02 at <i>) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, ErrorPosition, Category, Description) VALUES
(N'a',                  2, 0, N'UnknownSymbol', N'Вираз з однієї літери'),
(N'2+x',                2, 2, N'UnknownSymbol', N'Літера в кінці виразу'),
(N'1 + a',              2, 2, N'UnknownSymbol', N'Позиція рахується після видалення пробілів'),
(N'3^2',                2, 1, N'UnknownSymbol', N'Непідтримуваний оператор ^'),
(N'1.5',                2, 1, N'UnknownSymbol', N'Дробове число'),
(N'[1+2]',              2, 0, N'UnknownSymbol', N'Квадратні дужки'),
(N'5mod3',              2, 2, N'UnknownSymbol', N'Текстовий mod (у коді використовується %)'),
(N'1' + NCHAR(9) + N'+2', 2, 1, N'UnknownSymbol', N'Символ табуляції не видаляється як пробіл'),
(N'+a',                 2, 1, N'UnknownSymbol', N'Невідомий символ перевіряється раніше за початок виразу');

/* ---------- Невірний початок виразу (Error 03) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, Category, Description) VALUES
(N'+1',  3, N'BadStart', N'Вираз починається з +'),
(N'*2',  3, N'BadStart', N'Вираз починається з *'),
(N'%5',  3, N'BadStart', N'Вираз починається з %'),
(N')1',  3, N'BadStart', N'Вираз починається з )'),
(N'+1+', 3, N'BadStart', N'Початок перевіряється раніше за кінець');

/* ---------- Незавершений вираз (Error 05) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, Category, Description) VALUES
(N'1+',  5, N'BadEnd', N'Вираз закінчується бінарним оператором'),
(N'2*(', 5, N'BadEnd', N'Вираз закінчується відкриваючою дужкою'),
(N'm',   5, N'BadEnd', N'Вираз лише з унарного мінуса'),
(N'1+*', 5, N'BadEnd', N'Кінець перевіряється раніше за два оператори підряд');

/* ---------- Після цифри йде "(", "m" або "p" (Error 01 at <i>) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, ErrorPosition, Category, Description) VALUES
(N'2(3)', 1, 1, N'AfterDigit', N'Цифра, за якою йде відкриваюча дужка'),
(N'2m3',  1, 1, N'AfterDigit', N'Цифра, за якою йде унарний мінус'),
(N'12p3', 1, 2, N'AfterDigit', N'Багатоцифрове число, за яким іде унарний плюс');

/* ---------- Два оператори підряд (Error 04 at <i>) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, ErrorPosition, Category, Description) VALUES
(N'1+*2',     4, 2, N'TwoOperators', N'+ і * підряд'),
(N'1--2',     4, 2, N'TwoOperators', N'Два мінуси підряд'),
(N'(1+2)*/3', 4, 6, N'TwoOperators', N'* і / підряд після дужок');

/* ---------- Невірні синтаксичні конструкції (Error 03) ---------- */
INSERT INTO dbo.FormatTestCases (Expression, ErrorCode, Category, Description) VALUES
(N'(1+)',   3, N'Syntax', N'Бінарний оператор перед закриваючою дужкою'),
(N'()',     3, N'Syntax', N'Порожні дужки'),
(N'2+()',   3, N'Syntax', N'Порожні дужки після оператора'),
(N'(+1)',   3, N'Syntax', N'Бінарний оператор після відкриваючої дужки'),
(N'(*2)',   3, N'Syntax', N'Множення після відкриваючої дужки'),
(N'(1)(2)', 3, N'Syntax', N'Відкриваюча дужка після закриваючої'),
(N'(1)2',   3, N'Syntax', N'Число після закриваючої дужки'),
(N'(1)m2',  3, N'Syntax', N'Унарний мінус після закриваючої дужки'),
(N'(m)',    3, N'Syntax', N'Закриваюча дужка після унарного мінуса'),
(N'mm2',    3, N'Syntax', N'Два унарні мінуси підряд'),
(N'pm2',    3, N'Syntax', N'Унарний плюс, за яким іде унарний мінус'),
(N'm+2',    3, N'Syntax', N'Бінарний плюс після унарного мінуса'),
(N'p*2',    3, N'Syntax', N'Множення після унарного плюса');
GO

SELECT Category, COUNT(*) AS Cases FROM dbo.FormatTestCases GROUP BY Category ORDER BY Category;
SELECT COUNT(*) AS Total FROM dbo.FormatTestCases;
GO
