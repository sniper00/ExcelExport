local excel = require("excel")
local fs = require("fs")
local json = require("json")
local task = require("task")
local util = require("util")

local print = task.print

local error_count = 0
local function fatil(...)
    error_count = error_count + 1
    task.error(...)
end

local IgnoreFiles = {
    ["xxxx.xlsx"] = true
}

if task.name == "MainTask" then

    local args = ...
    local InputDir = args[2]
    local OutputDir = args[3]

    print("Start export server config")
    local begin_time = os.clock()

    fs.createdirs(OutputDir)

    local excelfiles = fs.listdir(InputDir, "*.xlsx")

    local address = {}
    local files = {}

    local worker_count = util.cpu()

    for i = 1, worker_count do
        address[i] = task.new("work" .. i, "main.lua")
        print("new worker", "work" .. i)
        files[i] = {}
    end

    for k, file in ipairs(excelfiles) do
        table.insert(files[(k % worker_count) + 1], file)
    end

    local exit = false

    task.async(function()
        local fn = {}
        local total_error_count = 0

        for k, id in ipairs(address) do
            ---给子线程分配任务
            fn[k] = function()
                local n, err = task.call("lua", id, "run", files[k], OutputDir)
                if not n then
                    task.error(err)
                end
                total_error_count = total_error_count + n
            end
        end

        ---等待所有子线程任务完成
        task.wait_all(fn)

        if total_error_count > 0 then
            fatil("has error, check it", total_error_count)
            os.exit(-1)
        end

        -- local message = {
        --     msgtype = "text",
        --     text = {
        --         content = table.concat({"error"}, "\n")
        --     }
        -- }

        -- local uri = "https://www.baidu.com"
        -- print_r(task.http_post_json(uri, message))

        print("Export config success. cost", (os.clock() - begin_time) .. "s")
        exit = true
    end)

    while not exit do
        task.update()
        task.thread_sleep(1)
    end
