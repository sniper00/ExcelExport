using System;

using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
using XLua;
using System.IO;

namespace ExcelExport
{
    public class FileSystem
    {
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int ListDir(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            try
            {
                translator.Get(L, 1, out string dir);
                translator.Get(L, 2, out string searchPattern);

                string[] files = Directory.GetFiles(dir, searchPattern);

                LuaAPI.lua_newtable(L);
                int idx = 1;
                foreach(var file in files)
                {
                    LuaAPI.lua_pushstring(L, file);
                    LuaAPI.xlua_rawseti(L, -2, idx++);
                }
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.ListDir:" + e);
            }
        }

        public static int GetFileName(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            try
            {
                translator.Get(L, 1, out string file);

                var filename = Path.GetFileName(file);
                LuaAPI.lua_pushstring(L, filename);
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.GetFileName:" + e);
            }
        }

        public static int GetFileNameWithoutExtension(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            try
            {
                translator.Get(L, 1, out string file);

                var filename = Path.GetFileNameWithoutExtension(file);
                LuaAPI.lua_pushstring(L, filename);
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.GetFileName:" + e);
            }
        }

        public static int CreateDirectory(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            try
            {
                translator.Get(L, 1, out string path);
                Directory.CreateDirectory(path);
                return 0;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.CreateDirectory:" + e);
            }
        }

        public static int PathJoin(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            try
            {
                translator.Get(L, 1, out string path1);
                translator.Get(L, 2, out string path2);
                translator.PushAny(L, Path.Join(path1, path2));
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception FileSystem.PathJoin:" + e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int OpenLib(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "listdir");
            LuaAPI.lua_pushstdcallcfunction(L, ListDir);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "name");
            LuaAPI.lua_pushstdcallcfunction(L, GetFileName);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "stem");
            LuaAPI.lua_pushstdcallcfunction(L, GetFileNameWithoutExtension);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "createdirs");
            LuaAPI.lua_pushstdcallcfunction(L, CreateDirectory);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "join");
            LuaAPI.lua_pushstdcallcfunction(L, PathJoin);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }
}
