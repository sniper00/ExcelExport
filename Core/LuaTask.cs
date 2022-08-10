using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using KeraLua;
using System.Threading.Tasks;
using LuaState = System.IntPtr;
using System.Runtime.InteropServices;

namespace LuaTask
{
    public enum PTYPE
    {
        Lua = 1,
        Error = 2,
        Timer = 3,
        Http = 4,
    }

    public enum LogLevel
    {
        Info = 1,
        Warn = 2,
        Error = 3,
    }

    public class Message
    {
        public PTYPE Type { get; set; }
        public long Sender { get; set; }
        public long Receiver { get; set; }
        public long Session { get; set; }
        public byte[] Data { get; set; }
    }

    class Timer
    {
        long timerUUID = 0;

        static long timeOffset = 0;

        struct TimerContext
        {
            public long timeStamp;
            public long timerId;
        }

        class TimerComparer : IComparer<TimerContext>
        {
            public int Compare(TimerContext x, TimerContext y)
            {
                if (x.timeStamp == y.timeStamp)
                {
                    return x.timerId.CompareTo(y.timerId);
                }
                return x.timeStamp.CompareTo(y.timeStamp);
            }
        }

        SortedSet<TimerContext> timers = new SortedSet<TimerContext>(new TimerComparer());

        public long Now()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds() + Interlocked.Read(ref timeOffset);
        }

        static public void SetOffset(long v)
        {
            Interlocked.Exchange(ref timeOffset, v);
        }

        public long Add(long mills)
        {
            mills += Now();

            long timerId = ++timerUUID;

            var ctx = new TimerContext
            {
                timeStamp = mills,
                timerId = timerId
            };

            timers.Add(ctx);
            return timerId;
        }

