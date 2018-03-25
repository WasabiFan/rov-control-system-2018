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
  void initialize();
  bool readPacketFromSerial(SerialPacket *out);
  void sendPacketToSerial(SerialPacket *packet);

  void sendRawMessageToSerial(std::string string);

  void debugEchoPacketToSerial(SerialPacket *packet);

  uint32_t getTimeSinceLastReceive();
};