using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace lsb
{
    class Program
    {
        static string GetWordsBytes(string s) // получение строки последовательности битов сообщения
        {
            string bytes = "";
            byte[] code = Encoding.GetEncoding(1251).GetBytes(s); // последовательность байтов в ASCII
            foreach (var c in code)
            {
                string newByte = Convert.ToString(c, 2);
                for (int i = newByte.Length; i < 8; i++)
                    bytes += '0'; // добавление первых нулей
                bytes += newByte;
            }
            return bytes;
        }

        static Pixel[,] LoadPixels(Bitmap bmp) // создание массива пикселей файла bmp
        {
            var pixels = new Pixel[bmp.Width, bmp.Height];
            for (var x = 0; x < bmp.Width; x++)
                for (var y = 0; y < bmp.Height; y++)
                    pixels[x, y] = new Pixel(bmp.GetPixel(x, y));
            return pixels;
        }

        static string GetImage (Pixel[,] pixels, string file) // создание файла bmp с новым словом
        {
            Console.WriteLine("Введите слово, которое вы хотите закодировать");
            string word = Console.ReadLine();          
            string message = GetWordsBytes(word); 
            Console.WriteLine("Кодируем последовательность: " + message);

            int height = pixels.GetLength(1);
            int width = pixels.GetLength(0);
            if (height* width * 3 < message.Length) return null; // сообщение большое для данного файла
            var newPixels = CodeMessage(pixels, message); // встраивание сообщения 

            return CreateBitmap(newPixels, file); // создание нового файла bmp

        }

        static Pixel[,] CodeMessage (Pixel[,] pixels, string message) // встраивание сообщение в массив пикселей
        {
            int height = pixels.GetLength(1);
            int width = pixels.GetLength(0);
            var newPixels = new Pixel[width, height];
            
            int i;
            byte[] mess = new byte[message.Length + 8]; // последовательность встраиваемых битов
            for (i = 0; i < mess.Length; i++)
            {
                mess[i] = 0;
                if (i < message.Length) if (message[i] == '1') mess[i] = 1;
            }
            
            i = 0;
            byte mask = 254;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                { // добавление 1 или 0 на место последнего бита
                    newPixels[x, y] = pixels[x, y];
                    if (i < mess.Length) newPixels[x, y].R = (byte)(pixels[x, y].R & mask | mess[i++]);
                    if (i < mess.Length) newPixels[x, y].G = (byte)(pixels[x, y].G & mask | mess[i++]);
                    if (i < mess.Length) newPixels[x, y].B = (byte)(pixels[x, y].B & mask | mess[i++]);
                }
            }
            return newPixels;
        }

        static string GetString (string byteString) // декодирование последовательности битов в сообщение
        {
            byte[] bytes = new byte[byteString.Length / 8];
            for (int i = 0; i + 8 <= byteString.Length; i += 8)            
                    bytes[i / 8] = Convert.ToByte(byteString.Substring(i, 8), 2);                 
            string s = Encoding.GetEncoding(1251).GetString(bytes);
            return s;
        }
               
        static string GetMessage (Pixel[,] pixels) // извлечение сообщения из файла
        {
            int height = pixels.GetLength(1);
            int width = pixels.GetLength(0);
            string bytes = ""; // строка с последовательностью встроенных 0 и 1

            int k = 0;
            string lastBit;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                { // считывание последних битов каждого цвета пикселя                    
                    lastBit = Convert.ToString(pixels[x, y].R % 2);
                    bytes += lastBit;
                    if (lastBit == "0") k++;
                    else k = 0;
                    if (k == 8) break;
                    lastBit = Convert.ToString(pixels[x, y].G % 2);
                    bytes += lastBit;
                    if (lastBit == "0") k++;
                    else k = 0;
                    if (k == 8) break; lastBit = Convert.ToString(pixels[x, y].B % 2);
                    bytes += lastBit;
                    if (lastBit == "0") k++;
                    else k = 0;
                    if (k == 8) break;
                }
                if (k == 8) break; // встречен байт 00000000 = null
            }
            return GetString(bytes); // декодирование последовательности битов 
        }

        static string CreateBitmap (Pixel[,] newPixels, string file) // создание нового файла bmp
        {
            Random rnd = new Random();
            string name;
            name = rnd.Next(0, 999).ToString() + file; // создание рандомного имени
            Bitmap bmp = new Bitmap(file, true);
            for (int x = 0; x < newPixels.GetLength(0); x++)
                for (int y = 0; y < newPixels.GetLength(1); y++)
                    bmp.SetPixel(x, y, Color.FromArgb(newPixels[x, y].R, newPixels[x, y].G, newPixels[x, y].B));
           
            FileStream nameFile;
            nameFile = new FileStream(name, FileMode.Create); //открываем поток на запись результатов 
            bmp.Save(nameFile, System.Drawing.Imaging.ImageFormat.Bmp);
            nameFile.Close();
            return name;
        }

        static double CountPsnr(string name1, string name2) // подсчет psnr двух файлов
        {
            Bitmap bmp1 = (Bitmap)Image.FromFile(name1);
            Bitmap bmp2 = (Bitmap)Image.FromFile(name2);
            var pixels1 = LoadPixels(bmp1);
            var pixels2 = LoadPixels(bmp2);
            float MSE = 0;
            for (int x = 0; x < pixels1.GetLength(0); x++)
                for (int y = 0; y < pixels1.GetLength(1); y++)
                {
                    MSE += (pixels1[x, y].R - pixels2[x, y].R) * (pixels1[x, y].R - pixels2[x, y].R);
                    MSE += (pixels1[x, y].G - pixels2[x, y].G) * (pixels1[x, y].G - pixels2[x, y].G);
                    MSE += (pixels1[x, y].B - pixels2[x, y].B) * (pixels1[x, y].B - pixels2[x, y].B);
                }
            if (MSE == 0) return -1;
            MSE /= (pixels1.GetLength(0) * pixels1.GetLength(1) * 3);
            float MAX = 255;
            return 10 * Math.Log10(MAX * MAX / MSE);
        }

        static void Main()
        {            
            Console.WriteLine("Введите номер действия: 1. Встроить. 2. Извлечь.");
            int action = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Введите имя обрабатываемого файла");
            string picName = Console.ReadLine(); 
            

            Bitmap bmp; // обрабатываемый файл
            try
            {
                bmp = (Bitmap)Image.FromFile(picName);
            }
            catch
            {
                Console.WriteLine("Неверное имя файла");
                return;
            }
            
            var pixels = LoadPixels(bmp); // создаем массив пикселей обрабатываемого файла
            switch (action)
            {
                case 1:
                    var picNew = GetImage(pixels, picName); // создаем новый файл
                    if (picNew == null) Console.WriteLine("Слишком длинное сообщение"); 
                    else
                    {
                        Console.WriteLine("Имя нового файла: " + picNew);
                        double psnr = CountPsnr(picName, picNew); // считаем psnr
                        if (psnr==-1) Console.WriteLine("Сообщения одинаковые");
                        else Console.WriteLine("PSNR: " + Convert.ToString(psnr));
                    }                    
                    break;
                case 2:
                    var message = GetMessage(pixels); // извлечение сообщения из файла                    
                        Console.WriteLine("Сообщение: " + message);
                    break;
                default:
                    Console.WriteLine("Неправильно выбран метод");
                    break;
            }
            Console.ReadKey();
        }
    }
}
