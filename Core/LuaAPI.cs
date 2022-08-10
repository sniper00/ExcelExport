using System;
using System.Runtime.InteropServices;
using System.Text;
using LuaState = System.IntPtr;
using System.Security;
using charptr_t = System.IntPtr;
using lua_Alloc = System.IntPtr;
using lua_CFunction = System.IntPtr;
using lua_Debug = System.IntPtr;
using lua_Hook = System.IntPtr;
using lua_Integer = System.Int64;
using lua_KContext = System.IntPtr;
using lua_KFunction = System.IntPtr;
using lua_Number = System.Double;
using lua_Number_ptr = System.IntPtr;
using lua_Reader = System.IntPtr;
using lua_State = System.IntPtr;
using lua_WarnFunction = System.IntPtr;
using lua_Writer = System.IntPtr;
using size_t = System.UIntPtr;
using voidptr_t = System.IntPtr;

namespace KeraLua
{
    [SuppressUnmanagedCodeSecurity]
    internal static class LuaAPI
    {
        // ReSharper disable IdentifierTypo
#if __IOS__ || __TVOS__ || __WATCHOS__ || __MACCATALYST__
        private const string LuaLibraryName = "@rpath/liblua54.framework/liblua54";
#elif __ANDROID__
        private const string LuaLibraryName = "liblua54.so";
#elif __MACOS__
        private const string LuaLibraryName = "liblua54.dylib";
#elif WINDOWS_UWP
        private const string LuaLibraryName = "lua54.dll";
#else
        private const string LuaLibraryName = "lua54";
#endif

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA2101 // Bug on CA + VS2017

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_absindex(lua_State luaState, int idx);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_arith(lua_State luaState, int op);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_CFunction lua_atpanic(lua_State luaState, lua_CFunction panicf);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_callk(lua_State luaState, int nargs, int nresults, lua_KContext ctx, lua_KFunction k);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_checkstack(lua_State luaState, int extra);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_close(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_compare(lua_State luaState, int index1, int index2, int op);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_concat(lua_State luaState, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_copy(lua_State luaState, int fromIndex, int toIndex);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_createtable(lua_State luaState, int elements, int records);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_dump(lua_State luaState, lua_Writer writer, voidptr_t data, int strip);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_error(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gc(lua_State luaState, int what, int data);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gc(lua_State luaState, int what, int data, int data2);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Alloc lua_getallocf(lua_State luaState, ref voidptr_t ud);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int lua_getfield(lua_State luaState, int index, string k);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int lua_getglobal(lua_State luaState, string name);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Hook lua_gethook(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gethookcount(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gethookmask(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_geti(lua_State luaState, int index, long i);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int lua_getinfo(lua_State luaState, string what, lua_Debug ar);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_getiuservalue(lua_State luaState, int idx, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern charptr_t lua_getlocal(lua_State luaState, lua_Debug ar, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_getmetatable(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_getstack(lua_State luaState, int level, lua_Debug n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gettable(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_gettop(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern charptr_t lua_getupvalue(lua_State luaState, int funcIndex, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_iscfunction(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_isinteger(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_isnumber(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_isstring(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_isuserdata(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_isyieldable(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_len(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int lua_load
           (lua_State luaState,
            lua_Reader reader,
            voidptr_t data,
            string chunkName,
            string mode);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_State lua_newstate(lua_Alloc allocFunction, voidptr_t ud);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_State lua_newthread(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern voidptr_t lua_newuserdatauv(lua_State luaState, size_t size, int nuvalue);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_next(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_pcallk
            (lua_State luaState,
            int nargs,
            int nresults,
            int errorfunc,
            lua_KContext ctx,
            lua_KFunction k);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushboolean(lua_State luaState, int value);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushcclosure(lua_State luaState, lua_CFunction f, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushinteger(lua_State luaState, lua_Integer n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushlightuserdata(lua_State luaState, voidptr_t udata);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern charptr_t lua_pushlstring(lua_State luaState, byte[] s, size_t len);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushnil(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushnumber(lua_State luaState, lua_Number number);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_pushthread(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_pushvalue(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_rawequal(lua_State luaState, int index1, int index2);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_rawget(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_rawgeti(lua_State luaState, int index, lua_Integer n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_rawgetp(lua_State luaState, int index, voidptr_t p);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern size_t lua_rawlen(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_rawset(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_rawseti(lua_State luaState, int index, lua_Integer i);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_rawsetp(lua_State luaState, int index, voidptr_t p);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_resetthread(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_resume(lua_State luaState, lua_State from, int nargs, out int results);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_rotate(lua_State luaState, int index, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_setallocf(lua_State luaState, lua_Alloc f, voidptr_t ud);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void lua_setfield(lua_State luaState, int index, string key);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void lua_setglobal(lua_State luaState, string key);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_sethook(lua_State luaState, lua_Hook f, int mask, int count);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_seti(lua_State luaState, int index, long n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_setiuservalue(lua_State luaState, int index, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern charptr_t lua_setlocal(lua_State luaState, lua_Debug ar, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_setmetatable(lua_State luaState, int objIndex);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_settable(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_settop(lua_State luaState, int newTop);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern charptr_t lua_setupvalue(lua_State luaState, int funcIndex, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_setwarnf(lua_State luaState, lua_WarnFunction warningFunctionPtr, voidptr_t ud);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_status(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern size_t lua_stringtonumber(lua_State luaState, string s);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_toboolean(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_CFunction lua_tocfunction(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_CFunction lua_toclose(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Integer lua_tointegerx(lua_State luaState, int index, out int isNum);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern charptr_t lua_tolstring(lua_State luaState, int index, out size_t strLen);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Number lua_tonumberx(lua_State luaState, int index, out int isNum);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern voidptr_t lua_topointer(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_State lua_tothread(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern voidptr_t lua_touserdata(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_type(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern charptr_t lua_typename(lua_State luaState, int type);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern voidptr_t lua_upvalueid(lua_State luaState, int funcIndex, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_upvaluejoin(lua_State luaState, int funcIndex1, int n1, int funcIndex2, int n2);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Number lua_version(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void lua_warning(lua_State luaState, string msg, int tocont);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_xmove(lua_State from, lua_State to, int n);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_yieldk(lua_State luaState,
            int nresults,
            lua_KContext ctx,
            lua_KFunction k);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_argerror(lua_State luaState, int arg, string message);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_callmeta(lua_State luaState, int obj, string e);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_checkany(lua_State luaState, int arg);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Integer luaL_checkinteger(lua_State luaState, int arg);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern charptr_t luaL_checklstring(lua_State luaState, int arg, out size_t len);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Number luaL_checknumber(lua_State luaState, int arg);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_checkoption(lua_State luaState, int arg, string def, string[] list);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void luaL_checkstack(lua_State luaState, int sz, string message);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_checktype(lua_State luaState, int arg, int type);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern voidptr_t luaL_checkudata(lua_State luaState, int arg, string tName);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void luaL_checkversion_(lua_State luaState, lua_Number ver, size_t sz);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_error(lua_State luaState, string message);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaL_execresult(lua_State luaState, int stat);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_fileresult(lua_State luaState, int stat, string fileName);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_getmetafield(lua_State luaState, int obj, string e);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_getsubtable(lua_State luaState, int index, string name);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Integer luaL_len(lua_State luaState, int index);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_loadbufferx
            (lua_State luaState,
            byte[] buff,
            size_t sz,
            string name,
            string mode);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_loadfilex(lua_State luaState, string name, string mode);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_newmetatable(lua_State luaState, string name);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_State luaL_newstate();

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_openlibs(lua_State luaState);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Integer luaL_optinteger(lua_State luaState, int arg, lua_Integer d);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern lua_Number luaL_optnumber(lua_State luaState, int arg, lua_Number d);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int luaL_ref(lua_State luaState, int registryIndex);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void luaL_requiref(lua_State luaState, string moduleName, lua_CFunction openFunction, int global);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_setfuncs(lua_State luaState, [In] LuaRegister[] luaReg, int numUp);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void luaL_setmetatable(lua_State luaState, string tName);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern voidptr_t luaL_testudata(lua_State luaState, int arg, string tName);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern charptr_t luaL_tolstring(lua_State luaState, int index, out size_t len);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern charptr_t luaL_traceback
           (lua_State luaState,
            lua_State luaState2,
            string message,
            int level);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int luaL_typeerror(lua_State luaState, int arg, string typeName);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_unref(lua_State luaState, int registryIndex, int reference);

        [DllImport(LuaLibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void luaL_where(lua_State luaState, int level);

        #region "Externs"
        /// <summary>
        /// Pushes binary buffer onto the stack (usually UTF encoded string) or any arbitraty binary data
        /// </summary>
        /// <param name="buffer"></param>
        internal static void lua_pushbuffer(LuaState L, byte[] buffer)
        {
            if (buffer == null)
            {
                NativeMethods.lua_pushnil(L);
                return;
            }
            NativeMethods.lua_pushlstring(L, buffer, (UIntPtr)buffer.Length);
        }

        /// <summary>
        /// Pushes a string onto the stack
        /// </summary>
        /// <param name="value"></param>
        internal static void lua_pushstring(LuaState L, string value)
        {
            if (value == null)
            {
                NativeMethods.lua_pushnil(L);
                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(value);
            lua_pushbuffer(L, buffer);
        }

        /// <summary>
        /// Pushes a string onto the stack
        /// </summary>
        /// <param name="value"></param>
        internal static void lua_pushasciistring(LuaState L, string value)
        {
            if (value == null)
            {
                NativeMethods.lua_pushnil(L);
                return;
            }

            byte[] buffer = Encoding.ASCII.GetBytes(value);
            lua_pushbuffer(L, buffer);
        }

        /// <summary>
        /// Push a instring using string.Format 
        /// PushString("Foo {0}", 10);
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        internal static void lua_pushstring(LuaState L, string value, params object[] args)
        {
            lua_pushstring(L, string.Format(value, args));
        }

        /// <summary>
        /// Pops n elements from the stack. 
        /// </summary>
        /// <param name="n"></param>
        internal static void lua_pop(LuaState L, int n) => NativeMethods.lua_settop(L, -n - 1);

        /// <summary>
        /// Pushes a boolean value with value b onto the stack. 
        /// </summary>
        /// <param name="b"></param>
        internal static void lua_pushboolean(LuaState L, bool b) => NativeMethods.lua_pushboolean(L, b ? 1 : 0);

        /// <summary>
        /// Checks whether the function argument arg is a string and returns this string;
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        internal static byte[] lua_checkbuffer(LuaState L, int argument)
        {
            UIntPtr len;
            IntPtr buff = NativeMethods.luaL_checklstring(L, argument, out len);
            if (buff == IntPtr.Zero)
                return null;

            int length = (int)len;
            if (length == 0)
                return new byte[0];

            byte[] output = new byte[length];
            Marshal.Copy(buff, output, 0, length);
            return output;
        }

        /// <summary>
        /// Checks whether the function argument arg is a string and returns this string;
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        internal static string lua_checkstring(LuaState L, int argument)
        {
            byte[] buffer = lua_checkbuffer(L, argument);
            if (buffer == null)
                return null;
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Raises an error. The error message format is given by fmt plus any extra arguments
        /// </summary>
        /// <param name="value"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static int luaL_error(LuaState L, string value, params object[] v)
        {
            string message = string.Format(value, v);
            return NativeMethods.luaL_error(L, message);
        }

        /// <summary>
        /// Returns the raw "length" of the value at the given index: for strings, this is the string length; for tables, this is the result of the length operator ('#') with no metamethods; for userdata, this is the size of the block of memory allocated for the userdata; for other values, it is 0. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static int luaL_rawlen(LuaState L, int index)
        {
            return (int)NativeMethods.lua_rawlen(L, index);
        }

        /// <summary>
        /// Converts the Lua value at the given index to the signed integral type lua_Integer. The Lua value must be an integer, or a number or string convertible to an integer (see ��3.4.3); otherwise, lua_tointegerx returns 0. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static long lua_tointeger(LuaState L, int index)
        {
#pragma warning disable IDE0018 // Inline variable declaration
            int isNum;
#pragma warning restore IDE0018 // Inline variable declaration
            return NativeMethods.lua_tointegerx(L, index, out isNum);
        }

        internal static void luaL_newlib(LuaState L, LuaRegister[] library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library), "library shouldn't be null");
            NativeMethods.lua_createtable(L, 0, library.Length);
            NativeMethods.luaL_setfuncs(L, library, 0);
        }

        internal static LuaType luaL_type(lua_State L, int index)
        {
            return (LuaType)lua_type(L, index);
        }

        internal static byte[] lua_tobuffer(lua_State L, int index)
        {
            UIntPtr len;
            IntPtr buff = lua_tolstring(L, index, out len);

            if (buff == IntPtr.Zero)
                return null;

            int length = (int)len;
            if (length == 0)
                return new byte[0];

            byte[] output = new byte[length];
            Marshal.Copy(buff, output, 0, length);
            return output;
        }

        /// <summary>
        /// Converts the Lua value at the given index to a C# string
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static string lua_tostring(lua_State L, int index)
        {
            byte[] buffer = lua_tobuffer(L, index);
            if (buffer == null)
                return null;
            return Encoding.UTF8.GetString(buffer);
        }

        internal static bool luaL_isinteger(lua_State L, int index)
        {
            return lua_isinteger(L, index) == 1;
        }

        internal static bool luaL_toboolean(lua_State L, int index)
        {
            return lua_toboolean(L, index) == 1;
        }

        internal static double lua_tonumber(lua_State L, int index)
        {
            int isNum;
            return lua_tonumberx(L, index, out isNum);
        }

        internal static int __LuaTraceback(lua_State L)
        {
            string msg = lua_tostring(L, 1);
            if (null != msg)
            {
                luaL_traceback(L, L, msg, 1);
            }
            else
            {
                lua_pushstring(L, "(no error message)");
            }
            return 1;
        }

        internal static LuaFunction LuaTraceback = __LuaTraceback;

        internal static void lua_openlib(LuaState L, string name, LuaFunction fn, bool global = false)
        {
            luaL_requiref(L, name, fn.ToFunctionPointer(), global ? 1 : 0);
            lua_pop(L, 1);
        }

        /// <summary>
        /// Loads a buffer as a Lua chunk
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="name"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        internal static LuaStatus luaL_loadbuffer(LuaState L,  byte[] buffer, string name, string mode)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "buffer shouldn't be null");
            return (LuaStatus)luaL_loadbufferx(L, buffer, (UIntPtr)buffer.Length, name, mode);
        }

        /// <summary>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static LuaStatus luaL_loadbuffer(LuaState L, byte[] buffer, string name)
        {
            return luaL_loadbuffer(L, buffer, name, null);
        }

        /// <summary>
        /// Loads a buffer as a Lua chunk
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static LuaStatus luaL_loadbuffer(LuaState L, byte[] buffer)
        {
            return luaL_loadbuffer(L, buffer, null, null);
        }

        /// <summary>
        /// Loads a string as a Lua chunk
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static LuaStatus luaL_loadstring(LuaState L, string chunk, string name = null)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(chunk);
            return luaL_loadbuffer(L, buffer, name);
        }

        /// <summary>
        ///  Pushes a new C closure onto the stack. When a C function is created, it is possible to associate 
        ///  some values with it, thus creating a C closure (see ��4.4); these values are then accessible to the function 
        ///  whenever it is called. To associate values with a C function, first these values must be pushed onto the 
        ///  stack (when there are multiple values, the first value is pushed first). 
        ///  Then lua_pushcclosure is called to create and push the C function onto the stack, 
        ///  with the argument n telling how many values will be associated with the function. 
        ///  lua_pushcclosure also pops these values from the stack. 
        /// </summary>
        /// <param name="function"></param>
        /// <param name="n"></param>
        internal static void lua_pushcclosure(LuaState L, LuaFunction function, int n)
        {
            lua_pushcclosure(L, function.ToFunctionPointer(), n);
        }

        /// <summary>
        /// Pushes a C function onto the stack. This function receives a pointer to a C function and pushes onto the stack a Lua value of type function that, when called, invokes the corresponding C function. 
        /// </summary>
        /// <param name="function"></param>
        internal static void lua_pushcfunction(LuaState L, LuaFunction function)
        {
            lua_pushcclosure(L, function.ToFunctionPointer(), 0);
        }

        /// <summary>
        /// Loads a file as a Lua chunk. This function uses lua_load to load the chunk in the file named filename
        /// </summary>
        /// <param name="file"></param>
        /// <param name="mode"></param>
        /// <returns>The status of operation</returns>
        internal static LuaStatus luaL_loadfile(LuaState L, string file, string mode = null)
        {
            return (LuaStatus)luaL_loadfilex(L, file, mode);
        }

        internal static LuaStatus lua_pcall(LuaState L, int arguments, int results, int errorFunctionIndex)
        {
            return (LuaStatus)lua_pcallk(L, arguments, results, errorFunctionIndex, IntPtr.Zero, IntPtr.Zero);
        }

        internal static void lua_call(LuaState L, int arguments, int results)
        {
            lua_callk(L, arguments, results, IntPtr.Zero, IntPtr.Zero);
        }

        internal static void lua_closestate(LuaState L)
        {
            if (L == IntPtr.Zero)
                return;
            lua_close(L);
        }

        #endregion "Externs"
    }

#pragma warning restore CA2101 // Bug on CA + VS2017
#pragma warning restore IDE1006 // Naming Styles

#if !MONO_RUNTIME
    public class MonoPInvokeCallback : Attribute
    {
        private Type type;
        public MonoPInvokeCallback(Type t) { type = t; }
    }
#endif

    class LuaException : ApplicationException
    {
        public LuaException(string message) : base(message)
        {
        }
    }
}