        public long Pop()
        {
            if (timers.Count == 0)
                return 0;

            var min = timers.Min;
            if (min.timeStamp <= Now())
            {
                var timerId = min.timerId;
                timers.Remove(min);
                return timerId;
            }
            return 0;
        }
    }

    public class LuaService:IDisposable
    {
        Task task;

        readonly BlockingCollection<Message> queue = new BlockingCollection<Message>();
        readonly CancellationTokenSource cancelToken = new CancellationTokenSource();
        readonly Timer timer = new Timer();

        private LuaState L;

        public TaskManager taskManager { get; }

        public long ID { get; }
        public string Name { get; }

        public LuaService(long id, string name, string file, TaskManager manager, LuaState v)
        {
            ID = id;
            Name = name;
            taskManager = manager;

            L = v;

            if (L == LuaState.Zero)
                L = LuaAPI.luaL_newstate();

            SetExtraObject(L, this, true);

            if (file != null)
            {
                taskManager.LuaEnvInit(L);
                LuaAPI.lua_pushcfunction(L, LuaAPI.LuaTraceback);
                int traceFn = LuaAPI.lua_gettop(L);
                LuaAPI.lua_pushcfunction(L, ProtectInit);
                LuaAPI.lua_pushstring(L, file);

                if (LuaAPI.lua_pcall(L, 1, -1, traceFn) != LuaStatus.OK || LuaAPI.lua_gettop(L) > 1)
                {
                    throw new LuaException(LuaAPI.lua_tostring(L, -1));
                }
                LuaAPI.lua_pop(L, 1);
            }

        }

        /// <summary>
        /// Finalizer, will dispose the lua state if wasn't closed
        /// </summary>
        ~LuaService()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose lua state
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        /// <summary>
        /// Dispose the lua context (calling Close)
        /// </summary>
#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            Dispose(true);
        }

        static int ProtectInit(IntPtr L)
        {
            LuaAPI.luaL_openlibs(L);
            string source = LuaAPI.lua_tostring(L, 1);
            if (LuaStatus.OK != LuaAPI.luaL_loadfile(L, source))
            {
                LuaAPI.lua_pushstring(L, "loadfile {0}", LuaAPI.lua_tostring(L, -1));
                return 1;
            }
            LuaAPI.lua_call(L, 0, 0);
            return 0;
        }

        private static void SetExtraObject<T>(LuaState L, T obj, bool weak) where T : class
        {
            var handle = GCHandle.Alloc(obj, weak ? GCHandleType.Weak : GCHandleType.Normal);
            IntPtr extraSpace = L - LuaState.Size;
            Marshal.WriteIntPtr(extraSpace, GCHandle.ToIntPtr(handle));
        }

        private static T GetExtraObject<T>(LuaState L) where T : class
        {
            IntPtr extraSpace = L - LuaState.Size;
            IntPtr pointer = Marshal.ReadIntPtr(extraSpace);
            var handle = GCHandle.FromIntPtr(pointer);
            if (!handle.IsAllocated)
                return null;

            return (T)handle.Target;
        }

        public static LuaService FromIntPtr(IntPtr L)
        {
            if (L == IntPtr.Zero)
                return null;
            LuaService state = GetExtraObject<LuaService>(L);
            if (state != null)
                return state;
            return null;
        }

        void HandleMessage(Message m)
        {
            int trace = 1;
            int top = LuaAPI.lua_gettop(L);
            if (top == 0)
            {
                LuaAPI.lua_pushcfunction(L, LuaAPI.LuaTraceback);
                LuaAPI.lua_rawgetp(L, (int)LuaRegistry.Index, L);
            }
            else
            {
                if (top != 2)
                {
                    throw new LuaException("failed");
                }
            }

            LuaAPI.lua_pushvalue(L, 2);

            LuaAPI.lua_pushinteger(L, m.Sender);
            LuaAPI.lua_pushbuffer(L, m.Data);
            LuaAPI.lua_pushinteger(L, m.Session);
            LuaAPI.lua_pushinteger(L, (int)m.Type);

            LuaStatus r = LuaAPI.lua_pcall(L, 4, 0, trace);
            if (LuaStatus.OK == r)
                return;

            string error = "unknown error";
            switch (r)
            {
                case LuaStatus.ErrRun:
                    error = string.Format("{0} error :\n{1}", Name, LuaAPI.lua_tostring(L, -1));
                    break;
                case LuaStatus.ErrMem:
                    error = string.Format("{0} memory error", Name);
                    break;
                case LuaStatus.ErrErr:
                    error = string.Format("{0} error in error", Name);
                    break;
            };

            LuaAPI.lua_pop(L, 1);

            if (m.Session == 0)
            {
                taskManager.Log(LogLevel.Error, error);
            }
            else
            {
                taskManager.SendMessage(0, m.Sender, System.Text.Encoding.Default.GetBytes(error), m.Session, PTYPE.Error);
            }
        }

        public void Run(string name)
        {
            task = Task.Run(() =>
            {
                Message mTimer = new Message();
                mTimer.Type = PTYPE.Timer;

                while (!cancelToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        //handle timer
                        while (true)
                        {
                            var timerId = timer.Pop();
                            if (timerId == 0)
                                break;
                            mTimer.Sender = timerId;
                            HandleMessage(mTimer);
                        }

                        //handle message
                        Message m;
                        while (queue.TryTake(out m, 10))
                        {
                            HandleMessage(m);
                        }
                    }
                    catch (Exception ex)
                    {
                        taskManager.Log(LogLevel.Error, ex.Message);
                    }
                }
            });
        }

        public void PushMessage(Message m)
        {
            queue.Add(m);
        }

        public bool SendMessage(long to, byte[] data, long session, PTYPE type)
        {
            return taskManager.SendMessage(ID, to, data, session, type);
        }

        public void Close(bool closeLua = true)
        {
            cancelToken.Cancel();
            if (task != null)
            {
                task.Wait();
            }
            if (closeLua)
            {
                LuaAPI.lua_closestate(L);
                L = IntPtr.Zero;
            }
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static public int SendMessage(LuaState L)
        {
            LuaService S = FromIntPtr(L);
            bool ok = S.SendMessage(LuaAPI.luaL_checkinteger(L, 1), LuaAPI.lua_checkbuffer(L, 2), LuaAPI.luaL_checkinteger(L, 3), (PTYPE)LuaAPI.luaL_checkinteger(L, 4));
            LuaAPI.lua_pushboolean(L, ok);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static int PopMessage(LuaState L)
        {
            LuaService S = FromIntPtr(L);

            Message m;
            if (S.queue.TryTake(out m))
            {
                LuaAPI.lua_pushinteger(L, m.Sender);
                LuaAPI.lua_pushbuffer(L, m.Data);
                LuaAPI.lua_pushinteger(L, m.Session);
                LuaAPI.lua_pushinteger(L, (int)m.Type);
                return 4;
            }

            long timerId = S.timer.Pop();
            if (timerId > 0)
            {
                LuaAPI.lua_pushinteger(L, timerId);
                LuaAPI.lua_pushnil(L);
                LuaAPI.lua_pushinteger(L, 0);
                LuaAPI.lua_pushinteger(L, (int)PTYPE.Timer);
                return 4;
            }
            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static int FindService(LuaState L)
        {
            LuaService S = FromIntPtr(L);

            var name = LuaAPI.lua_checkstring(L, 1);
            long ID = S.taskManager.FindService(name);
            if (ID > 0)
            {
                LuaAPI.lua_pushinteger(L, ID);
                return 1;
            }
            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static int AddTimer(LuaState L)
        {
            LuaService S = FromIntPtr(L);
            var mills = LuaAPI.luaL_checkinteger(L, 1);
            var timerId = S.timer.Add(mills);
            LuaAPI.lua_pushinteger(L, timerId);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static public int Log(LuaState L)
        {
            LuaService S = FromIntPtr(L);
            var lv = (LogLevel)LuaAPI.luaL_checkinteger(L, 1);
            var logline = LuaAPI.lua_checkstring(L, 2);
            S.taskManager.Log(lv, logline);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaFunction))]
        static public int SetCallBack(LuaState L)
        {
            LuaAPI.luaL_checktype(L, 1, (int)LuaType.Function);
            LuaAPI.lua_settop(L, 1);
            LuaAPI.lua_rawsetp(L, (int)LuaRegistry.Index, L);
            return 0;
        }

        private static readonly LuaRegister[] l = new LuaRegister[]
        {
            new LuaRegister{name ="log", function = Log},
            new LuaRegister{name ="send", function = SendMessage},
            new LuaRegister{name ="pop", function = PopMessage},
            new LuaRegister{name ="find", function = FindService},
            new LuaRegister{name ="callback", function = SetCallBack},
            new LuaRegister{name ="timeout", function = AddTimer},
            new LuaRegister{name = null, function = null}
        };

        [MonoPInvokeCallback(typeof(LuaFunction))]
        public static int OpenLib(IntPtr L)
        {
            var S = LuaService.FromIntPtr(L);
            LuaAPI.lua_createtable(L, 0, l.Length+ 2);
            LuaAPI.luaL_setfuncs(L, l, 0);
            LuaAPI.lua_pushstring(L, "name");
            LuaAPI.lua_pushstring(L, S.Name);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.lua_pushstring(L, "id");
            LuaAPI.lua_pushinteger(L, S.ID);
            LuaAPI.lua_rawset(L, -3);
            return 1;
        }
    }


    public class TaskManager
    {
        //Set custom loader, openlibs
        public Action<LuaState> LuaEnvInit;

        public Action<LogLevel, string> Log;
        readonly ConcurrentDictionary<long, LuaService> luaServices = new ConcurrentDictionary<long, LuaService>();

        const long MainID = 1;

        long serviceUUID = 1;

        public TaskManager(LuaState L)
        {
            Log = (lv, line) =>
            {
#if !XLUA_GENERAL
                switch (lv)
                {
                    case LogLevel.Warn:
                        {
                            Debug.LogWarning(line);
                            break;
                        }
                    case LogLevel.Error:
                        {
                            Debug.LogError(line);
                            break;
                        }
                    default:
                        {
                            Debug.Log(line);
                            break;
                        }
                }
#else
                var oldColor = Console.ForegroundColor;
                switch (lv)
                {
                    case LogLevel.Warn:
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        }
                    case LogLevel.Error:
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        }
                    default:
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                }
                Console.WriteLine(line);
                Console.ForegroundColor = oldColor;
#endif
            };
            luaServices.TryAdd(MainID, new LuaService(MainID, "MainTask", null, this, L));
        }

        public bool SendMessage(long from, long to, byte[] data, long session, PTYPE type)
        {
            Message m = new Message
            {
                Sender = from,
                Receiver = to,
                Data = data,
                Session = -session,
                Type = type
            };

            LuaService S;
            if (luaServices.TryGetValue(m.Receiver, out S))
            {
                S.PushMessage(m);
                return true;
            }
            return false;
        }

        public long FindService(string name)
        {
            foreach (var v in luaServices.Values)
            {
                if (v.Name == name)
                {
                    return v.ID;
                }
            }
            return 0;
        }

        public void Shutdown()
        {
            foreach (var v in luaServices.Values)
            {
                v.Close(v.ID != MainID);
                luaServices.TryRemove(v.ID, out _);
            }
        }

        static int NewService(IntPtr L)
        {
            LuaService S = LuaService.FromIntPtr(L);
            try
            {
                var taskManager = S.taskManager;
                var name = LuaAPI.lua_checkstring(L, 1);
                var source = LuaAPI.lua_checkstring(L, 2);
                var id = Interlocked.Increment(ref taskManager.serviceUUID);
                var service = new LuaService(id, name, source, taskManager, LuaState.Zero);
                service.Run(source);
                taskManager.luaServices.TryAdd(id, service);
                LuaAPI.lua_pushinteger(L, id);
                return 1;
            }
            catch (Exception ex)
            {
                return LuaAPI.luaL_error(L, ex.Message);
            }
        }

        static int RemoveService(IntPtr L)
        {
            LuaService S = LuaService.FromIntPtr(L);
            var taskManager = S.taskManager;

            var id = LuaAPI.luaL_checkinteger(L, 1);
            if (taskManager.luaServices.TryRemove(id, out LuaService rm))
            {
                rm.Close();
            }
            return 0;
        }

        static int Shutdown(IntPtr L)
        {
            LuaService S = LuaService.FromIntPtr(L);
            var taskManager = S.taskManager;
            taskManager.Shutdown();
            return 0;
        }

        static int SetTimeOffset(IntPtr L)
        {
            var mills = LuaAPI.luaL_checkinteger(L, 1);
            Timer.SetOffset(mills);
            return 0;
        }

        static int ThreadSleep(IntPtr L)
        {
            var mills = LuaAPI.luaL_checkinteger(L, 1);
            Thread.Sleep((int)mills);
            return 0;
        }

        static readonly LuaRegister[] l = new LuaRegister[]
         {
            new LuaRegister{name ="new", function = NewService},
            new LuaRegister{name ="remove", function = RemoveService},
            new LuaRegister{name ="shutdown", function = Shutdown},
            new LuaRegister{name ="set_time_offset", function = SetTimeOffset},
            new LuaRegister{name ="thread_sleep", function = ThreadSleep},
            new LuaRegister{name = null, function = null}
         };

        [MonoPInvokeCallback(typeof(LuaFunction))]
        public static int OpenLib(IntPtr L)
        {
            LuaAPI.luaL_newlib(L, l);
            return 1;
        }
    }
}

