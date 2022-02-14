using System.Text.Json;
using XLua;
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
using System.IO;
using System.Text;

namespace ExcelExport
{
    public class LuaJson
    {
        const int MAX_DEPTH = 64;

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]

        static long array_size(RealStatePtr L, int index)
        {
            // test first key
            LuaAPI.lua_pushnil(L);
            if (LuaAPI.lua_next(L, index) == 0) // empty table
                return 0;

            long firstkey = LuaAPI.lua_isinteger(L, -2) ? LuaAPI.lua_toint64(L, -2) : 0;
            LuaAPI.lua_pop(L, 2);

            if (firstkey <= 0)
            {
                return 0;
            }
            else if (firstkey == 1)
            {
                /*
                * https://www.lua.org/manual/5.4/manual.html#6.6
                * The length operator applied on a table returns a border in that table.
                * A border in a table t is any natural number that satisfies the following condition :
                * (border == 0 or t[border] ~= nil) and t[border + 1] == nil
                */
                uint objlen1 = LuaAPI.xlua_objlen(L, index);
                LuaAPI.lua_pushint64(L, objlen1);
                if (LuaAPI.lua_next(L, index) != 0) // has more fields?
                {
                    LuaAPI.lua_pop(L, 2);
                    return 0;
                }
                return objlen1;
            }

            uint objlen = LuaAPI.xlua_objlen(L, index);
            if (firstkey > objlen)
                return 0;

            LuaAPI.lua_pushnil(L);
            while (LuaAPI.lua_next(L, index) != 0)
            {
                if (LuaAPI.lua_isinteger(L, -2))
                {
                    var x = LuaAPI.lua_toint64(L, -2);
                    if (x > 0 && x <= objlen)
                    {
                        LuaAPI.lua_pop(L, 1);
                        continue;
                    }
                }
                LuaAPI.lua_pop(L, 2);
                return 0;
            }
            return objlen;
        }

        static void EncodeTable(Utf8JsonWriter writer, RealStatePtr L, int idx, int depth = 0)
        {
            if ((++depth) > MAX_DEPTH)
                throw new LuaException("json.encode_table nested too depth");

            if (idx < 0)
                idx = LuaAPI.lua_gettop(L) + idx + 1;
            if (!LuaAPI.lua_checkstack(L, 6))
                throw new LuaException("json.encode_table stack overflow");

            long size = array_size(L, idx);
            if (size > 0)
            {
                writer.WriteStartArray();
                for (long i = 1; i <= size; i++)
                {
                    LuaAPI.xlua_rawgeti(L, idx, i);
                    EncodeOne(writer, L, -1, depth);
                    LuaAPI.lua_pop(L, 1);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartObject();
                LuaAPI.lua_pushnil(L); // [table, nil]
                while (LuaAPI.lua_next(L, idx) != 0)
                {
                    var key_type = LuaAPI.lua_type(L, -2);
                    switch (key_type)
                    {
                        case LuaTypes.LUA_TSTRING:
                            {
                                writer.WritePropertyName(LuaAPI.lua_tostring(L, -2));
                                EncodeOne(writer, L, -1, depth);
                                break;
                            }
                        case LuaTypes.LUA_TNUMBER:
                            {
                                if (LuaAPI.lua_isinteger(L, -2))
                                {
                                    writer.WritePropertyName(LuaAPI.lua_toint64(L, -2).ToString());
                                    EncodeOne(writer, L, -1, depth);
                                }
                                else
                                {
                                    throw new LuaException("json encode: not support double type 'key'.");
                                }
                                break;
                            }
                        default:
                            throw new LuaException(string.Format("json encode: unsupport key type : {0}", key_type));
                    }
                    LuaAPI.lua_pop(L, 1);
                }
                writer.WriteEndObject();
            }
        }

        static void EncodeOne(Utf8JsonWriter writer, RealStatePtr L, int idx, int depth = 0)
        {
            var t = LuaAPI.lua_type(L, idx);
            switch (t)
            {
                case LuaTypes.LUA_TBOOLEAN:
                    {
                        writer.WriteBooleanValue(LuaAPI.lua_toboolean(L, idx));
                        return;
                    }
                case LuaTypes.LUA_TNUMBER:
                    {
                        if (LuaAPI.lua_isinteger(L, idx))
                            writer.WriteNumberValue(LuaAPI.lua_toint64(L, idx));
                        else
                            writer.WriteNumberValue(LuaAPI.lua_tonumber(L, idx));
                        return;
                    }
                case LuaTypes.LUA_TSTRING:
                    {
                        writer.WriteStringValue(LuaAPI.lua_tostring(L, idx));
                        return;
                    }
                case LuaTypes.LUA_TTABLE:
                    {
                        EncodeTable(writer, L, idx, depth);
                        return;
                    }
                case LuaTypes.LUA_TNIL:
                    {
                        writer.WriteNullValue();
                        return;
                    }
                case LuaTypes.LUA_TLIGHTUSERDATA:
                    {
                        if (LuaAPI.lua_touserdata(L, idx) == RealStatePtr.Zero)
                        {
                            writer.WriteNullValue();
                            return;
                        }
                        break;
                    }
            }
            throw new LuaException(string.Format("json encode: unsupport value type : {0}", t));
        }
        static int Encode(RealStatePtr L)
        {
            var type = LuaAPI.lua_type(L, 1);
            if (type != LuaTypes.LUA_TTABLE)
                throw new LuaException("param '1' need table type");
            bool format = LuaAPI.lua_toboolean(L, 2);
            LuaAPI.lua_settop(L, 1);
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = format });
            EncodeOne(writer, L, 1, 0);
            writer.Flush();
            LuaAPI.lua_pushstring(L, Encoding.UTF8.GetString(ms.ToArray()));
            return 1;
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int OpenLib(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "encode");
            LuaAPI.lua_pushstdcallcfunction(L, Encode);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }
}