#include "comms.h"

#include "common.h"

#include <iostream>
#include <sstream>
#include <iterator>

#include <Arduino.h>

void Comms::initialize()
{
    Serial.begin(115200);
}

void Comms::sendOrQueueAsyncPacket(std::shared_ptr<SerialPacket> packet)
{
    if (Serial)
    {
        sendPacketToSerial(packet.get());
    }
    else
    {
        asynchronousMessageQueue.push(packet);
        if (asynchronousMessageQueue.size() > ASYNC_PACKET_QUEUE_MAX_LENGTH)
        {
            asynchronousMessageQueue.pop();
        }
    }
}

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
    std::ostringstream ss;
    ss << '!' << packet->type;

    for (std::size_t i = 0; i < packet->parameters.size(); i++)
    {
        ss << ' ' << packet->parameters[i];
    }
    sendRawMessageToSerial(ss.str());
}

void Comms::logError(std::string error)
{
    std::shared_ptr<SerialPacket> packet = std::make_shared<SerialPacket>();
    packet->type = "log";
    packet->parameters.push_back("error");
    packet->parameters.push_back(error);

    DEBUG_SERIAL_IPRINT("ERROR: ");
    DEBUG_SERIAL_PRINTLN(error.c_str());

    sendOrQueueAsyncPacket(packet);
}

void Comms::sendRawMessageToSerial(std::string string)
{
    Serial.print(string.c_str());
    Serial.print("\n");
}

void Comms::debugEchoPacketToSerial(SerialPacket *packet)
{
    Serial.print("Packet type: ");
    Serial.print((*packet).type.c_str());
    Serial.print("\n");
    for (std::size_t i = 0; i < (*packet).parameters.size(); i++)
    {
        Serial.print("    [");
        Serial.print(i);
        Serial.print("]: ");
        Serial.print((*packet).parameters[i].c_str());
        Serial.print("\n");
    }
}

void Comms::update()
{
    if (Serial)
    {
        while(!asynchronousMessageQueue.empty())
        {
            sendPacketToSerial(asynchronousMessageQueue.front().get());
            asynchronousMessageQueue.pop();
        }
    }
}

uint32_t Comms::getTimeSinceLastReceive()
{
    return millis() - this->lastReceiveTime;
}

Comms& Comms::getInstance()
{
    static Comms comms;
    return comms;
}