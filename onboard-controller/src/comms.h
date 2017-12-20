#include <string>
#include <vector>

#include <Arduino.h>

struct SerialPacket
{
    std::string type;
    std::vector<std::string> parameters;
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