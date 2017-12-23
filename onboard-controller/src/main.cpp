#include "Arduino.h"

#include "common.h"
#include "comms.h"
#include "control.h"

#include <string>

#define DISABLE_TIMEOUT (5000)

Comms comms;
Control control;

void setup()
{
    Serial.begin(9600);

    // TODO: Remove blocking loop
    while(!Serial);
    Serial.println("Running...");

    control.init();

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

std::string to_string(float n)
{
    std::ostringstream s;
    s << n;
    return s.str();
}

bool parseFloat (float &f, std::string s)
{
    std::stringstream ss(s);
    ss >> f;
    
    char c;
    return !ss.fail() && !ss.get(c);
}

void sendTelemetry()
{
    auto controlTelemetry = control.getTelemetryInfo();

    SerialPacket telemetryPacket("telemetry");
    telemetryPacket.parameters.push_back(controlTelemetry.isScalingAtLimit ? "true" : "false");
    telemetryPacket.parameters.push_back(to_string(controlTelemetry.limitScaleFactor));
    comms.sendPacketToSerial(&telemetryPacket);
}

bool handleControlPacket(std::vector<std::string> parameters)
{
    if(parameters.size() != 6)
    {
        return false;
    }

    Eigen::Vector6f rigidForcesPct;
    for(size_t i = 0; i < 6; i++)
    {
        if(!parseFloat(rigidForcesPct[i], parameters[i]))
        {
            return false;
        }
    }
    control.updateRequestedRigidForcesPct(rigidForcesPct);
}

void loop()
{
    SerialPacket lastPacket;
    while(comms.readPacketFromSerial(&lastPacket))
    {
        // TODO: Prevent blocking loop for too long
        if(lastPacket.type == "control")
        {
            if(!handleControlPacket(lastPacket.parameters))
            {
                // TODO
            }
        }
        else
        {
            // TODO
        }
    }

    if(comms.getTimeSinceLastReceive() > DISABLE_TIMEOUT)
    {
        control.disable();
        // TODO: log?
    }

    sendTelemetry();
}

// https://forum.pjrc.com/threads/29177-Teensy-3-1-signalr-c-(-text-_kill_r-0xe)-undefined-reference-to-_kill-error
extern "C" {
  int _getpid() { return -1;}
  int _kill(int pid, int sig) { return -1; }
  int _write() {return -1;}
}