using System;
using KeraLua;
using LuaState = System.IntPtr;

namespace ExcelExport
{
    public static class LuaUtility
    {
        static public int StringFormat(LuaState L)
        {
            try
            {
                int n = LuaAPI.lua_gettop(L);
                if (n == 0)
                {
                    return LuaAPI.luaL_error(L, "string.Format need at least on param");
                }

                string fmt = LuaAPI.lua_tostring(L, 1);

                object[] param = new object[n - 1];
                for (int idx = 2; idx <= n; idx++)
                {
                    object v = null;

                    var t = LuaAPI.luaL_type(L, idx);
                    switch (t)
                    {
                        case LuaType.Boolean:
                            {
                                v = LuaAPI.luaL_toboolean(L, idx);
                                break;
                            }
                        case LuaType.Number:
                            {
                                if (LuaAPI.luaL_isinteger(L, idx))
                                    v = LuaAPI.lua_tointeger(L, idx);
                                else
                                    v = LuaAPI.lua_tonumber(L, idx);
                                break;
                            }
                        case LuaType.String:
                            {
                                v = LuaAPI.lua_tostring(L, idx);
                                break;
                            }
                    }
                    if (null == v)
                        throw new LuaException(string.Format("UnSupport lua type {0}", t));
                    param[idx - 2] = v;
                }
                LuaAPI.lua_pushstring(L, string.Format(fmt, param));
                return 1;
            }
            catch (Exception e)
            {
                return LuaAPI.luaL_error(L, e.Message);
            }
        }

        static public int GetCpuNum(LuaState L)
        {
            LuaAPI.lua_pushinteger(L, Environment.ProcessorCount);
            return 1;
        }

        static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="format", function = StringFormat},
            new LuaRegister{name ="cpu", function = GetCpuNum},
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
