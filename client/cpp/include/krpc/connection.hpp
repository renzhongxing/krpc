#pragma once

#include <chrono>  // NOLINT(build/c++11)
#include <stdexcept>
#include <string>
#include <vector>

#define ASIO_STANDALONE
#include <asio.hpp>

namespace krpc {

class Connection {
 public:
  Connection(const std::string& address, unsigned int port);
  void connect();
  void close();
  /** Send data to the connection. Blocks until all data has been sent. */
  void send(const char* data, size_t length);
  void send(const std::string& data);
  /** Receive data from the connection for a message. Blocks until a message has been received. */
  std::string receive_message();
  /** Receive data from the connection. Blocks until length bytes have been received. */
  std::string receive(size_t length);
  /** Receive up to length bytes of data from the connection. */
  std::string partial_receive(size_t length,
                              std::chrono::milliseconds timeout = std::chrono::milliseconds(10));
 private:
  asio::io_service io_service;
  asio::ip::tcp::socket socket;
  const std::string address;
  const unsigned int port;
  asio::ip::tcp::resolver resolver;
};

}  // namespace krpc
