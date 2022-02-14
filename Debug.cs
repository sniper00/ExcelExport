using System;
using System.Collections.Generic;
using System.Text;
using XLua;

using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;

namespace ExcelExport
{
    public class Debug
    {
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int Info(RealStatePtr L)
        {
            try
            {
                int n = LuaAPI.lua_gettop(L);
                string s = string.Empty;

                if (0 != LuaAPI.xlua_getglobal(L, "tostring"))
                {
                    return LuaAPI.luaL_error(L, "can not get tostring in print:");
                }

                for (int i = 1; i <= n; i++)
                {
                    LuaAPI.lua_pushvalue(L, -1);  /* function to be called */
                    LuaAPI.lua_pushvalue(L, i);   /* value to print */
                    if (0 != LuaAPI.lua_pcall(L, 1, 1, 0))
                    {
                        return LuaAPI.lua_error(L);
                    }
                    s += LuaAPI.lua_tostring(L, -1);

                    if (i != n) s += "\t";

                    LuaAPI.lua_pop(L, 1);  /* pop result */
                }
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(s);
                Console.ForegroundColor = oldColor;
                return 0;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception Console.Log:" + e);
            }
        }

        public static int Warn(RealStatePtr L)
        {
            try
            {
                int n = LuaAPI.lua_gettop(L);
                string s = string.Empty;

                if (0 != LuaAPI.xlua_getglobal(L, "tostring"))
                {
                    return LuaAPI.luaL_error(L, "can not get tostring in print:");
                }

                for (int i = 1; i <= n; i++)
                {
                    LuaAPI.lua_pushvalue(L, -1);  /* function to be called */
                    LuaAPI.lua_pushvalue(L, i);   /* value to print */
                    if (0 != LuaAPI.lua_pcall(L, 1, 1, 0))
                    {
                        return LuaAPI.lua_error(L);
                    }
                    s += LuaAPI.lua_tostring(L, -1);

                    if (i != n) s += "\t";

                    LuaAPI.lua_pop(L, 1);  /* pop result */
                }
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(s);
                Console.ForegroundColor = oldColor;
                return 0;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception Console.Log:" + e);
            }
        }

        public static int Error(RealStatePtr L)
        {
            try
            {
                int n = LuaAPI.lua_gettop(L);
                string s = string.Empty;

                if (0 != LuaAPI.xlua_getglobal(L, "tostring"))
                {
                    return LuaAPI.luaL_error(L, "can not get tostring in print:");
                }

                for (int i = 1; i <= n; i++)
                {
                    LuaAPI.lua_pushvalue(L, -1);  /* function to be called */
                    LuaAPI.lua_pushvalue(L, i);   /* value to print */
                    if (0 != LuaAPI.lua_pcall(L, 1, 1, 0))
                    {
                        return LuaAPI.lua_error(L);
                    }
                    s += LuaAPI.lua_tostring(L, -1);

                    if (i != n) s += "\t";

                    LuaAPI.lua_pop(L, 1);  /* pop result */
                }
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(s);
                Console.ForegroundColor = oldColor;
                return 0;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception Console.Log:" + e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int OpenLib(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "info");
            LuaAPI.lua_pushstdcallcfunction(L, Info);
            LuaAPI.lua_rawset(L, -3);

            LuaAPI.xlua_pushasciistring(L, "warn");
            LuaAPI.lua_pushstdcallcfunction(L, Warn);
            LuaAPI.lua_rawset(L, -3);

            LuaAPI.xlua_pushasciistring(L, "error");
            LuaAPI.lua_pushstdcallcfunction(L, Error);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }
}
