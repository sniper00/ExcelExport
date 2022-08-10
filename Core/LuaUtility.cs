using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using KeraLua;
using LuaState = System.IntPtr;
using System.Net.Http;
using System.Net.Http.Headers;
using LuaTask;

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

        public class HttpResponse
        {
            public int status_code { get; set; }
            public string version { get; set; }
            public HttpResponseHeaders headers { get; set; }
            public string content { get; set; }
        }

        static async void DoHttpRequest(TaskManager mgr, long id, long session, string method, string uri, string content, Dictionary<string, string> headers)
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(new HttpMethod(method), uri);
                if (content != null)
                    httpRequestMessage.Content = new StringContent(content);

                if (null != headers)
                {
                    foreach (var v in headers)
                    {
                        if(string.Compare(v.Key, "Content-Type", true) == 0)
                        {
                            httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(v.Value);
                        }
                        else
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(v.Key, v.Value);
                        }
                    }
                }

                var client = new HttpClient();

                HttpResponseMessage httpResponseMessage = await client.SendAsync(httpRequestMessage);

                var httpResponse = new HttpResponse();
                httpResponse.status_code = (int)httpResponseMessage.StatusCode;
                httpResponse.version = httpResponseMessage.Version.ToString();
                httpResponse.headers = httpResponseMessage.Headers;
                httpResponse.content = await httpResponseMessage.Content.ReadAsStringAsync();
                var str = JsonSerializer.Serialize(httpResponse);
                mgr.SendMessage(0, id, Encoding.UTF8.GetBytes(str), -session, PTYPE.Http);
            }
            catch(Exception ex)
            {
                mgr.SendMessage(0, id, Encoding.UTF8.GetBytes(ex.Message), -session, PTYPE.Error);
            }
        }

        static public int HttpRequest(LuaState L)
        {
            var id = LuaAPI.luaL_checkinteger(L, 1);
            var session = LuaAPI.luaL_checkinteger(L, 2);
            string uri = LuaAPI.lua_checkstring(L, 3);
            string method = LuaAPI.lua_checkstring(L, 4);
            string content = null;
            if (0 != LuaAPI.lua_isstring(L, 5))
                content = LuaAPI.lua_tostring(L, 5);
            Dictionary<string, string> headers = null;
            if(LuaType.Table == LuaAPI.luaL_type(L, 6))
            {
                headers = new Dictionary<string, string>();
                LuaAPI.lua_pushnil(L);
                while (LuaAPI.lua_next(L, -2)!=0)
                {
                    headers.Add(LuaAPI.lua_checkstring(L, -2), LuaAPI.lua_checkstring(L, -1));
                    LuaAPI.lua_pop(L, 1);
                }
            }
            LuaService S = LuaService.FromIntPtr(L);
            DoHttpRequest(S.taskManager, id, session, method, uri, content, headers);
            return 0;
        }

        static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="strfmt", function = StringFormat},
            new LuaRegister{name ="cpu", function = GetCpuNum},
            new LuaRegister{name ="http_request", function = HttpRequest},
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
