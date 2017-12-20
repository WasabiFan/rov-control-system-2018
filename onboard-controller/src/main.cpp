#include "Arduino.h"
#include <Eigen.h>
#include <Dense>

#include "comms.h"

// https://forum.pjrc.com/threads/29177-Teensy-3-1-signalr-c-(-text-_kill_r-0xe)-undefined-reference-to-_kill-error
extern "C"{
  int _getpid() { return -1;}
  int _kill(int pid, int sig) { return -1; }
  int _write() {return -1;}
}

using namespace Eigen;
typedef Matrix<float, 6, 1> Vector6f;

Matrix<float, 6, 6> intrinsics = Matrix<float, 6, 6>::Random();

void setup()
{
    Serial.begin(9600);

    while(!Serial);
    Serial.println("Running...");

    /*
    // Throwaway variable to consume result and prevent the operation from being optimized away
    double x = 0;

    uint32_t start = millis();
    for(int i = 0; i < 10000; i++) {
        Vector6f goals = Vector6f::Random();
        Vector6f result = intrinsics.fullPivLu().solve(goals);
        x += result.sum();
    }
    Serial.println("Done");
    uint32_t duration = millis() - start;
    Serial.println(x);
    Serial.println(duration);*/
}

void loop()
{
    SerialPacket lastPacket;
    while(readPacketFromSerial(&lastPacket)) {
        sendPacketToSerial(&lastPacket);
    }
}