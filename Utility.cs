using System;
using XLua;

using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;

namespace ExcelExport
{
    public static class Utility
    {
        static public int StringFormat(RealStatePtr L)
        {
            try
            {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
                int n = LuaAPI.lua_gettop(L);
                if(n==0)
                {
                    return LuaAPI.luaL_error(L, "string.Format need at least on param");
                }

                string fmt;
                translator.Get(L, 1, out fmt);

                object[] param = new object[n - 1];
                for (int i = 2; i <= n; i++)
                {
                    translator.Get(L, i, out param[i - 2]);
                }
                LuaAPI.lua_pushstring(L, string.Format(fmt, param));
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception Console.Log:" + e);
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int OpenLib(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "format");
            LuaAPI.lua_pushstdcallcfunction(L, StringFormat);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }
}
