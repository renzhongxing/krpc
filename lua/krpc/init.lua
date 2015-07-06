local krpc = {}

-- TODO: move this elsewhere
package.path = package.path .. ';../../protoc-gen-lua/protobuf/?.lua'
package.cpath = package.cpath .. ';../../protoc-gen-lua/protobuf/?.so'

local socket = require "socket"
local decoder = require "krpc.decoder"
local Client = require "krpc.client"

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000
local DEFAULT_STREAM_PORT = 50001

local CLIENT_NAME_LENGTH = 32
local CLIENT_IDENTIFIER_LENGTH = 16

function krpc.connect(address, rpcPort, streamPort, name)

  address = address or DEFAULT_ADDRESS
  rpcPort = rpcPort or DEFAULT_RPC_PORT
  streamPort = streamPort or DEFAULT_STREAM_PORT

  -- assert rpcPort != streamPort

  -- Connect to RPC server
  rpc_connection = socket.tcp()
  rpc_connection:connect(address, rpcPort)
  rpc_connection:send("\x48\x45\x4C\x4C\x4F\x2D\x52\x50\x43\x00\x00\x00")
  name = name:sub(1,CLIENT_NAME_LENGTH)
  rpc_connection:send(name .. string.rep("\x00", CLIENT_NAME_LENGTH - name:len()))
  client_identifier = rpc_connection:receive(CLIENT_IDENTIFIER_LENGTH)

  -- Connect to Stream server
  -- TODO
  stream_connection = nil

  return Client:new(rpc_connection, stream_connection)
end

return krpc
