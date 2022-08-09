using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using KeraLua;
using LuaState = System.IntPtr;

namespace ExcelExport
{
    public class ExcelReader
    {
        public delegate void TypePush(LuaState L, object o);

        static void PushBool(LuaState L, object o)
        {
            bool v = (bool)o;
            LuaAPI.lua_pushboolean(L, v?1:0);
        }
        static void PushInt(LuaState L, object o)
        {
            int v = (int)o;
            LuaAPI.lua_pushinteger(L, v);
        }

        static void PushLong(LuaState L, object o)
        {
            long v = (long)o;
            LuaAPI.lua_pushinteger(L, v);
        }

        static void PushUInt(LuaState L, object o)
        {
            uint v = (uint)o;
            LuaAPI.lua_pushinteger(L, v);
        }

        static void PushULong(LuaState L, object o)
        {
            ulong v = (ulong)o;
            LuaAPI.lua_pushinteger(L, (long)v);
        }

        static void PushFloat(LuaState L, object o)
        {
            float v = (float)o;
            long lv = (long)v;
            if (lv == v)
            {
                LuaAPI.lua_pushinteger(L, lv);
            }
            else
            {
                LuaAPI.lua_pushnumber(L, v);
            }
        }

        static void PushDouble(LuaState L, object o)
        {
            double v = (double)o;
            long lv = (long)v;
            if (lv == v)
            {
                LuaAPI.lua_pushinteger(L, lv);
            }
            else
            {
                LuaAPI.lua_pushnumber(L, v);
            }
        }

        static void PushString(LuaState L, object o)
        {
            LuaAPI.lua_pushutf8string(L, (string)o);
        }

        static Dictionary<Type, TypePush> ToLuaMap = new Dictionary<Type, TypePush>
        {
            { typeof(bool), PushBool},
            { typeof(int), PushInt},
            { typeof(long), PushLong },
            { typeof(uint), PushUInt },
            { typeof(ulong), PushULong },
            { typeof(float), PushFloat },
            { typeof(double), PushDouble },
            { typeof(string),PushString }
        };

        static void PushDataTable(LuaState L, DataTable dt)
        {
            LuaAPI.lua_createtable(L, dt.Rows.Count, 0);
            int ordinal = 1;
            foreach (DataRow row in dt.Rows)
            {
                LuaAPI.lua_createtable(L, dt.Columns.Count, 0);
                foreach (DataColumn col in dt.Columns)
                {
                    object value = row[col];
                    Type type = value.GetType();
                    var fn = ToLuaMap.GetValueOrDefault(type);
                    if (fn != null)
                    {
                        fn(L, value);
                        LuaAPI.lua_rawseti(L, -2, col.Ordinal + 1);
                    }
                }
                LuaAPI.lua_rawseti(L, -2, ordinal++);
            }
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static int Read(LuaState L)
        {
            string filePath = "";
            try
            {
                filePath = LuaAPI.lua_checkstring(L, 1);
                FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var reader = ExcelReaderFactory.CreateReader(fileStream))
                {
                    var dataset = reader.AsDataSet();
                    LuaAPI.lua_createtable(L, dataset.Tables.Count, 0);
                    int ordinal = 1;
                    foreach (DataTable t in dataset.Tables)
                    {
                        PushDataTable(L, t);
                        LuaAPI.lua_rawseti(L, -2, ordinal++);
                    }
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LuaAPI.lua_pushboolean(L, false);
                LuaAPI.lua_pushstring(L, "ExcelDataReader File {0} {1}", filePath, ex.Message);
                return 2;
            }
        }

        static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="read", function = Read},
            new LuaRegister{name = null, function = null}
        };

        //[MonoPInvokeCallback(typeof(LuaFunction))]
        public static int OpenLib(LuaState L)
        {
            LuaAPI.luaL_newlib(L, l);
            return 1;
        }
    }
}
