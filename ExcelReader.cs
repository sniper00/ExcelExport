using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using XLua;

using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;

namespace ExcelExport
{
    public class ExcelReader
    {
        public delegate void TypePush(RealStatePtr L, object o);

        static void PushBool(RealStatePtr L, object o)
        {
            bool v = (bool)o;
            LuaAPI.lua_pushboolean(L, v);
        }
        static void PushInt(RealStatePtr L, object o)
        {
            int v = (int)o;
            LuaAPI.lua_pushint64(L, v);
        }

        static void PushLong(RealStatePtr L, object o)
        {
            long v = (long)o;
            LuaAPI.lua_pushint64(L, v);
        }

        static void PushUInt(RealStatePtr L, object o)
        {
            uint v = (uint)o;
            LuaAPI.lua_pushint64(L, v);
        }

        static void PushULong(RealStatePtr L, object o)
        {
            ulong v = (ulong)o;
            LuaAPI.lua_pushint64(L, (long)v);
        }

        static void PushFloat(RealStatePtr L, object o)
        {
            float v = (float)o;
            long lv = (long)v;
            if (lv == v)
            {
                LuaAPI.lua_pushint64(L, lv);
            }
            else
            {
                LuaAPI.lua_pushnumber(L, v);
            }
        }

        static void PushDouble(RealStatePtr L, object o)
        {
            double v = (double)o;
            long lv = (long)v;
            if (lv == v)
            {
                LuaAPI.lua_pushint64(L, lv);
            }
            else
            {
                LuaAPI.lua_pushnumber(L, v);
            }
        }

        static void PushString(RealStatePtr L, object o)
        {
            string v = (string)o;
            LuaAPI.lua_pushstring(L, v);
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

        static void PushDataTable(RealStatePtr L, ObjectTranslator translator, DataTable dt)
        {
            LuaAPI.lua_newtable(L);
            int ordinal = 1;
            foreach (DataRow row in dt.Rows)
            {
                LuaAPI.lua_newtable(L);
                foreach (DataColumn col in dt.Columns)
                {
                    object value = row[col];
                    Type type = value.GetType();
                    var fn = ToLuaMap.GetValueOrDefault(type);
                    if (fn != null)
                    {
                        fn(L, value);
                        LuaAPI.xlua_rawseti(L, -2, col.Ordinal + 1);
                    }
                }
                LuaAPI.xlua_rawseti(L, -2, ordinal++);
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int Read(RealStatePtr L)
        {
            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            string filePath = "";
            try
            {
                translator.Get(L, 1, out filePath);

                FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var reader = ExcelReaderFactory.CreateReader(fileStream))
                {
                    var dataset = reader.AsDataSet();
                    LuaAPI.lua_newtable(L);
                    int ordinal = 1;
                    foreach (DataTable t in dataset.Tables)
                    {
                        PushDataTable(L, translator, t);
                        LuaAPI.xlua_rawseti(L, -2, ordinal++);
                    }
                    return 1;
                }
            }
            catch (Exception ex)
            {
                LuaAPI.lua_pushboolean(L, false);
                translator.Push(L, string.Format("ExcelDataReader File {0} {1}", filePath, ex.Message));
                return 2;
            }
        }

        static LuaCSFunction templateRead = Read;

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int OpenLib(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "read");
            LuaAPI.lua_pushstdcallcfunction(L, templateRead);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }
}
