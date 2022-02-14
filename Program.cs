using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using XLua;

using System.Data;

namespace ExcelExport
{
    class Program
    {
        static LuaEnv luaEnv = new LuaEnv();

        [LuaCallCSharp]
        public static List<Type> LuaCallCSharp
        {
            get
            {
                List<Type> list = new List<Type>()
                {
                    typeof(DataSet)
                };
                return list;
            }
        }

        static int Main(string[] args)
        {
            if(args.Length<1)
            {
                Console.WriteLine("Input lua file");
                return -1;
            }

            //https://github.com/ExcelDataReader/ExcelDataReader#important-note-on-net-core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            luaEnv.AddBuildin("json", LuaJson.OpenLib);
            luaEnv.AddBuildin("excel", ExcelReader.OpenLib);
            luaEnv.AddBuildin("console", Debug.OpenLib);
            luaEnv.AddBuildin("fs", FileSystem.OpenLib);
            luaEnv.AddBuildin("utility", Utility.OpenLib);

            try
            {
                var luaString = File.ReadAllText(args[0]);
                var luaFn = luaEnv.LoadString(luaString, Path.GetFileName(args[0]));
                luaFn.Call(args);
                return 0;
            }
            catch(Exception ex)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = oldColor;
                return -1;
            }
        }
    }
}
