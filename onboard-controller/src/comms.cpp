#include "comms.h"
#include <iostream>
#include <sstream>
#include <iterator>

std::string serialLineBuffer;

bool readPacketFromSerial(SerialPacket* resultPacket) {
    while(Serial.available()) {
        char val = Serial.read();

        if(val == '\n') {
            if(serialLineBuffer[0] != '!') {
                // Received non-packet, bail
                serialLineBuffer.clear();
                return false;
            }
            std::istringstream bufferStream(serialLineBuffer);

            std::getline(bufferStream, resultPacket->type, ' ');
            resultPacket->type.erase(0, 1);
            
            resultPacket->parameters.clear();
            std::string nextParameter;    
            while (std::getline(bufferStream, nextParameter, ' ')) {
                resultPacket->parameters.push_back(nextParameter);
            }

            serialLineBuffer.clear();
            return true;
        }

        serialLineBuffer += val;
    }

    return false;
}

void echoPacketToSerial(SerialPacket* packet) {
    Serial.print("Packet type: ");
    Serial.println((*packet).type.c_str());
    for(uint i = 0; i < (*packet).parameters.size(); i++) {
        Serial.print("    [");
        Serial.print(i);
        Serial.print("]: ");
        Serial.println((*packet).parameters[i].c_str());
    }
}