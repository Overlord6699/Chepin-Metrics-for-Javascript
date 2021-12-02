using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Метра_форма
{
    public partial class Form1 : Form
    {
        public int numOfMethods = 0;
        public int realSpenIndex = 2;
        public int strIndex = 0;
        public int spenSum = 0;    

        [Flags]
        public enum VaribleKind : byte
        {
            Паразитная = 0,
            Вводимая = 1,
            Модифицируемая = 3,
            Управляющая = 4,
            Используемая = 8,

            Выводимая = 20,
            Ввод = 40
        }

        public enum VariableIndex: byte
        {
            Вводимая = 1,
            Модифицируемая = 2,
            Управляющая = 3,
            Паразитная = 4
        }

        public struct Vareble
        {
            public string name;
            public VaribleKind kind;
            public int counter;
            public Vareble(string name, VaribleKind kind, int counter)
            {
                this.name = name;
                this.kind = kind;
                this.counter = counter;
            }
        }

        public  Vareble[] GlobalVaribles = new Vareble[0];
        public  Vareble[] LocalVaribles = new Vareble[0];
        public  string[] DescriptionWords = { "const", "{", "function", "type", "uses", "label", "let", "var" };
        public  string[] WordsOfEndOperator = { "do", "{", "else", "}" };
        public  int beginEndCounter = 0;   //если был найден begin, то +1; если end, то -1
        public  int totalP = 0, totalM = 0, totalC = 0, totalT = 0;
        public int totalOutP = 0, totalOutM = 0, totalOutC = 0, totalOutT = 0;
        public double ChepinValue = 0, OutChepinValue = 0;

        public  bool IsLetterOfIdent(char c)
        {
            //последние 2 условия некорректные
            return (('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9') || (c == '_') || (c == '{') || (c == '}'));
        }

        public  void DeleteLineComment(ref string str)
        {
            int startIndex = str.IndexOf("//");
            if (startIndex != -1)
                str = str.Remove(startIndex);
        }

        public void DeleteMultiLineComment(ref string str)
        /*вырезает многострочные комментарии*/
        {
            int startIndex;
            string commentType = "";
            if ((startIndex = str.IndexOf("/*")) != -1)
                commentType = "*\\";
            else if ((startIndex = str.IndexOf("(*")) != -1)
                commentType = "*)";

            if (startIndex != -1)
            {
                string tempStr = str;
                str = str.Remove(startIndex) + " ";
                while ((startIndex = tempStr.IndexOf(commentType)) == -1)
                {
                    tempStr = GetString();
                    DeleteLineComment(ref tempStr);
                }
                str = str + tempStr.Remove(0, startIndex + 1);
            }
        }

        public void DeleteTextFromString(ref string str)
        {
            int startInd = str.IndexOf('"', 0), endInd;
            while (startInd != -1)
            {
                endInd = str.IndexOf('"', startInd + 1);
                str = str.Remove(startInd, endInd - startInd + 1);
                startInd = str.IndexOf('"', startInd);
            }
        }

        public string GetString()
        /*возращает следующую строку кода на Pascal*/
        {
            if (strIndex >= Form1.strArr.Length)
                return null;

            string result = Form1.strArr[strIndex];
            if (result != null)
            {
                DeleteTextFromString(ref result);
                DeleteLineComment(ref result);
                DeleteMultiLineComment(ref result);

                result = result + ' ';
                strIndex++;
            }


            return result;
        }

        //движение по элементам строк
        public int IncPos(ref string str, ref int pos)
        {
            if (++pos >= str.Length)
            {
                str = GetString();
                pos = 0;
            }

            return pos;
        }

        public string FindNextIdentifier(ref string str, ref int pos, bool beginPos)
        {
            if (str != null)
            {
                //обработка не-идентификатора
                while (!IsLetterOfIdent(str[pos]))
                {
                    IncPos(ref str, ref pos);

                    if (str == null)    //если текст кода кончился
                        return null;
                }

                //обработка идентификатора
                int tempPos = pos;
                string ident = "";
                while (IsLetterOfIdent(str[pos]))
                {
                    //отделяем текст от {
                    if (ident.Length > 0 && str[pos] == '{')
                        break;

                    ident = ident + str[pos];
                    IncPos(ref str, ref pos);
                }

                if (beginPos)
                    pos = tempPos;

                return ident;
            }

            return null;
        }

        public char FindNextSymbol(ref string str, ref int pos)
        {
            while (str[pos] == ' ')
                IncPos(ref str, ref pos);

            char result = str[pos];
            IncPos(ref str, ref pos);

            return result;
        }

        /*определяет есть ли слово в массиве строк*/
        public bool FindWordInArray(ref string word, ref string[] arr)
        {
            bool result = false;

            for (int i = 0; i < arr.Length; i++)
                if (String.Compare(word, arr[i], true) == 0)
                    result = true;

            return result;
        }

        /*добавляет новый элемент в массив строк*/
        public void AddToVarArray(ref Vareble[] arr, string str)
        {
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = new Vareble(str, VaribleKind.Паразитная, 0);
        }

        /*выписывает все переменные, стоящие после ближайшего слова var, в массив VariblesArr*/
        public void FindVaribles(ref string str, ref int pos, ref Vareble[] varArray)
        {
            string ident;

            do
            {
                do
                {
                    ident = FindNextIdentifier(ref str, ref pos, false);

                    //либо говорим, что переменная уже используется, либо создаём новую
                    if (!DetermineVarible(ident, VaribleKind.Используемая, true))
                        AddToVarArray(ref varArray, ident);

                    /*
                    //для переменной после =
                    if(FindNextSymbol(ref str, ref pos) == '=')
                    {
                        ident = FindNextIdentifier(ref str, ref pos, true);
                        if (global) DetermineVaribleInArr(ref GlobalVaribles, ref ident, VaribleKind.Используемая);
                        else DetermineVaribleInArr(ref LocalVaribles, ref ident, VaribleKind.Используемая);
                    }
                    */

                } while (FindNextSymbol(ref str, ref pos) != ';');

                ident = FindNextIdentifier(ref str, ref pos, true);

                //пока не найден новый раздел описания и не закончится let
            } while (!FindWordInArray(ref ident, ref DescriptionWords) &&
            String.Compare(ident, "let", true) == 0);
        }

        public bool DetermineVaribleInArr(ref Vareble[] arr, ref string name, VaribleKind kind, in bool isIncreasing)
        /*инициализирует вид переменной с именем name в массиве arr*/
        {
            int i = 0;
            bool result = false;

            while ((i < arr.Length) && !result)
            {
                result = String.Compare(arr[i].name, name, true) == 0;
                if (result)
                {
                    arr[i].kind = arr[i].kind | kind;
                    if (isIncreasing)
                        arr[i].counter++;
                }

                i++;
            }
            return result;
        }

        /*инициализирует вид переменной с именем name*/
        public bool DetermineVarible(string name, VaribleKind kind, in bool isEncreasing = false, in bool ignoreGlobal = false)
        {
            bool result;

            if(ignoreGlobal)
            {
                result = DetermineVaribleInArr(ref LocalVaribles, ref name, kind, in isEncreasing);
            }
            else if (!(result = DetermineVaribleInArr(ref LocalVaribles, ref name, kind, in isEncreasing)))
                result = DetermineVaribleInArr(ref GlobalVaribles, ref name, kind, in isEncreasing);
            
            return result;
        }

        /*определяет назнaчение переменных в заголовке for*/
        public void AnalyzeOperatorFor(ref string str, ref int pos, in bool global)
        {
            bool addVarNextTime = false;
            bool isEnded = false;
            //FindNextIdentifier(ref str, ref pos, false);   //пропускаем слово "let"

            //string ident =  FindNextIdentifier(ref str, ref pos, false);   //находим переменную цикла
            //DetermineVarible(ident, VaribleKind.Используемая | VaribleKind.Модифицируемая);
            do
            {
                string ident = FindNextIdentifier(ref str, ref pos, false);
                //тут может быть ошибка

                if (addVarNextTime)
                {
                    if (global)
                        AddToVarArray(ref GlobalVaribles, ident);
                    else
                        AddToVarArray(ref LocalVaribles, ident);

                    DetermineVarible(ident, VaribleKind.Управляющая | VaribleKind.Используемая);

                    addVarNextTime = false;
                }

      
                if (ident == "let" && !addVarNextTime)
                    addVarNextTime = true;
                else
                    DetermineVarible(ident, VaribleKind.Управляющая, true); //для других переменных в цикле


                if (str.LastIndexOf(")") - pos < 5)
                {
                    isEnded = true;

                    pos = str.LastIndexOf(")");
                }

            } while (!isEnded);
        }

        /*определяет назначение переменных в подпрограммах*/
        public void AnalyzeMethod(ref string str, ref int pos, VaribleKind kind, ref bool global)
        {
            string ident;
            char symbol;

            if (str[pos] == '.')
            {
                ident = FindNextIdentifier(ref str, ref pos, false);

                AnalyzeOperator(ref str, ref pos, ref ident, ref global);

                return;
            }
            else
            do
            {
                //проверка параметров
                if (CheckMethodParameters(ref str, ref pos))
                {
                    ident = FindNextIdentifier(ref str, ref pos, false);
                    DetermineVarible(ident, kind, true);
                    do
                    {
                        symbol = FindNextSymbol(ref str, ref pos);
                    } while ((symbol != ';') && !IsLetterOfIdent(symbol));
                    if (IsLetterOfIdent(symbol))
                    {
                        pos--;
                        ident = FindNextIdentifier(ref str, ref pos, true);
                    }
                }
                else
                    return;

            } while (!(FindWordInArray(ref ident, ref WordsOfEndOperator) || symbol == ';'));
        }

        public void AnalyzeCase(ref string str, ref int pos)
        /*определяет назночение переменной в операторе case*/
        {
            //FindNextIdentifier(ref str, ref pos, false); //пропускаем слово case
            string ident = FindNextIdentifier(ref str, ref pos, false); //получаем условие case
            DetermineVarible(ident, VaribleKind.Используемая | VaribleKind.Управляющая, true);
            /*
            while (String.Compare(ident, "of", true) != 0)
                ident = FindNextIdentifier(ref str, ref pos, false);
            */
        }

        public void AnalyzeConditionalOperator(ref string str, ref int pos)
        {
            string ident;
            char symbol;
            VaribleKind kind = VaribleKind.Используемая | VaribleKind.Управляющая;

            do
            {
                ident = FindNextIdentifier(ref str, ref pos, false);
                DetermineVarible(ident, kind, true);

                do
                {
                    symbol = FindNextSymbol(ref str, ref pos);
                    if (symbol == '[') kind = VaribleKind.Используемая;
                    else if (symbol == ']') kind = VaribleKind.Вводимая | VaribleKind.Используемая;
                } while ((symbol != ';') && !IsLetterOfIdent(symbol));

                if (IsLetterOfIdent(symbol))
                {
                    pos--;
                    ident = FindNextIdentifier(ref str, ref pos, true);
                }

            } while (!(FindWordInArray(ref ident, ref WordsOfEndOperator) || symbol == ';'));
        }

        /*анализирует оператор присваивания*/
        public void AnalyzeAssignmentOperator(ref string str, ref int pos, ref string ident)
        {
            string firstVar = "";
            char symbol;
            bool random = false;

            if (str.IndexOf("=") != -1)
            {
                firstVar = FindNextIdentifier(ref str, ref pos, false); //переменная после оператора

                do
                {
                    //ident = FindNextIdentifier(ref str, ref pos, false);
                    if (String.Compare(ident, "random", true) == 0)
                        random = true;
                    if (String.Compare(ident, firstVar, true) != 0)
                        DetermineVarible(firstVar, VaribleKind.Используемая, true);
                    do
                    {
                        symbol = FindNextSymbol(ref str, ref pos);
                    } while ((symbol != ';') && !IsLetterOfIdent(symbol));

                    if (str.IndexOf("=") != -1)
                        if (IsLetterOfIdent(symbol))
                        {
                            pos--;
                            ident = FindNextIdentifier(ref str, ref pos, true);
                        }

                } while (!(FindWordInArray(ref ident, ref WordsOfEndOperator) || symbol == ';'));
            }

            if (random) DetermineVarible(firstVar, VaribleKind.Вводимая, true);
            else DetermineVarible(ident, VaribleKind.Модифицируемая| VaribleKind.Используемая, true);
        }

        /*определяет тип оператора*/
        public void AnalyzeOperator(ref string str, ref int pos, ref string ident, ref bool global)
        {
            //string ident = FindNextIdentifier(ref str, ref pos, true);

            if (ident == null)
                return;

            switch (ident.ToLower())
            {
                case "new":
                case "do":
                case "else":
                case "default":
                case "break":
                    break;
                case "for":
                    AnalyzeOperatorFor(ref str, ref pos, in global);
                    break;
                case "while":
                case "repeat":
                case "if":
                case "switch":
                    AnalyzeConditionalOperator(ref str, ref pos);//AnalyzeMethod(ref str, ref pos, VaribleKind.Управляющая | VaribleKind.Испоьзуемая);
                    break;
                case "case":
                    AnalyzeCase(ref str, ref pos);
                    break;
                case "{":
                    beginEndCounter++;
                    //FindNextIdentifier(ref str, ref pos, false);
                    break;
                case "}":
                    beginEndCounter--;


                    if (beginEndCounter == 0)
                    {
                        if (!global)
                        {
                            CreateOutputForm(ref LocalVaribles);
                        }

                        LocalVaribles = new Vareble[0];
                        global = true;
                    }


                    break;
                //вывод
                case "write":
                case "confirm":
                case "alert":
                    AnalyzeMethod(ref str, ref pos, VaribleKind.Выводимая, ref global);
                    break;
                //ввод
                case "prompt":
                    AnalyzeMethod(ref str, ref pos, VaribleKind.Ввод, ref global);
                    break;

                default: //сюда попадают переменные
                    if (DetermineVarible(ident, VaribleKind.Паразитная))
                        AnalyzeAssignmentOperator(ref str, ref pos, ref ident);  //оператор присваивания
                    else
                        AnalyzeMethod(ref str, ref pos, VaribleKind.Используемая, ref global);   //подпрограмма
                    break;
            }
        }

        public bool CheckMethodParameters(ref string str, ref int pos)
        {
            int temp = pos;
            bool hasParameters = true;

            FindNextSymbol(ref str, ref pos); //пропускаем '('

            if (FindNextSymbol(ref str, ref pos) == ')')
                hasParameters = false;

            pos = temp + 1;

            return hasParameters;
        }

        /*находит переменные в заголовке подпрограммы*/
        public  void AnalayzeMethodHead(ref string str, ref int pos)
        {
            //Console.WriteLine("\n{0}", FindNextIdentifier(ref str, ref pos, false));
            dataGridView1.Rows.Add(FindNextIdentifier(ref str, ref pos, false));
            numOfMethods++;

            string ident;
            char symbol = '0';

            do
            {
                //если параметр есть, то переменная вводится. АУФ
                VaribleKind kind = VaribleKind.Вводимая;

                do
                {
                    //есть параметры
                    if (CheckMethodParameters(ref str, ref pos))
                    {
                        ident = FindNextIdentifier(ref str, ref pos, false);

                        switch (ident)
                        {
                            /*
                            case "const": kind = VaribleKind.Вводимая; pos--; break;
                            case "var": kind = VaribleKind.Вводимая | VaribleKind.Используемая; pos--; break;
                            case "out": kind = VaribleKind.Используемая; pos--; break;
                            */
                            default:
                                AddToVarArray(ref LocalVaribles, ident);
                                DetermineVarible(ident, kind);
                                break;
                        }
                    }
                    else
                        return;
                } while (((symbol = FindNextSymbol(ref str, ref pos)) != ',') && (symbol != ')'));
                //while (FindNextSymbol(ref str, ref pos) != ':'); 

                kind = VaribleKind.Вводимая;

            } while (symbol != ')');    //пока не закончится описание переменных
        }

        public void PrintValuesForm(in int P, in int M,in int C,in int T,
            in double Res, in bool global, in bool isOutIdents = false)
        {
            int neededIndex, outputIndex, addIndex;

            if (global)
            {
                neededIndex = 3 + 5 * (numOfMethods);
                outputIndex = 4 + 5 * numOfMethods;
            }
            else
            {
                neededIndex = 3 + 5 * (numOfMethods - 1);
                outputIndex = 4 + 5 * (numOfMethods - 1);
            }

            if (isOutIdents)
                addIndex = 4;
            else
                addIndex = 0;


            if (P == 0)
                dataGridView1.Rows[neededIndex-1].Cells[
                    (int)VariableIndex.Вводимая+addIndex].Value = "--";
            if (M == 0)
                dataGridView1.Rows[neededIndex-1].Cells[
                    (int)VariableIndex.Модифицируемая+addIndex].Value = "--";
            if(C == 0)
                dataGridView1.Rows[neededIndex-1].Cells[
                    (int)VariableIndex.Управляющая+addIndex].Value = "--";
            if (T == 0)
                dataGridView1.Rows[neededIndex - 1].Cells[
                    (int)VariableIndex.Паразитная+addIndex].Value = "--";
                

            dataGridView1.Rows[neededIndex].Cells[
                (int)VariableIndex.Вводимая+addIndex].Value =
                "p = " + P.ToString();
            dataGridView1.Rows[neededIndex].Cells[
                (int)VariableIndex.Модифицируемая+addIndex].Value =
                "m = " + M.ToString();
            dataGridView1.Rows[neededIndex].Cells[
                (int)VariableIndex.Управляющая+addIndex].Value =
                "c = " + C.ToString();
            dataGridView1.Rows[neededIndex].Cells[
                (int)VariableIndex.Паразитная+ addIndex].Value =
                "t = " + T.ToString();


            dataGridView1.Rows[outputIndex].Cells[
                (int)VariableIndex.Вводимая+addIndex].Value
                = "Q="+P.ToString() + "+2";
            dataGridView1.Rows[outputIndex].Cells[
                (int)VariableIndex.Модифицируемая+ addIndex].Value
                = "*"+M.ToString()+ "+3*";
            dataGridView1.Rows[outputIndex].Cells[
                (int)VariableIndex.Управляющая+addIndex].Value
                = C.ToString() + "+0.5*";
            dataGridView1.Rows[outputIndex].Cells[
               (int)VariableIndex.Паразитная+addIndex].Value
                = T.ToString()+"="+Res.ToString();
        }

        public  void PrintValues(int P, int M, int C, int T, double Res)
        {
            Console.WriteLine("P = {0}, M = {1}, C = {2}, T = {3}", P, M, C, T);
            Console.WriteLine("Q = P + 2*M + 3*C + 0.5*T = {0}", Res);
        }

        public  void CalculateChepinValue(in Vareble[] arr, in bool global)
        {
            int P, M, C, T;
            int outP, outM, outC, outT;

            P = M = C = T = 0;
            outP = outM = outC = outT = 0;
            for (int i = 0; i < arr.Length; i++)
                if ((int)(arr[i].kind & VaribleKind.Используемая) != 0)
                {
                    if ((int)(arr[i].kind & VaribleKind.Управляющая) == 4)
                        C++;
                    else if ((int)(arr[i].kind & VaribleKind.Модифицируемая) == 3)
                        M++;
                    else
                        P++;
                }
                else
                    T++;

            //ввод\вывод
            for(int i = 0; i < arr.Length; i++)
                if(((int)(arr[i].kind & VaribleKind.Ввод) == 40) ||
                    ((int)(arr[i].kind & VaribleKind.Выводимая) == 20))
                {
                    if ((int)(arr[i].kind & VaribleKind.Используемая) != 0)
                    {
                        if ((int)(arr[i].kind & VaribleKind.Управляющая) == 4)
                            outC++;
                        else if ((int)(arr[i].kind & VaribleKind.Модифицируемая) == 3)
                            outM++;
                        else
                            outP++;
                    }
                    else
                        outT++;
                }

            double result = P + 2 * M + 3 * C + 0.5 * T;
            double outResult = outP + 2 * outM + 3 * outC + 0.5 * outT;

            PrintValuesForm(P, M, C, T, result, global);
            PrintValuesForm(outP, outM, outC, outT, outResult, global, true);

            totalP += P; totalM += M; totalC += C; totalT += T;
            totalOutP += outP; totalOutM += outM; totalOutC += outC; totalOutT += outT;

            ChepinValue += result;
            OutChepinValue += outResult;
        }

        public  void PrintVaribles(in Vareble[] arr, in bool global)
        /*выводит переменные и их характеристики*/
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].kind == VaribleKind.Используемая)
                    arr[i].kind = arr[i].kind | VaribleKind.Модифицируемая;
                Console.WriteLine("{0} {1}", arr[i].name, arr[i].kind);
            }

            CalculateChepinValue(arr, global);
        }

        /*анализирует программный код находя в нём основные разделы описания*/
        public  void AnalyzeProgram(ref string str, ref int pos, ref bool global, ref string ident)
        {
            while (ident != null)
            {
                switch (ident.ToLower())
                {
                    case "let":
                        if (global) FindVaribles(ref str, ref pos, ref GlobalVaribles);
                        else FindVaribles(ref str, ref pos, ref LocalVaribles);
                        break;
                    case "var":
                        if (global) FindVaribles(ref str, ref pos, ref GlobalVaribles);
                        else FindVaribles(ref str, ref pos, ref LocalVaribles);
                        break;
                    case "function":
                        global = false;
                        LocalVaribles = new Vareble[0];
                        AnalayzeMethodHead(ref str, ref pos);
                        //AddToVarArray(ref LocalVaribles, "Result");
                        //DetermineVarible("Result", VaribleKind.Используемая);
                        break;
                    /*
                    case "procedure":
                        global = false;
                        LocalVaribles = new Vareble[0];
                        AnalayzeMethodHead(ref str, ref pos);
                        break;
                    */
                    case "{": //начало подпрограммы
                        if (beginEndCounter == 0)
                        {
                            beginEndCounter++;
                            break;
                        }


                        beginEndCounter++;

                        while (beginEndCounter != 0)
                        {
                            ident = FindNextIdentifier(ref str, ref pos, false);
                            AnalyzeProgram(ref str, ref pos, ref global, ref ident);
                            //AnalyzeOperator(ref str, ref pos, ref ident, ref global);
                        }

                        //ident = FindNextIdentifier(ref str, ref pos, false);
                        //if (ident == "else")
                        //  AnalyzeOperator(ref str, ref pos, ref ident, in global);
                        //else
                        /*
                            //подпрограмма закончилась
                            if (!global)
                            {
                                CreateOutput(ref LocalVaribles);
                            }

                            LocalVaribles = new Vareble[0];
                            global = true;

                        */
                        break;
                    default:
                        AnalyzeOperator(ref str, ref pos, ref ident, ref global);
                        break;
                }

                if (str != null && ident != null)
                    ident = FindNextIdentifier(ref str, ref pos, false);
            }
        }

            
        //возращает введённое с клавиатуры имя файла, но если он несуществует то возр. null*/
        /*
        public static string GetFileName()
        {
            string fileName = Console.ReadLine();
            if (File.Exists(fileName))
                return fileName;
            else
                return null;
        }
        */

        public static void GetNoEqualData(ref Vareble[] arr)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                for (int j = i + 1; j < arr.Length; j++)
                {
                    if (arr[i].name == arr[j].name)
                    {
                        arr[i].kind = arr[i].kind | arr[j].kind;

                        //сдвиг
                        for (int k = j + 1; k < arr.Length; k++)
                            arr[k - 1] = arr[k];

                        Array.Resize(ref arr, arr.Length - 1);
                    }
                }
            }

        }

        //метод позволяет получить основную группу переменной из нескольких
        //по определённым правилам
        public static void GetOnlyOneKind(ref Vareble[] Variables)
        {
            for (int i = 0; i < Variables.Length; i++)
                if ((Variables[i].kind & VaribleKind.Используемая) != 0)
                {
                    if ((int)(Variables[i].kind & VaribleKind.Выводимая) == 2)
                    {
                        if ((int)(Variables[i].kind & VaribleKind.Управляющая) == 4)
                        {
                            int f = (int)(Variables[i].kind & VaribleKind.Управляющая);
                            Variables[i].kind = VaribleKind.Управляющая | VaribleKind.Выводимая;
                        }
                        else if ((int)(Variables[i].kind & VaribleKind.Модифицируемая) == 3)
                        {
                            int f = (int)(Variables[i].kind & VaribleKind.Модифицируемая);
                            Variables[i].kind = VaribleKind.Модифицируемая | VaribleKind.Выводимая;
                        }
                        else
                            Variables[i].kind = VaribleKind.Вводимая | VaribleKind.Выводимая;
                    }
                    else
                    { //если не выводится
                        if ((Variables[i].kind & VaribleKind.Управляющая) != 0)
                            Variables[i].kind = VaribleKind.Управляющая;
                        else if ((Variables[i].kind & VaribleKind.Модифицируемая) != 0)
                            Variables[i].kind = VaribleKind.Модифицируемая;
                        else
                            Variables[i].kind = VaribleKind.Вводимая;
                    }

                }
        }

        public  void OutputSpen(in Vareble[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                Console.WriteLine("{0} {1}", arr[i].name, arr[i].counter);


        }

        public  void PrintVariblesForm(in Vareble[] Variables, in bool isGlobal = false)
        {
            int neededIndex;

            if (isGlobal)
                dataGridView1.Rows.Add("Глобальные");


            dataGridView1.Rows.Add("Группа переменных",
                "P", "M", "C", "T", "P", "M", "C", "T");
            dataGridView1.Rows.Add("Переменные");
            dataGridView1.Rows.Add("Количество переменных");
            dataGridView1.Rows.Add("Метрика Чепина");

            if (isGlobal)
                neededIndex = 2 + 5 * (numOfMethods);
            else
                neededIndex = 2 + 5 * (numOfMethods - 1);

            for (int i = 0; i < Variables.Length; i++)
            {
                if((int)(Variables[i].kind & VaribleKind.Выводимая) != 20 &&
                    (int)(Variables[i].kind & VaribleKind.Ввод) != 40)
                    if ((Variables[i].kind & VaribleKind.Используемая) != 0)
                    {
                        if ((int)(Variables[i].kind & VaribleKind.Управляющая) == 4)
                            dataGridView1.Rows[neededIndex].Cells[(int)VariableIndex.Управляющая].Value +=
                             Variables[i].name + ", ";
                        else if ((int)(Variables[i].kind & VaribleKind.Модифицируемая) == 3)
                            dataGridView1.Rows[neededIndex].Cells[(int)VariableIndex.Модифицируемая].Value +=
                             Variables[i].name + ", ";
                        else
                            dataGridView1.Rows[neededIndex].Cells[(int)VariableIndex.Вводимая].Value +=
                              Variables[i].name + ", ";
                    }
                    else
                        dataGridView1.Rows[neededIndex].Cells[(int)VariableIndex.Паразитная].Value +=
                             Variables[i].name + ", ";

                //ввод/вывод
                if ((int)(Variables[i].kind & VaribleKind.Выводимая) == 20 ||
                    (int)(Variables[i].kind & VaribleKind.Ввод) == 40)
                {
                    if ((Variables[i].kind & VaribleKind.Используемая) != 0)
                    {
                        if ((int)(Variables[i].kind & VaribleKind.Управляющая) == 4)
                            dataGridView1.Rows[neededIndex].Cells[4 +(int)VariableIndex.Управляющая].Value +=
                              Variables[i].name + ", ";
                        else if ((int)(Variables[i].kind & VaribleKind.Модифицируемая) == 3)
                            dataGridView1.Rows[neededIndex].Cells[4 + (int)VariableIndex.Модифицируемая].Value +=
                             Variables[i].name + ", ";
                        else
                            dataGridView1.Rows[neededIndex].Cells[4+(int)VariableIndex.Вводимая].Value +=
                              Variables[i].name + ", ";
                    }
                    else
                        dataGridView1.Rows[neededIndex].Cells[4+(int)VariableIndex.Паразитная].Value +=
                              Variables[i].name + ", ";
                }

            }
        }


        private void OutputSpenForm(in Vareble[] arr, bool isGlobal = false)
        {
            if (isGlobal)
                dataGridView2.Columns.Add("", "Global");
            else
                dataGridView2.Columns.Add("",
                    dataGridView1.Rows[0+5*(numOfMethods-1)]
                    .Cells[0].Value.ToString());

            realSpenIndex++;

            for (int i = 0; i < arr.Length; i++)
            {

                dataGridView2.Columns.Add("col" + realSpenIndex.ToString()
                    , arr[i].name);
                dataGridView2.Columns[realSpenIndex - 1].Width = 40;
                dataGridView2.Rows[0].Cells[realSpenIndex - 1].Value =
                    arr[i].counter.ToString();

                realSpenIndex++;
            }  
        }
        
        private void CreateOutputForm(ref Vareble[] arr, in bool global = false)
        {
            GetRightVariables(ref arr);
            GetNoEqualData(ref arr);
            //GetOnlyOneKind(ref arr);
            PrintVariblesForm(arr, global);
            OutputSpenForm(arr, global);
            CalculateChepinValue(arr, global);
            CalculateSpenSum(arr);
        }

        public void CreateOutput(ref Vareble[] arr)
        {
            GetRightVariables(ref arr);
            GetNoEqualData(ref arr);
            //GetOnlyOneKind(ref arr);
            //PrintVaribles(arr);
            OutputSpen(arr);
        }

        public void GetRightVariables(ref Vareble[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                if ((int)arr[i].name[0] >= 48 && (int)arr[i].name[0] <= 58)
                {
                    for (int j = i + 1; j < arr.Length; j++) //сдвиг
                        arr[j - 1] = arr[j];

                    Array.Resize(ref arr, arr.Length - 1);
                }
        }

        void CalculateSpenSum(in Vareble[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                spenSum += arr[i].counter;
        }



        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            // получаем выбранный файл
            string filename = openFileDialog1.FileName;
            // читаем файл в строку
            string fileText = System.IO.File.ReadAllText(filename);
            textBox1.Text = fileText;   
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void ClearGrids()
        {
            dataGridView1.Rows.Clear();
            dataGridView2.Rows.Clear();

            int counter = dataGridView2.Columns.Count;

            for (int i = counter - 1; i > 0; i--)
                dataGridView2.Columns.RemoveAt(i);
        }

        private void ReloadVars()
        {
            GlobalVaribles = new Vareble[0];
            LocalVaribles = new Vareble[0];

            realSpenIndex = 2;
            strIndex = 0;
            numOfMethods = 0;
            spenSum = 0;
            totalP = totalM = totalC = totalT = 0;
            totalOutP = totalOutM = totalOutC = totalOutT = 0;
            ChepinValue = OutChepinValue = 0;
            beginEndCounter = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            ReloadVars();

            ClearGrids();
           
            dataGridView2.Rows.Add("Спен");



            int pos = 0;
            bool global = true;

            strArr = textBox1.Text.Split('\n');
            string s = GetString();

            string ident = FindNextIdentifier(ref s, ref pos, false);
            AnalyzeProgram(ref s, ref pos, ref global, ref ident);

            CreateOutputForm(ref GlobalVaribles, true);

            dataGridView2.Columns.Add("dfg", "Суммарный спен программы");
            dataGridView2.Rows[0].Cells[realSpenIndex - 1].Value =
                spenSum.ToString();

            dataGridView1.Rows.Add("", "");
            dataGridView1.Rows.Add("Итого:",
                "Q="+totalP.ToString() + "+2*",
                totalM.ToString()+"+3*", 
                totalC.ToString()+"+0.5*",
                totalT.ToString()+"="+ChepinValue.ToString(),
                "Q=" + totalOutP.ToString() + "+2*",
                totalOutM.ToString() + "+3*",
                totalOutC.ToString() + "+0.5*",
                totalOutT.ToString() + "=" + OutChepinValue.ToString()
                );

        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
