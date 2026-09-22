# Лабораторна робота №1 — Модульне тестування ПЗ (варіант 21)

Курс «Автоматизовані системи тестування програмного продукту».

**Завдання (варіант 21):** написати модульні тести з максимальним покриттям коду для методу
`AnalaizerClass.Format()` бібліотеки AnalaizerClassLibrary програми «Калькулятор».
Тестові дані мають зберігатися в базі даних.

## Структура

| Шлях | Що це |
|---|---|
| `AnalizerClassLibrary/AnalaizerClass.cs` | Код, що тестується (метод `Format()`, рядки 156–267). Не змінювався. |
| `AnalizerFormatTests/FormatTests.cs` | **Модульні тести** (MSTest). |
| `AnalizerFormatTests/AnalizerFormatTests.csproj` | Тестовий проєкт (.NET Framework 4.7.2). |
| `Database/CreateCalculatorTestDB.sql` | **Скрипт бази даних** `CalculatorTestDB` з 55 тестовими випадками. |
| `CalcClassBr/`, `ErrorLibrary/`, `GraphInterface/` | Інші частини калькулятора (з вихідного архіву). |

## База даних

- `dbo.ErrorCodes` — довідник кодів помилок, які може повернути `Format()` (1, 2, 3, 4, 5, 7).
- `dbo.FormatTestCases` — тестові випадки: вхідний вираз і **або** очікуваний результат,
  **або** код помилки та її позиція (обмеження `CK_FormatTestCases_Expected`).

Текст помилки в базі не зберігається. Тест будує очікуване повідомлення з констант `ErrorsExpression`.

| Категорія | Що перевіряє | К-сть |
|---|---|---|
| Valid | коректні вирази, видалення пробілів | 12 |
| Empty | порожній вираз / лише пробіли | 2 |
| Length | межа довжини 65536 / 65537 (Error 07) | 4 |
| UnknownSymbol | невідомі символи (Error 02 at &lt;i&gt;) | 9 |
| BadStart | невірний початок (Error 03) | 5 |
| BadEnd | незавершений вираз (Error 05) | 4 |
| AfterDigit | `(`, `m`, `p` після цифри (Error 01 at &lt;i&gt;) | 3 |
| TwoOperators | два оператори підряд (Error 04 at &lt;i&gt;) | 3 |
| Syntax | інші невірні конструкції (Error 03) | 13 |

## Тести

- `Format_DataDriven_FromDatabase` — data-driven тест (`[DataSource]`), виконується для кожного рядка `FormatTestCases`.
- `Format_RemovesSpacesFromExpressionField` — після виклику в полі `expression` немає пробілів.
- `Format_ErrorResult_StartsWithAmpersand` — помилка повертається з префіксом `&`.
- `Format_NullExpression_ThrowsNullReferenceException` — поведінка для `expression = null`.

**Результат:** пройшли всі 58 перевірок із 58. Покриття `Format()`: 98,7 % рядків (78/79) і 98,7 % гілок (77/78).
Непокритий рядок 260 недосяжний: вираз, що закінчується унарним оператором, відсікається раніше перевіркою кінця рядка.

## Як запустити

1. Створити базу: відкрити `Database/CreateCalculatorTestDB.sql` у SSMS або Visual Studio
   і виконати на сервері `(localdb)\MSSQLLocalDB`. Можна також з командного рядка:
   `sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\CreateCalculatorTestDB.sql`.
2. Відкрити `Calculator_Exam_CommandProject.sln` у Visual Studio 2022.
3. **Test → Test Explorer → Run All**.

Покриття коду з командного рядка:

```
dotnet test AnalizerFormatTests --collect "Code Coverage;Format=Cobertura"
```
