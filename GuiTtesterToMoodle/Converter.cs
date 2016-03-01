using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows;
using System.Threading;

namespace GuiTtesterToMoodle
{
    class Converter
    {


       public static string BodyConverter(string[] ttesterFiles, System.Windows.Controls.ProgressBar ProgBar, bool CheckIsOneFile)

        {           
            //возвращает сообщение с логом выполнения функции
            string Result = "";

            //если не создана папка для сохранения в my documents, создаём её
            string saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Ttester To Moodle Converter\\";

            if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);
            //Создаем логфайл
            StreamWriter Logfile = new StreamWriter(saveFolder + "LogErrors.txt", false, System.Text.Encoding.Default);

            //массив путей .txt файлов
            if (ttesterFiles.Length == 0) { Console.WriteLine("Нет файлов для обработки"); Logfile.WriteLine("Нет файлов для обработки"); }

            //создаем xml-документ, главный xml-тэг и список остальных тэгов
            XDocument XMLFile = new XDocument();
            XElement quiz = new XElement("quiz");
            List<XElement> quizChildrens = new List<XElement>();

            //конвертируем каждый xml-файл
            foreach (var ttesterFileName in ttesterFiles)
            {
                //какая-то магия для прогрессбара
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate { ProgBar.Value += 1; }));
                

                //создаем список строк из исходного ttester-файла
                StreamReader ttesterTxt = new StreamReader(ttesterFileName, System.Text.Encoding.Default);
                List<string> listOfLines = textToList(ttesterTxt);
                ttesterTxt.Close();
               
                //создаем элемент класса с содержанием теста
                TtesterClass TestInfo = new TtesterClass();


                bool flagTheme = false;
                bool flagThemeEnds = false;

                bool flagQuestion = false;

                bool flagTitle = false;
                bool flagTitleEnds = false;

                //разбираем каждую строчку
                for (int i = 0; i < listOfLines.Count; i++)
                {
                    //разбираем ##THEMES
                    if (flagThemeEnds == false)
                    {
                        if (String.IsNullOrWhiteSpace(listOfLines[i]) & flagTheme == true) { flagTheme = false; flagThemeEnds = true; }     //относятся к темам
                        if (flagTheme == true) TestInfo.themes.Add(listOfLines[i]);                                                         //относятся к темам
                        if (listOfLines[i].Contains("###THEMES###")) flagTheme = true;                                                      //относятся к темам
                    }

                    //разбираем ###TITLE###
                    if (flagTitleEnds == false)
                    {
                        if (String.IsNullOrWhiteSpace(listOfLines[i]) & flagTitle == true) { flagTitle = false; flagTitleEnds = true; }      
                        if (flagTitle == true) TestInfo.title = System.Text.RegularExpressions.Regex.Replace(listOfLines[i], @"[^\w\.\s@-]", "");   //чистим TITLE непонятно от чего                                               //относятся к темам
                        if (listOfLines[i].Contains("###TITLE###")) flagTitle = true;
                    }


                    //разбираем вопросы(все вопросы начинаются с темы вопроса)
                    if (listOfLines[i].Contains("##theme ") || flagQuestion == true)
                    {
                        if (String.IsNullOrWhiteSpace(listOfLines[i])) { flagQuestion = false; }

                        flagQuestion = true;

                        //создаем элемент класса "Вопрос" и записываем тему вопроса
                        if (listOfLines[i].Contains("##theme "))
                        {
                            TestInfo.questions.Add(new QuestionClass());
                            TestInfo.questions[TestInfo.questions.Count - 1].theme = int.Parse(listOfLines[i].Substring(8));
                        }

                        //записываем вес вопроса
                        if (listOfLines[i].Contains("##score ")) TestInfo.questions[TestInfo.questions.Count - 1].score = listOfLines[i].Substring(8);

                        //записываем тип вопроса
                        if (listOfLines[i].Contains("##type ")) TestInfo.questions[TestInfo.questions.Count - 1].type = listOfLines[i].Substring(7);

                        //время, отведенное на вопрос (в мудле этого нет) - не используется
                        //выдираем вопросы
                        if (listOfLines[i].Contains("##time "))
                        {
                            TestInfo.questions[TestInfo.questions.Count - 1].time = listOfLines[i].Substring(7);
                            TestInfo.questions[TestInfo.questions.Count - 1].question = listOfLines[i + 1].Substring(0);
                        }
                        //записываем ответы
                        if (!String.IsNullOrWhiteSpace(listOfLines[i]) && TestInfo.questions[TestInfo.questions.Count - 1].question != null && (listOfLines[i].Substring(0, 1) == "+" || listOfLines[i].Substring(0, 1) == "-" || listOfLines[i].Substring(0, 1) == "*")) TestInfo.questions[TestInfo.questions.Count - 1].answers.Add(listOfLines[i]);

                    }

                }

