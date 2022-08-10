using System.Text.Json;
using System.IO;
using System.Text;
using KeraLua;
using LuaState = System.IntPtr;
using System.Collections.Generic;
using System;

namespace ExcelExport
{
    public class LuaJson
    {
        const int MAX_DEPTH = 64;

        static long ArraySize(LuaState L, int index)
        {
            // test first key
            LuaAPI.lua_pushnil(L);
            if (LuaAPI.lua_next(L, index) == 0) // empty table
                return 0;

            long firstkey = LuaAPI.lua_isinteger(L, -2) == 1 ? LuaAPI.luaL_checkinteger(L, -2) : 0;
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
                int objlen1 = LuaAPI.luaL_rawlen(L, index);
                LuaAPI.lua_pushinteger(L, objlen1);
                if (LuaAPI.lua_next(L, index) != 0) // has more fields?
                {
                    LuaAPI.lua_pop(L, 2);
                    return 0;
                }
                return objlen1;
            }

            int objlen = LuaAPI.luaL_rawlen(L, index);
            if (firstkey > objlen)
                return 0;

            LuaAPI.lua_pushnil(L);
            while (LuaAPI.lua_next(L, index) != 0)
            {
                if (LuaAPI.lua_isinteger(L, -2) == 1)
                {
                    var x = LuaAPI.lua_tointeger(L, -2);
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

        static void EncodeTable(Utf8JsonWriter writer, LuaState L, int idx, int depth = 0)
        {
            if ((++depth) > MAX_DEPTH)
                throw new LuaException("json.encode_table nested too depth");

            if (idx < 0)
                idx = LuaAPI.lua_gettop(L) + idx + 1;
            if (LuaAPI.lua_checkstack(L, 6) == 0)
                throw new LuaException("json.encode_table stack overflow");

            long size = ArraySize(L, idx);
            if (size > 0)
            {
                writer.WriteStartArray();
                for (long i = 1; i <= size; i++)
                {
                    LuaAPI.lua_rawgeti(L, idx, i);
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
                    var key_type = LuaAPI.luaL_type(L, -2);
                    switch (key_type)
                    {
                        case LuaType.String:
                            {
                                writer.WritePropertyName(LuaAPI.lua_tostring(L, -2));
                                EncodeOne(writer, L, -1, depth);
                                break;
                            }
                        case LuaType.Number:
                            {
                                if (LuaAPI.luaL_isinteger(L, -2))
                                {
                                    writer.WritePropertyName(LuaAPI.lua_tointeger(L, -2).ToString());
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

        static void EncodeOne(Utf8JsonWriter writer, LuaState L, int idx, int depth = 0)
        {
            var t = LuaAPI.luaL_type(L, idx);
            switch (t)
            {
                case LuaType.Boolean:
                    {
                        writer.WriteBooleanValue(LuaAPI.luaL_toboolean(L, idx));
                        return;
                    }
                case LuaType.Number:
                    {
                        if (LuaAPI.luaL_isinteger(L, idx))
                            writer.WriteNumberValue(LuaAPI.lua_tointeger(L, idx));
                        else
                            writer.WriteNumberValue(LuaAPI.lua_tonumber(L, idx));
                        return;
                    }
                case LuaType.String:
                    {
                        writer.WriteStringValue(LuaAPI.lua_tostring(L, idx));
                        return;
                    }
                case LuaType.Table:
                    {
                        EncodeTable(writer, L, idx, depth);
                        return;
                    }
                case LuaType.Nil:
                    {
                        writer.WriteNullValue();
                        return;
                    }
                case LuaType.LightUserData:
                    {
                        if (LuaAPI.lua_touserdata(L, idx) == LuaState.Zero)
                        {
                            writer.WriteNullValue();
                            return;
                        }
                        break;
                    }
            }
            throw new LuaException(string.Format("json encode: unsupport value type : {0}", t));
        }
        static int Encode(LuaState L)
        {
            LuaAPI.luaL_checktype(L, 1, (int)LuaType.Table);
            bool format = LuaAPI.luaL_toboolean(L, 2);
            LuaAPI.lua_settop(L, 1);
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = format });
            EncodeOne(writer, L, 1);
            writer.Flush();
            LuaAPI.lua_pushstring(L, Encoding.UTF8.GetString(ms.ToArray()));
            return 1;
        }

        public class LuaPushHandler
        {
            Stack<int> stack = new Stack<int>();
            LuaState L;

            public LuaPushHandler(LuaState L)
            {
                this.L = L;
            }

            public bool Bool(bool b)
            {
                LuaAPI.lua_pushboolean(L, b);
                Commit();
                return true;
            }

            public bool Long(long i)
            {
                LuaAPI.lua_pushinteger(L, i);
                Commit();
                return true;
            }

            public bool Double(double d)
            {
                LuaAPI.lua_pushnumber(L, d);
                Commit();
                return true;
            }

            public bool String(string str) {
                LuaAPI.lua_pushstring(L, str);
                Commit();
                return true;
            }

            public bool StartObject()
            {
                LuaAPI.luaL_checkstack(L, 20, null);
                LuaAPI.lua_createtable(L, 0, 8);
                stack.Push(0);// 0 means hash, >0 means array
                return true;
            }

            public bool Key(string str)
            {
                if (str.Length == 0)
                {
                    return false;
                }
                char c = str[0];
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    if (long.TryParse(str, out long res))
                    {
                        LuaAPI.lua_pushinteger(L, res);
                        return true;
                    }
                }
                LuaAPI.lua_pushstring(L, str);
                return true;
            }

            public bool EndObject()
            {
                stack.Pop();
                Commit();
                return true;
            }

            public bool StartArray()
            {
                LuaAPI.luaL_checkstack(L, 20, null);
                LuaAPI.lua_createtable(L, 8, 0);
                stack.Push(1);
                return true;
            }

            public bool EndArray()
            {
                stack.Pop();
                Commit();
                return true;
            }

            void Commit()
            {
                if (stack.TryPeek(out int v))
                {
                    if (v == 0)
                    {
                        LuaAPI.lua_rawset(L, -3);
                    }
                    else
                    {
                        LuaAPI.lua_rawseti(L, -2, v++);
                        stack.Pop();
                        stack.Push(v);
                    }
                }
            }
        }

        static void DecodeOne(Utf8JsonReader reader, LuaPushHandler handler)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartArray:
                        {
                            handler.StartArray();
                            break;
                        }
                    case JsonTokenType.EndArray:
                        {
                            handler.EndArray();
                            break;
                        }
                    case JsonTokenType.StartObject:
                        {
                            handler.StartObject();
                            break;
                        }
                    case JsonTokenType.EndObject:
                        {
                            handler.EndObject();
                            break;
                        }
                    case JsonTokenType.PropertyName:
                        {
                            handler.Key(reader.GetString());
                            break;
                        }
                    case JsonTokenType.String:
                        {
                            handler.String(reader.GetString());
                            break;
                        }
                    case JsonTokenType.Number:
                        {
                            if(reader.TryGetInt64(out long v))
                            {
                                handler.Long(v);
                            }
                            else
                            {
                                handler.Double(reader.GetDouble());
                            }
                            break;
                        }
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        {
                            handler.Bool(reader.GetBoolean());
                            break;
                        }
                }
            }
        }

        static int Decode(LuaState L)
        {
            try
            {
                var content = LuaAPI.lua_checkbuffer(L, 1);
                LuaAPI.lua_settop(L, 1);
                Utf8JsonReader reader = new Utf8JsonReader(content);
                LuaPushHandler handler = new LuaPushHandler(L);
                DecodeOne(reader, handler);
                return 1;
            }
            catch(Exception ex)
            {
                return LuaAPI.luaL_error(L, ex.Message);
            }
        }

        static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="encode", function = Encode},
            new LuaRegister{name ="decode", function = Decode},
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