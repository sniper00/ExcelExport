using System;
using System.Collections.Generic;
using System.IO;
using KeraLua;
using LuaTask;
using LuaState = System.IntPtr;

namespace ExcelExport
{
   
    class Program
    {
        static void OpenLibs(LuaState L)
        {
            LuaAPI.luaL_openlibs(L);
            LuaAPI.lua_openlib(L, "json", LuaJson.OpenLib);
            LuaAPI.lua_openlib(L, "fs", FileSystem.OpenLib);
            LuaAPI.lua_openlib(L, "excel", ExcelReader.OpenLib);
            LuaAPI.lua_openlib(L, "util", LuaUtility.OpenLib);
            LuaAPI.lua_openlib(L, "task.core", LuaService.OpenLib);
            LuaAPI.lua_openlib(L, "task.manager.core", TaskManager.OpenLib);
        }

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("        ExcelExport script.lua [args]");
            Console.WriteLine("Examples:");
            Console.WriteLine("        ExcelExport main.lua hello");
        }

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                return -1;
            }

            //https://github.com/ExcelDataReader/ExcelDataReader#important-note-on-net-core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try
            {
                if(!File.Exists(args[0]))
                {
                    Usage();
                    throw new LuaException("File not found " + args[0]);
                }
                var L = LuaAPI.luaL_newstate();
                TaskManager mgr = new TaskManager(L);
                OpenLibs(L);

                mgr.LuaEnvInit = OpenLibs;

                LuaAPI.lua_pushcfunction(L, LuaAPI.LuaTraceback);
                int tracefn = LuaAPI.lua_gettop(L);

                var r = LuaAPI.luaL_loadfile(L, args[0]);
                if (r!= LuaStatus.OK)
                {
                    throw new LuaException(LuaAPI.lua_tostring(L, -1));
                }

                LuaAPI.lua_createtable(L, args.Length, 0);
                for(int i = 0; i < args.Length; ++i)
                {
                    LuaAPI.lua_pushstring(L, args[i]);
                    LuaAPI.lua_rawseti(L, -2, i + 1);
                }

                r = LuaAPI.lua_pcall(L, 1, 0, tracefn);
                if (r!= LuaStatus.OK)
                {
                    throw new LuaException(LuaAPI.lua_tostring(L, -1));
                }
                mgr.Shutdown();
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
