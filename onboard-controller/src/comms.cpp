#include "comms.h"
#include <iostream>
#include <sstream>
#include <iterator>

bool Comms::readPacketFromSerial(SerialPacket *resultPacket)
{
    while (Serial.available())
    {
        char val = Serial.read();

        if (val == '\n')
        {
            if (serialLineBuffer[0] != '!')
            {
                // Received non-packet, bail
                serialLineBuffer.clear();
                return false;
            }
            std::istringstream bufferStream(serialLineBuffer);

            std::getline(bufferStream, resultPacket->type, ' ');
            resultPacket->type.erase(0, 1);

            resultPacket->parameters.clear();
            std::string nextParameter;
            while (std::getline(bufferStream, nextParameter, ' '))
            {
                resultPacket->parameters.push_back(nextParameter);
            }

            serialLineBuffer.clear();
            lastReceiveTime = millis();
            return true;
        }

        serialLineBuffer += val;
    }

    return false;
}

void Comms::sendPacketToSerial(SerialPacket *packet)
{
    Serial.print('!');
    Serial.print(packet->type.c_str());

    for (std::size_t i = 0; i < packet->parameters.size(); i++)
    {
        Serial.print(' ');
        Serial.print(packet->parameters[i].c_str());
    }

    Serial.print('\n');
}

void Comms::debugEchoPacketToSerial(SerialPacket *packet)
{
    Serial.print("Packet type: ");
    Serial.println((*packet).type.c_str());
    for (std::size_t i = 0; i < (*packet).parameters.size(); i++)
    {
        Serial.print("    [");
        Serial.print(i);
        Serial.print("]: ");
        Serial.println((*packet).parameters[i].c_str());
    }
}

uint32_t Comms::getTimeSinceLastReceive()
{
    return millis() - this->lastReceiveTime;
}