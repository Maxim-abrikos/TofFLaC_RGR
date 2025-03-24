using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Compiler
{
    internal static class LeksAnalisation
    {
        private static readonly HashSet<string> _variables = new HashSet<string>();


        public static int GetIndexAfterSecondWordRegex(string input)
        {
            Match match = Regex.Match(input, @"^(\S+\s+){2}");

            if (!match.Success)
            {
                return -1; // Не найдено два слова.
            }

            // Возвращаем индекс, где заканчивается совпадение (то есть, после второго слова и разделителя).
            return match.Length;
        }

        public static List<string> Analyze(TextEditor TE)
        {
            var result = new List<string>();

            for (int i = 0; i < TE.LineCount; i++)
            {
                var line = TE.Document.GetText(TE.Document.GetLineByNumber(i + 1)).Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                var lineErrors = new List<string>();

                // Проверка окончания строки
                if (!line.EndsWith(";"))
                {
                    var errorPosition = line.Length;
                    lineErrors.Add($"Ошибка синтаксиса окончания строки: строка должна заканчиваться символом ';'. Позиция: {errorPosition}");
                }
                else
                {
                    line = line[..^1].Trim(); // Убираем точку с запятой
                }

                // Если символ '=' отсутствует, добавляем его
                if (!line.Contains("="))
                {
                    lineErrors.Add($"Ошибка присваивания: отсутствует символ '='. Позиция: {GetIndexAfterSecondWordRegex(line)}");
                    line = line.Insert(GetIndexAfterSecondWordRegex(line), "="); // Добавляем '=' в строку
                }

                // Разделяем строку на левую и правую части
                var parts = line.Split(new[] { '=' }, 2);
                var leftPart = parts[0].Trim();
                var rightPart = parts[1].Trim();

                // Проверка левой части
                var leftErrors = CheckLeftPart(leftPart, out var variableName, out var leftError, out var leftErrorPosition);
                if (leftErrors.Count > 0)
                    lineErrors.AddRange(leftErrors);

                // Проверка правой части
                var rightErrors = CheckRightPart(rightPart, leftPart.Length + 1, out var rightError);
                if (rightErrors.Count > 0)
                    lineErrors.AddRange(rightErrors);

                // Добавляем результат для текущей строки
                if (lineErrors.Count > 0)
                {
                    result.Add($"Строка {i + 1}:\n");
                    result.AddRange(lineErrors);
                }
                else
                {
                    result.Add($"Строка {i + 1}: ошибок нет");
                }
            }
            return result;
        }

        //public static List<string> Analyze(TextEditor TE)
        //{
        //    var result = new List<string>();

        //    for (int i = 0; i < TE.LineCount; i++)
        //    {
        //        var line = TE.Document.GetText(TE.Document.GetLineByNumber(i + 1)).Trim();
        //        if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
        //            continue;

        //        var lineErrors = new List<string>();

        //        if (!line.EndsWith(";"))
        //        {
        //            var errorPosition = line.Length;
        //            lineErrors.Add($"Ошибка синтаксиса окончания строки: строка должна заканчиваться символом ';'. Позиция: {errorPosition}");
        //        }
        //        else
        //        {
        //            line = line[..^1].Trim();
        //        }

        //        var parts = line.Split(new[] { '=' }, 2);
        //        if (parts.Length != 2)
        //        {
        //            var errorPosition = line.Length;
        //            lineErrors.Add($"Ошибка присваивания: отсутствует символ '='. Позиция: {GetIndexAfterSecondWordRegex(line)}");

        //        }
        //        else
        //        {
        //            var leftPart = parts[0].Trim();
        //            var rightPart = parts[1].Trim();
        //            var Sus = CheckLeftPart(leftPart, out var variableName, out var leftError, out var leftErrorPosition);
        //            if (Sus.Count > 0)
        //                lineErrors.AddRange(Sus);

        //            var Bepis = CheckRightPart(rightPart, parts[0].Length + 1, out var rightError);
        //            if (Bepis.Count > 0)
        //                lineErrors.AddRange(Bepis);
        //        }

        //        if (lineErrors.Count > 0)
        //        {
        //            result.Add($"Строка {i + 1}:\n");
        //            result.AddRange(lineErrors);
        //        }
        //        else
        //        {
        //            result.Add($"Строка {i + 1}: ошибок нет");
        //        }
        //    }

        //    return result;
        //}

        private static List<Token> TokenizeLine(string line)
        {
            var tokens = new List<Token>();
            var buffer = "";
            var startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (char.IsWhiteSpace(ch))
                {
                    if (!string.IsNullOrEmpty(buffer))
                    {
                        tokens.Add(new Token(buffer, startIndex, i - 1));
                        buffer = "";
                    }
                    tokens.Add(new Token(ch.ToString(), i, i));
                    startIndex = i + 1;
                }
                else if (IsSpecialSymbol(ch))
                {
                    if (!string.IsNullOrEmpty(buffer))
                    {
                        tokens.Add(new Token(buffer, startIndex, i - 1));
                        buffer = "";
                    }
                    tokens.Add(new Token(ch.ToString(), i, i));
                    startIndex = i + 1;
                }
                else
                {
                    buffer += ch;
                }
            }

            if (!string.IsNullOrEmpty(buffer))
            {
                tokens.Add(new Token(buffer, startIndex, line.Length - 1));
            }

            return tokens;
        }

        private static bool IsSpecialSymbol(char ch)
        {
            return ch == '=' || ch == '(' || ch == ')' || ch == ',' || ch == ';';
        }


        private static List<string> CheckLeftPart(string leftPart, out string variableName, out string error, out int errorPosition)
        {
            variableName = null;
            error = null;
            errorPosition = -1;
            List <string> ToReturn = new List<string>();
            var parts = leftPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "ожидается формат 'Complex <имя_переменной>'";
                errorPosition = leftPart.IndexOf(leftPart, StringComparison.Ordinal);
                ToReturn.Add(error + " " + "Позиция: " + errorPosition);
                //return false;
            }

            if (parts[0] != "Complex")
            {
                error = "Ключевое слово 'Complex' отсутствует или указано неверно";
                errorPosition = leftPart.IndexOf(parts[0], StringComparison.Ordinal);
                ToReturn.Add(error + " " + "Позиция: " + errorPosition);
            }

            if (!IsValidVariableName(parts[1]))
            {
                error = $"Недопустимое имя переменной: '{parts[1]}'";
                errorPosition = leftPart.IndexOf(parts[1], StringComparison.Ordinal);
                ToReturn.Add(error + " " + "Позиция: "+errorPosition);
            }

            variableName = parts[1];
            return ToReturn;
        }


        
        private static List<string> CheckRightPart(string rightPart, int rightPartStartOffset, out string error)
        {
            error = null;
            List<string> ToReturn = new List<string>();
            int errorPosition = -1;

            // Сохраняем оригинальную строку
            string originalRightPart = rightPart;

            // Удаляем незначащие пробелы для упрощения проверки
            rightPart = Regex.Replace(rightPart, @"\s+", " ");
            rightPart = rightPart.Trim();

            // Проверка наличия ключевого слова "new"
            var newMatch = Regex.Match(rightPart, @"^(new) ");
            if (!newMatch.Success)
            {
                error = "Ключевое слово 'new' отсутствует или указано неверно.";
                errorPosition = originalRightPart.IndexOf("new", StringComparison.Ordinal);
                if (errorPosition == -1)
                {
                    errorPosition = 0;
                }
                errorPosition += rightPartStartOffset;
                ToReturn.Add(error + " Позиция: " + errorPosition);
            }

            // Проверка наличия ключевого слова "Complex"
            var complexMatch = Regex.Match(rightPart, @"(Complex)");
            if (!complexMatch.Success)
            {
                error = "Ключевое слово 'Complex' отсутствует или указано неверно.";
                errorPosition = originalRightPart.IndexOf("Complex", StringComparison.Ordinal);
                if (errorPosition == -1)
                {
                    // Если "Complex" не найден, указываем позицию после "new"
                    errorPosition = originalRightPart.IndexOf("new", StringComparison.Ordinal) + "new".Length + 1;
                }
                errorPosition += rightPartStartOffset;
                ToReturn.Add(error + " Позиция: " + (errorPosition +1));
            }

            // Поиск открывающей скобки
            var openingParenthesisMatch = Regex.Match(rightPart, @"\(");
            if (!openingParenthesisMatch.Success)
            {
                error = "Открывающая скобка '(' отсутствует.";
                errorPosition = originalRightPart.IndexOf("Complex", StringComparison.Ordinal) + "Complex".Length;
                errorPosition += rightPartStartOffset;
                ToReturn.Add(error + " Позиция: " + errorPosition);
            }

            // Поиск закрывающей скобки
            var closingParenthesisMatch = Regex.Match(rightPart, @"\)");
            if (!closingParenthesisMatch.Success)
            {
                error = "Закрывающая скобка ')' отсутствует.";
                errorPosition = originalRightPart.Length; // Конец строки
                errorPosition += rightPartStartOffset;
                ToReturn.Add(error + " Позиция: " + errorPosition);
            }

            // Поиск запятой
            var commaMatch = Regex.Match(rightPart, @",");
            if (!commaMatch.Success)
            {
                error = "Разделитель ',' отсутствует.";
                errorPosition = originalRightPart.IndexOf(")", StringComparison.Ordinal);
                if (errorPosition > 0)
                {
                    errorPosition -= 1;
                }
                errorPosition += rightPartStartOffset;
                ToReturn.Add(error + " Позиция: " + errorPosition);
            }
            Regex numberRegex = new Regex(@"\((?<first>[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?),(?<second>[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?)\)");
            Match numberMatch = numberRegex.Match(originalRightPart);

            if (!numberMatch.Success)
            {
                // Если структура нарушена, пытаемся извлечь числа вручную
                int commaIndex = originalRightPart.IndexOf(",", StringComparison.Ordinal);
                int closingBracketIndex = originalRightPart.IndexOf(")", StringComparison.Ordinal);

                if (commaIndex != -1 && closingBracketIndex != -1)
                {
                    string firstNum = originalRightPart.Substring(openingParenthesisMatch.Index + 1, commaIndex - (openingParenthesisMatch.Index + 1)).Trim();
                    string secondNum = originalRightPart.Substring(commaIndex + 1, closingBracketIndex - (commaIndex + 1)).Trim();

                    // Проверка формата первого числа
                    if (!Regex.IsMatch(firstNum, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?$"))
                    {
                        error = "Первый аргумент конструктора имеет неверный формат.";
                        errorPosition = openingParenthesisMatch.Index + 1 + rightPartStartOffset;
                        ToReturn.Add(error + " Позиция: " + errorPosition);
                    }

                    // Проверка формата второго числа
                    if (!Regex.IsMatch(secondNum, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?$"))
                    {
                        error = "Второй аргумент конструктора имеет неверный формат.";
                        errorPosition = commaIndex + 1 + rightPartStartOffset;
                        ToReturn.Add(error + " Позиция: " + errorPosition);
                    }
                }
            }
            else
            {
                // Если структура корректна, проверяем числа
                string firstNum = numberMatch.Groups["first"].Value.Trim();
                string secondNum = numberMatch.Groups["second"].Value.Trim();

                // Проверка формата первого числа
                if (!Regex.IsMatch(firstNum, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?$"))
                {
                    error = "Первое число имеет неверный формат.";
                    errorPosition = numberMatch.Groups["first"].Index + rightPartStartOffset;
                    ToReturn.Add(error + " Позиция: " + errorPosition);
                }

                // Проверка формата второго числа
                if (!Regex.IsMatch(secondNum, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?f?$"))
                {
                    error = "Второе число имеет неверный формат.";
                    errorPosition = numberMatch.Groups["second"].Index + rightPartStartOffset;
                    ToReturn.Add(error + " Позиция: " + errorPosition);
                }
            }

            return ToReturn;
        }

        private static bool IsValidVariableName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private static bool IsValidNumber(string number)
        {
            return Regex.IsMatch(number, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?$");
        }
    }
}
