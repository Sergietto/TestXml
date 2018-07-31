using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.IO;
using static System.Console;

namespace TestXml
{
    class Program
    {
        static string[] xmlFiles;       // Массив, хранящий полные пути к файлам, включая расширения
        static List<Calculation> calculations = new List<Calculation>();            //  Список успешно десериализованных объектов Calculation
        static Dictionary<string, int> serializedFiles = new Dictionary<string, int>();     // Словарь из пар "имя_файла - количество сериализованных Calculation-ов"

        static void Main(string[] args)
        {
            // Про Stopwatch узнал от MSDN, собственно, весь код оттуда, я даже комментарии тереть не стал, хотя там всё и логично)
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            string path = string.Empty;
            if (args.Length > 0)
                path = args[0];
            else
            {
                path = @"C:\Users\Петя\Desktop\Calculations\";
                /*
                WriteLine("Не указан путь к директории, дальнейшая работа невозможна!");
                ReadKey();
                return;
                */
            }
            
            // Получаем все пути до xml-файлов в директории path
            try
            {
                xmlFiles = Directory.GetFiles(path, "*.xml");
            }
            catch (Exception e)
            {
                WriteLine($"Какой-то непутёвый путь. Подробности: {e.Message}");
                ReadKey();
                return;
            }

            if (xmlFiles.Length == 0)
            {
                WriteLine("По указанному пути не найдено ни одного xml файла!");
                ReadKey();
                return;
            }

            // Считываем все xml-ы и считаем всякое
            foreach (string file in xmlFiles)
            {
                GetFromXml(file);
                calculations.Clear();
            }

            // Получение файла/ов с максимальным количеством сериализованных объектов Calculation
            WriteLine("----MAX----\n");
            int maxValue = serializedFiles.Values.Max();
            var maxPairs = serializedFiles.Where(pairs => pairs.Value == maxValue).ToList();

            WriteLine($"Файл[ы], с наибольшим количеством сериализованных Calculation-ов:");
            foreach (KeyValuePair<string, int> kv in maxPairs)
            {
                WriteLine($"Файл: {kv.Key}\nСериализовано: {kv.Value}\n");
            }
            WriteLine("----MAX----");

            stopWatch.Stop();

            
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            WriteLine($"Время работы приложения: {elapsedTime}");
            
            ReadKey();
        }

        /// <summary>
        /// Последовательно подсчитывает распарсенные из Xml данные.
        /// </summary>
        /// <returns>Возвращает результат вычислений.</returns>
        private static int Calculate()
        {
            int result = 0;
            for (int i = 0; i < calculations.Count; i++)
            {
                int secondOperand = (int)calculations[i].mod;

                switch (calculations[i].operand)
                {
                    case Operand.add:
                        result += secondOperand;
                        break;
                    case Operand.multiply:
                        result *= secondOperand;
                        break;
                    case Operand.divide:
                        result = (secondOperand == 0) ? 0 : result / secondOperand;     // Кривенькая защита от деления на ноль без предупреждений и опознавательных знаков
                        break;
                    case Operand.subtract:
                        result -= secondOperand;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Парсит Xml-документ
        /// </summary>
        /// <param name="path">Путь к Xml-документу</param>
        private static void GetFromXml(string path)
        {
            XmlDocument xDoc;
            try
            {
                xDoc = new XmlDocument();
                xDoc.Load(path);
            }
            catch (Exception e)
            {
                WriteLine($"С файлом {path} что-то пошло не так: {e.Message}\n");
                return;
            }
            
            XmlElement xRoot = xDoc.DocumentElement;    // Корень документа, если я правильно понимаю, то это узел Calculations, хотя не то что бы я особо понимаю)
            int countOfNormalCalc = 0;

            // Перебор всех элементов <calculation>
            foreach (XmlNode xnode in xRoot)
            {
                Calculation calculation = new Calculation();
                bool valueIsOk = true;        // На случай, если ошибки будут исключительно в значениях
                
                // Перебор узлов элемента <calculation> и парсинг их в шарпные значения
                foreach (XmlNode childnode in xnode.ChildNodes)
                {
                    // Последующие проверки исключительно на значения, если, например, в атрибуте value будет допущена очепятка, то программа сбоя не почувствует, а элегантного решения этой проблемы я пока не придумал(
                    if (childnode.Attributes.GetNamedItem("name").Value == "uid")
                    {
                        // Получаем строку, потом проверяем её на непустость. Так как в задании я не помню ничего за какой-либо контроль uid, то больше проверок, кроме как на "а не пусто ли" я не придумал.
                        string tempUid = childnode.Attributes.GetNamedItem("value").Value;
                        if (!string.IsNullOrEmpty(tempUid))
                            calculation.uid = tempUid;
                        else
                        {
                            WriteLine($"Файл: {path}\nПричина сбоя: отстутствует uid.\n");
                            valueIsOk = false;
                            break;
                        }
                    }

                    if (childnode.Attributes.GetNamedItem("name").Value == "operand")
                    {
                        if (Enum.TryParse(childnode.Attributes.GetNamedItem("value").Value, out Operand operand))
                            calculation.operand = operand;


                        if (calculation.operand == Operand.Unknown)
                        {
                            WriteLine($"Файл: {path}\nUid: {calculation.uid}\nПричина сбоя: сбой в определении оператора. Убедитесь, что элемент с атрибутом name равным \"operand\" имеет одно из допустимых значений.\n");
                            valueIsOk = false;
                            break;
                        }
                    }

                    if (childnode.Attributes.GetNamedItem("name").Value == "mod")
                    {
                        // Та тут тоже, что и везде - получение и парсинг, если возможно.
                        string tempModStringValue = childnode.Attributes.GetNamedItem("value").Value;
                        int tempModValue;
                        if (int.TryParse(tempModStringValue, out tempModValue))
                        {
                            calculation.mod = tempModValue;
                        }
                        else
                        {
                            WriteLine($"Файл: {path}\nUid: {calculation.uid}\nПричина сбоя: неверное числовое значение. Убедитесь, что элемент с атрибутом name равным \"mod\" является числом.\n");
                            valueIsOk = false;
                            break;
                        }

                    }
                }

                if (valueIsOk) {
                    // Проверки на случай, если проверки выше не были затронуты из-за отсутствия нужных атрибутов
                    if (string.IsNullOrEmpty(calculation.uid))
                    {
                        WriteLine($"Файл: {path}\nПричина сбоя: отстутствует uid. Убедитесь, что элемент с атрибутом name равным \"uid\" существует и не содержит синтаксических ошибок.\n");
                        continue;
                    }

                    if (calculation.operand == Operand.Unknown)
                    {
                        WriteLine($"Файл: {path}\nUid: {calculation.uid}\nПричина сбоя: сбой в определении оператора. Убедитесь, что элемент с атрибутом name равным \"operand\" существует и не содержит синтаксических ошибок.\n");
                        continue;
                    }

                    if (calculation.mod == null)
                    {
                        WriteLine($"Файл: {path}\nUid: {calculation.uid}\nПричина сбоя: сбой в определении оператора. Убедитесь, что элемент с атрибутом name равным \"mod\" существует и не содержит синтаксических ошибок.\n");
                        continue;
                    }

                    calculations.Add(calculation);
                    countOfNormalCalc++;
                }
            }
            serializedFiles.Add(path, countOfNormalCalc);
            
            WriteLine($"Файл: {Path.GetFileName(path)}\nРезультат: {Calculate()}\n");
        }
    }
}