                //записываем лог ошибок
                if (TestInfo.title == null) { Result += "Отсутсвует название теста (TITLE): " + ttesterFileName + "\r\n"; Console.WriteLine("Отсутсвует название теста (TITLE): " + ttesterFileName); Logfile.WriteLine("Отсутсвует название теста (TITLE): " + ttesterFileName); }
                if (TestInfo.themes.Count == 0) { Result += "Отсутсвует Тема для тестов (THEMES): " + ttesterFileName+ "\r\n";  Console.WriteLine("Отсутсвует Тема для тестов (THEMES): " + ttesterFileName); Logfile.WriteLine("Отсутсвует Тема для тестов (THEMES): " + ttesterFileName); }
                if (TestInfo.questions.Count == 0) { Result += "Отсутсвуют вопросы для обработки в тесте: " + ttesterFileName + "\r\n"; Console.WriteLine("Отсутсвуют вопросы для обработки в тесте: " + ttesterFileName); Logfile.WriteLine("Отсутсвуют вопросы для обработки в тесте: " + ttesterFileName); }


                //сортируем экземпляр класса теста по темам(по возрастанию)
                TestInfo.questions.Sort(delegate (QuestionClass q1, QuestionClass q2) {
                    return q1.theme.CompareTo(q2.theme);
                });


                //пробегаемся по всем вопросам и, в зависимости от типа вопроса, вызываем необходимую функцию генерации xml-элемента

                int FlagTheme = 0;
                bool flagtemp = false;

                foreach (var foreachQuestion in TestInfo.questions)
                {
                   // try
                  //  {
                        if (FlagTheme != foreachQuestion.theme) { FlagTheme = foreachQuestion.theme; quizChildrens.Add(questionCategory(TestInfo.themes[foreachQuestion.theme - 1], CheckIsOneFile, TestInfo.title)); }

                        switch (foreachQuestion.type)
                        {
                            case "1":
                                //quizChildrens.Add(multiQuestion(foreachQuestion.type, foreachQuestion.score, foreachQuestion.question, foreachQuestion.answers));
                                quizChildrens.Add(singleQuestion(foreachQuestion.score, foreachQuestion.question, foreachQuestion.answers));
                                break;

                            case "2":
                                quizChildrens.Add(multiQuestion(foreachQuestion.score, foreachQuestion.question, foreachQuestion.answers));
                                break;

                            case "3":
                                quizChildrens.Add(openQuestion(foreachQuestion.score, foreachQuestion.question, foreachQuestion.answers));
                                break;

                            case "4":
                                quizChildrens.Add(crossQuestion(foreachQuestion.score, foreachQuestion.question, foreachQuestion.answers));
                                break;

                            default:

                                if (flagtemp == false) { Result += "Вопросы в которые не вошли в тест " + TestInfo.title + "\r\n"; Console.WriteLine("Вопросы в которые не вошли в тест " + TestInfo.title + ":"); flagtemp = true; Logfile.WriteLine("Вопросы в которые не вошли в тест ####" + TestInfo.title + "####:"); }

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(foreachQuestion.question);
                                Logfile.WriteLine("--- " + foreachQuestion.question);
                                Console.ResetColor();
                                break;
                     //   }

                    }
                /*    catch (Exception)
                    {
                        Result += "\r\nПри составлении теста возникли ошибки!\r\n";
                       Console.WriteLine("При составлении теста возникли ошибки!");
                    }
                    */
                }

