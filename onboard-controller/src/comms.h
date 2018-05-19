#pragma once

#include <string>
#include <vector>
#include <queue>
#include <memory>

struct SerialPacket
{
  std::string type;
  std::vector<std::string> parameters;

  SerialPacket(std::string type) : type(type) {}
  SerialPacket() {}
};

#define ASYNC_PACKET_QUEUE_MAX_LENGTH 10

class Comms
{
private:
  std::string serialLineBuffer;
  uint32_t lastReceiveTime = 0;
  std::queue<std::shared_ptr<SerialPacket>> asynchronousMessageQueue;

  void sendOrQueueAsyncPacket(std::shared_ptr<SerialPacket> packet);

public:
  Comms() {} 
  Comms(Comms const&) = delete;
  void operator=(Comms const&) = delete;

  void initialize();
  bool readPacketFromSerial(SerialPacket *out);
  void sendPacketToSerial(SerialPacket *packet);

  void logError(std::string error);
  void sendRawMessageToSerial(std::string string);

  void update();

  void debugEchoPacketToSerial(SerialPacket *packet);

  uint32_t getTimeSinceLastReceive();

  static Comms& getInstance();
};