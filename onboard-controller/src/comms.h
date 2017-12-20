#include <string>
#include <vector>

#include <Arduino.h>

struct SerialPacket {
    std::string type;
    std::vector<std::string> parameters;
};



bool readPacketFromSerial(SerialPacket* out);
void echoPacketToSerial(SerialPacket* packet);