                //если нужно сохранить тесты в разные файлы
                    if (CheckIsOneFile == false)
                    {

                    try
                    {
                        quiz.Add(quizChildrens);
                        XMLFile.Add(quiz);
                        //проверяем, создана ли папка, если нет - создаем
                        //добавляем основной xml-узел, сохраняем файл с именем TITLE теста
                        if (Directory.Exists(saveFolder + "Moodle")) { XMLFile.Save(saveFolder + "Moodle\\" + TestInfo.title + ".xml"); }
                        else { Directory.CreateDirectory(saveFolder + "Moodle"); XMLFile.Save(saveFolder + "Moodle\\" + TestInfo.title + ".xml"); }

                        //очистка quizChildrens
                        foreach (var item in quizChildrens) item.RemoveAll();
                        quizChildrens.Clear();
                        //и полностью чистим оставшиеся xml-узлы
                        quiz.RemoveAll();
                        XMLFile.RemoveNodes();
                    }
                    catch (ArgumentException e)
                    {
                        MessageBox.Show("Ошибка в тесте." + TestInfo.title + " Программа завершила свою работу в аварийном режиме! \r\n Техническая информация " + e.Message);
                    }

                    finally
                    {
                        //очистка quizChildrens
                        foreach (var item in quizChildrens) item.RemoveAll();
                        quizChildrens.Clear();
                        //и полностью чистим оставшиеся xml-узлы
                        quiz.RemoveAll();
                        XMLFile.RemoveNodes();
                    }

                        
                    }

                }
                
                //если сохраняем всё в один файл
                if (CheckIsOneFile == true)
                {

                        try
                        {
                            //добавляем узлы
                            quiz.Add(quizChildrens);
                            XMLFile.Add(quiz);

                            //проверяем, создана ли папка, если нет - создаем
                            //добавляем основной xml-узел, сохраняем файл в "Все тесты.xml"
                            if (Directory.Exists(saveFolder + "Moodle")) { XMLFile.Save(saveFolder + "Moodle\\Все тесты.xml"); }
                            else { Directory.CreateDirectory(saveFolder + "Moodle"); XMLFile.Save(saveFolder + "Moodle\\Все тесты.xml"); }

                            //очистка asd
                            foreach (var item in quizChildrens) item.RemoveAll();
                            quizChildrens.Clear();
                        }
                        catch (ArgumentException e)
                        {

                    MessageBox.Show("Ошибка в тесте. Программа завершила свою работу в аварийном режиме! \r\n Техническая информация " + e.Message);
                        }
                   
                }


            Logfile.Close();

            Result += "\r\n ########################## \r\n Обработка тестов завершина";
            return Result;
        }


        //формируем xelement категории
        public static XElement questionCategory(string theme, bool CheckIsOneFile, string Title)
        {
            
            XElement XQuestion = new XElement("question",
            new XAttribute("type", "category"),
            new XElement("category",
                new XElement("text", "$course$/Тест: " + Title + "/" + theme)));

            return XQuestion;
        }

        //одиночный вопрос
        public static XElement singleQuestion(string score, string question, List<string> answers, string time = "0")
        {
            XElement XQuestion = new XElement("question",
                new XAttribute("type", "multichoice"),
                new XElement("name",
                    new XElement("text", "Выбрать один вариант ответа")),
                new XElement("questiontext",
                    new XAttribute("format", "html"),
                    new XElement("text", question)),
                new XElement("generalfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "")),
                new XElement("defaultgrade", score),
                new XElement("penalty", "0"),
                new XElement("hidden", "0"),
                new XElement("single", "true"),
                new XElement("shuffleanswers", "true"),
                new XElement("answernumbering", "abc"),
                new XElement("correctfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "Ваш ответ верный.")),
                new XElement("partiallycorrectfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "Ваш ответ частично правильный.")),
                new XElement("incorrectfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "Ваш ответ неправильный.")),
                new XElement("shownumcorrect")
                );

            foreach (var item in answers)
            {
                int fraction = 0;
                if (item.Substring(0, 1) == "+") { fraction = 100; }
                
                XElement Xanswer = new XElement("answer",
                    new XAttribute("fraction", fraction),
                    new XAttribute("format", "html"),

                    new XElement("text", item.Substring(5)),
                    new XElement("feedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "")
                    ));
                XQuestion.Add(Xanswer);
            }


            return XQuestion;
        }

        //множественный вопрос multichoice
        public static XElement multiQuestion(string score, string question, List<string> answers, string time = "0")
        {

            XElement XQuestion = new XElement("question",
                new XAttribute("type", "multichoiceset"),
                new XElement("name",
                    new XElement("text", "Выбрать несколько вариантов ответа")),
                new XElement("questiontext",
                    new XAttribute("format", "html"),
                    new XElement("text", question)),
                new XElement("generalfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "")),
                new XElement("defaultgrade", score),
                new XElement("penalty", "0"),
                new XElement("hidden", "0"),
                new XElement("shuffleanswers", "true"),
                new XElement("answernumbering", "abc"),
                new XElement("correctfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "Ваш ответ верный.")),
                new XElement("incorrectfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "Ваш ответ неправильный.")),
                new XElement("shownumcorrect")
                );


            //ответы на вопросы
            double ball = 0;
            double fraction = 0;

            
            int plus = 0;
            foreach (var item in answers) if (item.Substring(0, 1) == "+") { plus++; }
            
            ball = Math.Round(100F / plus, 7);
            

            foreach (var item in answers)
            {
                
                fraction = 0;
                if (item.Substring(0, 1) == "+") { fraction = ball; }
                
                XElement Xanswer = new XElement("answer",
                    new XAttribute("fraction", fraction),
                    new XAttribute("format", "html"),

                    new XElement("text", item.Substring(5)),
                    new XElement("feedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "")
                    ));
                XQuestion.Add(Xanswer);
            }
            return XQuestion;
        }

        //открытый вопрос
        public static XElement openQuestion(string score, string question, List<string> answers, string time = "0")
        {
            string answerText = "";
            foreach (var item in answers[0]) if (Char.IsLetter(item) && Char.IsLower(item)) answerText += item;

            XElement XQuestion = new XElement("question",
                new XAttribute("type", "shortanswer"),
                new XElement("name",
                    new XElement("text", "Краткий ответ")),
                new XElement("questiontext",
                    new XAttribute("format", "html"),
                    new XElement("text", question)),
                new XElement("generalfeedback",
                    new XAttribute("format", "html"),
                    new XElement("text", "")),
                new XElement("defaultgrade", score),
                new XElement("penalty", "0"),
                new XElement("hidden", "0"),
                new XElement("usecase", "0"),
                new XElement("answer",
                    new XAttribute("fraction", "100"),
                    new XAttribute("format", "moodle_auto_format"),
                    new XElement("text", answerText),
                    new XElement("feedback",
                        new XAttribute("format", "html"),
                        new XElement("text", ""))));

            return XQuestion;

        }

        //вопрос на соответствия
        public static XElement crossQuestion(string score, string question, List<string> answers, string time = "0")
        {
            XElement XQuestion = new XElement("question",
                    new XAttribute("type", "matching"),
                    new XElement("name",
                        new XElement("text", "Выберите соответсвия")),
                    new XElement("questiontext",
                        new XAttribute("format", "html"),
                        new XElement("text", question)),
                    new XElement("generalfeedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "")),
                    new XElement("defaultgrade", score),
                    new XElement("penalty", "0"),
                    new XElement("hidden", "0"),
                    new XElement("shuffleanswers", "true"),
                    new XElement("correctfeedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "Ваш ответ верный.")),
                    new XElement("partiallycorrectfeedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "Ваш ответ частично правильный.")),
                    new XElement("incorrectfeedback",
                        new XAttribute("format", "html"),
                        new XElement("text", "Ваш ответ неправильный.")),
                    new XElement("shownumcorrect")

                );

            List<List<string>> crossQuestions = new List<List<string>>();
            List<string> cQName = new List<string>();
            List<string> cQNumber = new List<string>();

            crossQuestions.Add(cQName);
            crossQuestions.Add(cQNumber);

            foreach (var item in answers)
            {
                if (item.Substring(6, 2) != "00")
                {
                    crossQuestions[0].Add(item.Substring(9));
                    crossQuestions[1].Add(item.Substring(6, 2));
                }
            }

            //ответы начало
            for (int i = 0; i < crossQuestions[0].Count; i++)
            {
                if (crossQuestions[1][i][0] == '0') crossQuestions[1][i] = crossQuestions[1][i].Substring(1);
                string textAnswer = answers[Int32.Parse(crossQuestions[1][i]) - 1].Substring(9);

                XElement Xsubquestion = new XElement("subquestion",
                    new XAttribute("format", "html"),
                    new XElement("text", crossQuestions[0][i]),
                        new XElement("answer",
                            new XElement("text", textAnswer)));

                XQuestion.Add(Xsubquestion);
            }
            //ответы конец

            return XQuestion;
        }

        //разбиваем текстовый файл в List<string>
        static List<string> textToList(StreamReader txtFile)
        {
            List<string> lines = new List<string>();
            string tempLine;
            while ((tempLine = txtFile.ReadLine()) != null) lines.Add(tempLine);
            
            return lines;
        }
    }


    //класс теста
    class TtesterClass
    {

        public string title;
        public List<string> themes = new List<string>();
        public List<QuestionClass> questions = new List<QuestionClass>();

    }

    //класс вопросов к тесту
    class QuestionClass
    {
        public int theme;
        public string score;
        public string type;
        public string time;
        public string question;
        public List<string> answers = new List<string>();
    }


}
