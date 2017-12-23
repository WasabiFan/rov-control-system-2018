#include <string>
#include <vector>

struct SerialPacket
{
  std::string type;
  std::vector<std::string> parameters;

  SerialPacket(std::string type) : type(type) {}
  SerialPacket() {}
};

class Comms
{
private:
  std::string serialLineBuffer;
  uint32_t lastReceiveTime = 0;

public:
  bool readPacketFromSerial(SerialPacket *out);
  void sendPacketToSerial(SerialPacket *packet);

  void debugEchoPacketToSerial(SerialPacket *packet);

  uint32_t getTimeSinceLastReceive();
};