else
    local CALLBACK = {}

    task.dispatch('lua',function(sender, session, cmd, ...)
        local fn = CALLBACK[cmd]

        local res = {xpcall(fn, debug.traceback, ...)}

        if res[1] then
            table.remove(res,1)
        end
        task.response("lua", sender, session, table.unpack(res))
    end)

    CALLBACK["run"] = function (excelfiles, OutputDir)
        local strgsub   = string.gsub
        local tbinsert  = table.insert
        local tointeger = math.tointeger
        local strfmt    = string.format
        local strrep    = string.rep
        local tsort     = table.sort
        local tbconcat  = table.concat

        local function strtrim(input, chars)
            chars = chars or " \t\n\r"
            local pattern = "^[" .. chars .. "]+"
            input = strgsub(input, pattern, "")
            pattern = "[" .. chars .. "]+$"
            return strgsub(input, pattern, "")
        end

        local function tkeys(tbl)
            local keys = {}
            for k, _ in pairs(tbl) do
                tbinsert(keys, k)
            end
            return keys
        end

        local function writefile(filename, content)
            local file = io.open(filename, "w+b")
            if file then
                file:write(content)
                file:close()
                return true
            end
        end

        ---raw data read from excel
        local rawdatas = {}

        local formatdatas = {}

        local publishdata = {}

        local rowmt = {}

        local arrmt = {}

        local custommt = {}

        for _, file in ipairs(excelfiles) do
            local name = fs.stem(file)
            if string.sub(name, 1, 2) ~= "~$" then
                local res, err = excel.read(file)
                if not res then
                    fatil(err)
                    return
                end
                if not IgnoreFiles[name] then
                    rawdatas[name] = res
                end
            end
        end

        local TypeTraits = {}

        TypeTraits["int"] = {
            luat = "integer",
            decode = function(v)
                if v and #tostring(v) > 0 then
                    return assert(math.tointeger(v))
                end
                return 0
            end
        }

        TypeTraits["long"] = TypeTraits["int"]

        TypeTraits["string"] = {
            luat = "string",
            decode = function(v)
                if v then
                    return tostring(v)
                end
                return ""
            end
        }

        TypeTraits["float"] = {
            luat = "number",
            decode = function(v)
                if v then
                    return tonumber(v)
                end
                return 0.0
            end
        }

        TypeTraits["bool"] = {
            luat = "boolean",
            decode = function(v)
                if v then
                    if type(v) == "string" then
                        return string.lower(v) == 'true'
                    elseif type(v) == "number" then
                        return v > 0
                    elseif type(v) == "boolean" then
                        return v
                    end
                end
                return false
            end
        }

        TypeTraits["int[]"] = {
            luat = "integer[]",
            decode = function(data)
                data = strtrim(tostring(data))
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local tb = {}
                local _ = strgsub(data, '%-?%d+', function(w)
                    tbinsert(tb, assert(tointeger(w)))
                end)
                local res = { '{' }
                for _, v in ipairs(tb) do
                    if _ ~= 1 then
                        res[#res + 1] = ","
                    end
                    res[#res + 1] = tostring(v)
                end
                res[#res + 1] = "}"
                return setmetatable(res, custommt)
            end
        }

        TypeTraits["float[]"] = {
            luat = "number[]",
            decode = function(data)
                data = strtrim(tostring(data))
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local tb = {}
                local _ = strgsub(data, '[^|]+', function(w)
                    tbinsert(tb, assert(tonumber(w)))
                end)
                local res = { '{' }
                for _, v in ipairs(tb) do
                    if _ ~= 1 then
                        res[#res + 1] = ","
                    end
                    res[#res + 1] = tostring(v)
                end
                res[#res + 1] = "}"
                return setmetatable(res, custommt)
            end
        }

        TypeTraits["int[][]"] = {
            luat = "integer[][]",
            decode = function(data)
                data = strtrim(tostring(data))
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local tb = {}
                local _ = strgsub(data, '[^|]+', function(w)
                    tbinsert(tb, w)
                end)
                local res = { '{' }
                for _, v in ipairs(tb) do
                    if _ ~= 1 then
                        res[#res + 1] = ","
                    end
                    res[#res + 1] = table.concat(TypeTraits["[int]"].decode(v))
                end
                res[#res + 1] = "}"
                return setmetatable(res, custommt)
            end
        }

        TypeTraits["script"] = {
            luat = "table",
            decode = function(data)
                if not data then
                    return {}
                end
                data = "return " .. data
                return load(data)()
            end
        }

        TypeTraits["raw"] = {
            luat = "table",
            decode = function(data)
                data = tostring(data)
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local check = "return {" .. data .. "}"
                load(check)()
                return setmetatable({ "{" .. data .. "}" }, custommt)
            end
        }

        TypeTraits["raw[]"] = {
            luat = "string",
            decode = function(data, filename, colname)
                data = tostring(data)
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local check = "return {" .. data .. "}"
                local fn = load(check)
                local ok = xpcall(fn, debug.traceback)
                if not ok then
                    fatil(strfmt("Excel file '%s' col '%s' unsupport data format, maybe server not need this field"
                        , filename, colname))
                    return data
                end
                return setmetatable({ "{" .. data .. "}" }, custommt)
            end
        }

        TypeTraits["mapi"] = {
            luat = "table",
            decode = function(data)
                data = tostring(data)
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local tb = {}
                local _ = strgsub(data, '[^|]+', function(w)
                    local t = {}
                    local _ = strgsub(w, '[^;,]+', function(r)
                        tbinsert(t, tointeger(r))
                    end)
                    assert(#t == 2, "map need key-value")
                    tb[t[1]] = t[2]
                end)

                local res = { '{' }
                local idx = 1
                for k, v in pairs(tb) do
                    if idx ~= 1 then
                        res[#res + 1] = ","
                    end
                    res[#res + 1] = strfmt("[%s] = %s", tostring(k), tostring(v))
                    idx = idx + 1
                end
                res[#res + 1] = "}"
                return setmetatable(res, custommt)
            end
        }

        TypeTraits["string[]"] = {
            luat = "string[]",
            decode = function(data)
                data = strtrim(tostring(data))
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local tb = {}
                local _ = strgsub(data, '[^|]+', function(w)
                    tbinsert(tb, w)
                end)
                local res = { '{' }
                for _, v in ipairs(tb) do
                    if _ ~= 1 then
                        res[#res + 1] = ","
                    end
                    res[#res + 1] = '"' .. tostring(v) .. '"'
                end
                res[#res + 1] = "}"
                return setmetatable(res, custommt)
            end
        }

        TypeTraits["date"] = {
            luat = "tm",
            decode = function(data)
                data = tostring(data)
                if #data == 0 or data == "nil" then
                    return setmetatable({ "empty" }, custommt), true
                end
                local rep = "{year=%1,month=%2,day=%3,hour=%4,min=%5,sec=%6}"
                return setmetatable({ (string.gsub(data, "(%d+)[/-](%d+)[/-](%d+) (%d+):(%d+):(%d+)", rep)) }, custommt)
            end
        }

        local function trim_field(t)
            for k, v in ipairs(t) do
                t[k] = strtrim(v)
            end
            return t
        end

        --[[
    data format:
    1. comments
    2. colname
    3. datatype
    4. options
]]
        local function FormatOne(rawdata, filename)
            --print_r(rawdata)
            local comments = rawdata[1]
            local colname = trim_field(rawdata[2])
            local datatype = trim_field(rawdata[3])
            local options = {} -- rawdata[4]

            local startline = 4

            if datatype[1] == nil then
                return
            end

            local hasEmptyTable = false

            local resTb = {}
            for i = startline, #rawdata do
                local row = rawdata[i]
                local key
                local onerow = {}
                if row[1] then
                    for idx = 1, #colname do
                        local name = colname[idx]
                        if name then
                            if (not options[idx]) then
                                local value = row[idx]
                                --print(filename, name, value, datatype[idx])
                                local trait = TypeTraits[datatype[idx]]
                                if trait and trait.decode then
                                    local ok, res, empty = xpcall(trait.decode, debug.traceback, value, filename, name)
                                    if not ok then
                                        fatil(strfmt("Excel file '%s' col '%s' rawdata %s format error: %s",
                                            filename, name, tostring(value), res))
                                    else
                                        if not hasEmptyTable then
                                            hasEmptyTable = empty
                                        end
                                        value = res
                                        onerow[name] = value
                                        --- col 1 is key
                                        if idx == 1 then
                                            key = value
                                        end
                                    end
                                else
                                    colname[idx] = tostring(idx)
                                end
                            else
                                if idx == 1 then
                                    return
                                end
                            end
                        else
                            colname[idx] = tostring(idx)
                        end
                    end

                    if key then
                        if resTb[key] then
                            fatil(strfmt("Excel file '%s' row %d key duplicate!", filename, i))
                        else
                            resTb[key] = setmetatable(onerow, rowmt)
                        end
                    end
                end
            end

            return {
                comments = comments,
                datatype = datatype,
                colname = colname,
                data = resTb,
                options = options,
                hasEmptyTable = hasEmptyTable
            }
        end

        for name, data in pairs(rawdatas) do
            local res = FormatOne(data[1], name)
            if res then
                formatdatas[name] = res
            else
                task.warn(strfmt("Excel file '%s' main key is null, will skipped!", name))
            end
        end

        local SpaceFindTable = {}
        for i = 1, 32 do
            SpaceFindTable[i] = strrep("\t", i)
        end

        local function write(name, formatdata, direct)

            local order = formatdata.colname
            local datatype = formatdata.datatype
            local comments = formatdata.comments
            local options = formatdata.options

            local function write_value(v)
                if type(v) == "string" then
                    if string.find(v, "%c") then
                        v = "[[" .. v .. "]]"
                    else
                        v = "\'" .. v .. "\'"
                    end
                end
                return tostring(v)
            end

            local function write_key(v)
                if type(v) == "number" then
                    v = "[" .. v .. "]"
                end
                return tostring(v)
            end

            local result = {}

            if not direct then
                result[#result + 1] = strfmt("---@class %s_cfg", name)
                for k, v in ipairs(order) do
                    if not v or not datatype[k] then
                        task.warn(strfmt("Excel file '%s' col '%s' has unsupport datatype '%s' ,skipped!", name, v,
                            datatype[k]))
                    else
                        if comments[k] then
                            if not options or options[k] ~= "options" then
                                assert(datatype[k], tostring(k))
                                assert(comments[k], tostring(k))
                                assert(TypeTraits[datatype[k]], name .. ":" .. datatype[k])
                                result[#result + 1] = strfmt("---@field public %s %s @%s", v,
                                    TypeTraits[datatype[k]].luat, strgsub(comments[k], "%c", ""))
                            end
                        else
                            task.warn(strfmt("Excel file '%s' col '%s' has no comments.", name, v, datatype[k]))
                            result[#result + 1] = strfmt("---@field public %s %s", v, TypeTraits[datatype[k]].luat)
                        end
                    end
                end
            end

            if formatdata.hasEmptyTable then
                result[#result + 1] = "\nlocal empty = {}\n"
            else
                result[#result + 1] = ""
            end


            result[#result + 1] = "local M = {"

            local function write_one(k, v, nspace, first)
                local tp = type(v)
                if tp ~= "table" then
                    result[#result + 1] = strfmt("%s%s = %s,", SpaceFindTable[nspace], write_key(k), write_value(v))
                elseif not direct and getmetatable(v) == custommt then
                    result[#result + 1] = strfmt("%s%s = %s,", SpaceFindTable[nspace], write_key(k), table.concat(v))
                else
                    if not first then
                        result[#result + 1] = strfmt("%s%s = {", SpaceFindTable[nspace], write_key(k))
                    end
                    local keys
                    if getmetatable(v) == rowmt then
                        keys = order
                    else
                        keys = tkeys(v)
                        tsort(keys, function(a, b)
                            if type(a) == "number" and type(b) == "number" then
                                return a < b
                            else
                                return tostring(a) < tostring(b)
                            end
                        end)
                    end
                    for _, _k in ipairs(keys) do
                        local _v = v[_k]
                        if _v ~= nil then
                            write_one(_k, _v, nspace + 1)
                        end
                    end
                    if not first then
                        result[#result + 1] = strfmt("%s},", SpaceFindTable[nspace])
                    end
                end
            end

            write_one("", formatdata.data, 0, true)

            result[#result + 1] = "}\nreturn M\n"
            return tbconcat(result, "\n")
        end

        for name, formatdata in pairs(formatdatas) do
            local content = write(name, formatdata)
            if #content > 0 then
                local fn = assert(load(content, name .. ".lua"), content)
                publishdata[name] = { data = fn(), content = content }
            end
        end

        local filter = {}

        for name, v in pairs(publishdata) do
            local fn = filter[name]
            if fn then
                print("prepare", name)
                fn(v)
            end
            -- print_r(v.data)
            writefile(fs.join(OutputDir, name .. ".lua"), v.content)
        end

        return error_count
    end
end

