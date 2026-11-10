-- Lua sample
local M = {}

--- Adds two numbers.
-- @param a number
function M.add(a, b)
  return a + b
end

local t = { name = "loom", [1] = 0x1F, ok = true, none = nil }
for i, v in ipairs(t) do
  print(string.format("%d: %s", i, tostring(v)))
end

--[[ block
comment ]]
local s = [[long
string]]
if #s > 3 and not t.ok then goto done end
::done::
return M
