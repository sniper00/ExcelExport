using System;
using System.IO;
using KeraLua;
using LuaState = System.IntPtr;

namespace ExcelExport
{
    public class FileSystem
    {
        public static int ListDir(LuaState L)
        {
            try
            {
                string dir = LuaAPI.lua_checkstring(L, 1);
                string searchPattern = LuaAPI.lua_checkstring(L, 2);

                string[] files = Directory.GetFiles(dir, searchPattern);

                int idx = 1;
                LuaAPI.lua_createtable(L, files.Length, 0);
                foreach (var file in files)
                {
                    LuaAPI.lua_pushstring(L, file);
                    NativeMethods.lua_rawseti(L, -2, idx++);
                }
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "FileSystem.ListDir:" + e);
            }
        }

        public static int GetFileName(LuaState L)
        {
            try
            {
                string file = LuaAPI.lua_checkstring(L, 1);

                var filename = Path.GetFileName(file);
                LuaAPI.lua_pushstring(L, filename);
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.GetFileName:" + e);
            }
        }

        public static int GetFileNameWithoutExtension(LuaState L)
        {
            try
            {
                string file = LuaAPI.lua_checkstring(L, 1);

                var filename = Path.GetFileNameWithoutExtension(file);
                LuaAPI.lua_pushstring(L, filename);
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.GetFileName:" + e);
            }
        }

        public static int CreateDirectory(LuaState L)
        {
            try
            {
                string path = LuaAPI.lua_checkstring(L, 1);
                Directory.CreateDirectory(path);
                return 0;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.CreateDirectory:" + e);
            }
        }

        public static int PathJoin(LuaState L)
        {
            try
            {
                string path1 = LuaAPI.lua_checkstring(L, 1);
                string path2 = LuaAPI.lua_checkstring(L, 2);
                LuaAPI.lua_pushstring(L, Path.Join(path1, path2));
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.PathJoin:" + e);
            }
        }

        static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="listdir", function = ListDir},
            new LuaRegister{name ="name", function = GetFileName},
            new LuaRegister{name ="stem", function = GetFileNameWithoutExtension},
            new LuaRegister{name ="createdirs", function = CreateDirectory},
            new LuaRegister{name ="join", function = PathJoin},
            new LuaRegister{name = null, function = null}
        };

        [MonoPInvokeCallback(typeof(LuaFunction))]
        public static int OpenLib(LuaState L)
        {
            LuaAPI.luaL_newlib(L, l);
            return 1;
        }
    }
}
