local core = require("task.core")

local MAIN_ID = 1

local manager_core
if core.id == MAIN_ID then
    manager_core = require("task.manager.core")
end

local pairs = pairs
local type = type
local error = error
local tremove = table.remove
local traceback = debug.traceback

local co_create = coroutine.create
local co_running = coroutine.running
local co_yield = coroutine.yield
local co_resume = coroutine.resume
local co_close = coroutine.close or function () end

---@class LuaTask
local task = {
    name = core.name,
    id = core.id
}

local LOG_INFO = 1
local LOG_WARN = 2
local LOG_ERROR = 3

local function format_log(...)
    local level = 3
    local info = debug.getinfo(level, "Sl")
    local t = {}
    -- t[#t+1] = string.format("[%s]", core.name)
    t[#t+1] = os.date("[%Y-%m-%d %H:%M:%S]")..string.format("[%04x]", core.id)
    for _, v in ipairs({...}) do
        t[#t+1] = tostring(v)
    end
    t[#t+1] =string.format("(%s:%d)", info.source, info.currentline)
    return t
end

task.print = function (...)
    core.log(LOG_INFO, table.concat(format_log(...),'\t'))
end

task.warn = function (...)
    core.log(LOG_WARN, table.concat(format_log(...),'\t'))
end

task.error = function (...)
    core.log(LOG_ERROR, table.concat(format_log(...),'\t'))
end

task.PTYPE_LUA = 1
task.PTYPE_ERROR = 2
task.PTYPE_TIMER = 3

task.MAIN_TASK_NAME = "MainTask"

local uuid = 0
local session_id_coroutine = {}
local protocol = {}
local session_watcher = {}
local timer_routine = {}

local function coresume(co, ...)
    local ok, err = co_resume(co, ...)
    if not ok then
        err = traceback(co, tostring(err))
        co_close(co)
        error(err)
    end
    return ok, err
end

---make map<coroutine,sessionid>
local function make_response(receiver)
    uuid = uuid + 1
    if uuid == 0x7FFFFFFF then
        uuid = 1
    end

    if nil ~= session_id_coroutine[uuid] then
        error("sessionid is used!")
    end

    if receiver then
        session_watcher[uuid] = receiver
    end

    session_id_coroutine[uuid] = co_running()
    return uuid
end

function task.cancel_session(sessionid)
    session_id_coroutine[sessionid] = false
end

task.make_response = make_response

-------------------------协程操作封装--------------------------

local co_num = 0

local co_pool = setmetatable({}, {__mode = "kv"})

local function invoke(co, fn, ...)
    co_num = co_num + 1
    fn(...)
    co_num = co_num - 1
    co_pool[#co_pool + 1] = co
end

local function routine(fn, ...)
    local co = co_running()
    invoke(co, fn, ...)
    while true do
        invoke(co, co_yield())
    end
end

---Creates a new coroutine(from coroutine pool) and start it immediately.
---If `func` lacks call `coroutine.yield`, will run syncronously.
---@param func fun()
---@return thread
function task.async(func, ...)
    local co = tremove(co_pool)
    if not co then
        co = co_create(routine)
    end
    coresume(co, func, ...)
    return co
end

function task.wakeup(co, ...)
    local args = {...}
    task.timeout(0, function()
        local ok, err = co_resume(co, table.unpack(args))
        if not ok then
            err = traceback(co, tostring(err))
            co_close(co)
            task.error(err)
        end
    end)
end

---return count of running coroutine and total coroutine in coroutine pool
function task.coroutine_num()
    return co_num, #co_pool
end

--- Send message to target service (id=receiver), and use `coroutine.yield()` wait response
---  - If success, return values are params of `task.response(id,response, params...)`
---  - If failed, return `false` and `error message(string)`
---@param PTYPE string @protocol type
---@param receiver integer @receiver task's id
---@return any|boolean,string
function task.call(PTYPE, receiver, ...)
    local p = protocol[PTYPE]
    if not p then
        error(string.format("task call unknown PTYPE[%s] message", PTYPE))
    end

    if receiver == 0 then
        error("task co_call receiver == 0")
    end

    local sessionid = make_response(receiver)
	core.send(receiver, p.pack(...), sessionid, p.PTYPE)
    return co_yield()
end

--- Response message to the sender of `task.call`
---@param PTYPE string @protocol type
---@param receiver integer @receiver task's id
---@param sessionid integer
function task.response(PTYPE, receiver, sessionid, ...)
    if sessionid == 0 then return end
    local p = protocol[PTYPE]
    if not p then
        error("handle unknown message")
    end

    if receiver == 0 then
        error("task response receiver == 0")
    end

    core.send(receiver, p.pack(...), sessionid, p.PTYPE)
end

---
---Send message to target service (id=receiver)
---@param PTYPE string @protocol type
---@param receiver integer @receiver task's id
---@return boolean
function task.send(PTYPE, receiver, ...)
    local p = protocol[PTYPE]
    if not p then
        error(string.format("task send unknown PTYPE[%s] message", PTYPE))
    end

    core.send(receiver, p.pack(...), 0, p.PTYPE)
    return true
end


------------------------------------

local function _default_dispatch(sender, data, session, PTYPE)
    --task.print(core.name..":recv", sender, data, session, PTYPE)
    local p = protocol[PTYPE]
    if not p then
        error(string.format( "handle unknown PTYPE: %s. sender %u",PTYPE, sender))
    end

    if session > 0 and PTYPE ~= task.PTYPE_ERROR then
        session_watcher[session] = nil
        local co = session_id_coroutine[session]
        if co then
            session_id_coroutine[session] = nil
            --task.print(coroutine.status(co))
            if p.unpack then
                coresume(co, p.unpack(data))
            else
                coresume(co, data)
            end
            --task.print(coroutine.status(co))
            return
        end
        if co ~= false then
            error(string.format( "%s: response [%u] can not find co.", task.name, session))
        end
    else
        local dispatch = p.dispatch
        if not dispatch then
            error(string.format( "[%s] dispatch PTYPE [%u] is nil",task.name, p.PTYPE))
            return
        end
        if not p.israw and p.unpack then
            local co = tremove(co_pool)
            if not co then
                co = co_create(routine)
            end
            coresume(co, dispatch, sender, session, p.unpack(data))
        else
            dispatch(sender, session, data)
        end
    end
end

if core.id == MAIN_ID then -- main task
    ---main task must call this, handle message
    function task.update()
        while true do
            local sender, data, session, mtype = core.pop()
            if sender then
                --task.print(sender, data, session, mtype)
                _default_dispatch(sender, data, session, mtype )
            else
                break
            end
        end
    end

    ---comment
    ---@param name string
    ---@param modName string @ like require's param
    ---@return integer @return task's id.
    function task.new(name, modName)
        return manager_core.new(name, modName)
    end

    function task.remove(id)
        return manager_core.remove(id)
    end

    function task.close_all()
        manager_core.close_all()
    end

    function task.thread_sleep(n)
        return manager_core.thread_sleep(n)
    end
else
    core.callback(_default_dispatch)
end

function task.find(name)
    return core.find(name)
end

function task.register_protocol(t)
    local PTYPE = t.PTYPE
    if protocol[PTYPE] then
        task.print("Warning attemp register duplicated PTYPE", t.name)
    end
    protocol[PTYPE] = t
    protocol[t.name] = t
end

local reg_protocol = task.register_protocol

---@param PTYPE string
---@param fn fun(sender:integer, session:integer, ...)
---@return boolean
function task.dispatch(PTYPE, fn, israw)
    local p = protocol[PTYPE]
    if fn then
        local ret = p.dispatch
        p.dispatch = fn
        p.israw = israw
        return ret
    else
        return p and p.dispatch
    end
end

local function default_pack(...)
    return ...
end

local function default_unpack(...)
    return ...
end

do
    local tostring = tostring
    local getmetatable = getmetatable
    local load = load
    local strfmt = string.format
    local tconcat = table.concat
    local tunpack = table.unpack

    local M = {}

    local function serialize(...)
        local function pack_one(obj, tcache)
            local t = type(obj)
            if t == "number" then
                tcache[#tcache+1] = tostring(obj)
            elseif t == "boolean" then
                tcache[#tcache+1] = tostring(obj)
            elseif t == "string" then
                tcache[#tcache+1] = strfmt("%q", obj)
            elseif t == "table" then
                tcache[#tcache+1] = "{\n"
                for k, v in pairs(obj) do
                    tcache[#tcache+1] = "["
                    pack_one(k, tcache)
                    tcache[#tcache+1] = "]="
                    pack_one(v, tcache)
                    tcache[#tcache+1] = ",\n"
                end
                local metatable = getmetatable(obj)
                if metatable ~= nil and type(metatable.__index) == "table" then
                    for k, v in pairs(metatable.__index) do
                        tcache[#tcache+1] = "["
                        pack_one(k, tcache)
                        tcache[#tcache+1] = "]="
                        pack_one(v, tcache)
                        tcache[#tcache+1] = ",\n"
                    end
                end
                tcache[#tcache+1] = "}"
            elseif t == "nil" then
                return "nil"
            else
                error("can not serialize a " .. t .. " type.")
            end
            return tcache
        end

        local tcache = {}
        pack_one({...}, tcache)
        return tconcat(tcache, "")
    end

    local function unserialize(lua)
        local t = type(lua)
        if t == "nil" or lua == "" then
            return nil
        elseif t == "number" or t == "string" or t == "boolean" then
            lua = tostring(lua)
        else
            error("can not unserialize a " .. t .. " type.")
        end
        lua = "return " .. lua
        local func = load(lua)
        if func == nil then return nil end
        return tunpack(func())
    end

    reg_protocol {
        name = "lua",
        PTYPE = task.PTYPE_LUA,
        pack = serialize,
        unpack = unserialize,
        dispatch = function()
            error("PTYPE_LUA dispatch not implemented")
        end
    }
end

reg_protocol {
    name = "error",
    PTYPE = task.PTYPE_ERROR,
    pack = default_pack,
    dispatch = function(sender, session, data)
        local co = session_id_coroutine[session]
        if co then
            session_id_coroutine[session] = nil
            coresume(co, false, data)
            return
        end
    end
}

--------------------------timer-------------

reg_protocol {
    name = "timer",
    PTYPE = task.PTYPE_TIMER,
    dispatch = function(sender, session, data)
        local timerid = sender
        local v = timer_routine[timerid]
        timer_routine[timerid] = nil
        if not v then
            return
        end
        if type(v) == "thread" then
            coresume(v, timerid)
        elseif v then
            v()
        end
    end
}

---@param timerid integer @
function task.remove_timer(timerid)
    timer_routine[timerid] = false
end

function task.timeout(mills, fn)
    local timer_session = core.timeout(mills)
    timer_routine[timer_session] = fn
    return timer_session
end

--- # async
--- - coroutine sleep mills seconds
---@param mills integer
---@return integer
function task.sleep(mills)
    local timer_session = core.timeout(mills)
    timer_routine[timer_session] = co_running()
    return co_yield()
end

--- 等待多个异步调用，并获取结果
function task.wait_all(fnlist)
    local n = #fnlist
    local res = {}
    if n == 0 then
        return res
    end
    local co = coroutine.running()
    task.timeout(0, function()
        for i,fn in ipairs(fnlist) do
            task.async(function ()
                local one = {xpcall(fn, traceback)}
                if one[1] then
                    table.remove(one, 1)
                end
                res[i] = one
                n=n-1
                if n==0 then
                    if coroutine.status(co) == "suspended" then
                        task.wakeup(co)
                    end
                end
            end)
        end
    end)
    coroutine.yield()
    return res
end


return task