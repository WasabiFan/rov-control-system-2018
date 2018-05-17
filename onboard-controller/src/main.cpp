#include "Arduino.h"

#include "auxiliary_control.h"
#include "common.h"
#include "comms.h"
#include "control.h"

#include <string>

#define DISABLE_TIMEOUT (5000)

#define LOOP_FREQUENCY (100)

Comms comms;
Control control;
AuxiliaryControl auxControl;

void setup()
{
    // Illuminate built-in LED
    pinMode(13, OUTPUT);
    digitalWrite(13, HIGH);

    comms.initialize();
    DEBUG_SERIAL_BEGIN();

    // TODO: Remove blocking loop
    //while(!Serial);
    //Serial.println("Running...");

    DesignInfo design;
    design.centerOfMass = Eigen::Vector3f(0, 0, 0);
    design.thrusters = {{
        { 29, Eigen::Vector3f(1, 1, 0), Eigen::Vector3f(1, -1, 0) }, // Front right
        { 7, Eigen::Vector3f(1, -1, 0), Eigen::Vector3f(1, 1, 0) }, // Front left
        { 8, Eigen::Vector3f(-1, -1, 0), Eigen::Vector3f(-1, 1, 0) }, // Rear left
        { 10, Eigen::Vector3f(-1, 1, 0), Eigen::Vector3f(1, 1, 0) }, // Rear right
        { 9, Eigen::Vector3f(0, 1, 0), Eigen::Vector3f(0, 0, -1) }, // Bottom front
        { 30, Eigen::Vector3f(0, -1, 0), Eigen::Vector3f(0, 0, 1) } // Bottom rear
    }};
    design.gripperUpDownPin = 22;
    design.gripperOpenClosePin = 23;
    design.gimbalPin = 2;

    design.minGimbalPosition = 0.0;
    design.maxGimbalPosition = 0.7;

    control.init(design);
    Serial.println(control.getIntrinsicsDebugInfo().c_str());

    auxControl.init();

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

    const Eigen::IOFormat fmt(2, Eigen::DontAlignCols, "", ",", "", "", "", "");
    std::ostringstream stream;
    stream << controlTelemetry.lastOutputs.format(fmt);
    telemetryPacket.parameters.push_back(stream.str());
    comms.sendPacketToSerial(&telemetryPacket);
}

void sendOrientation()
{
    auto orientation = auxControl.getOrientation();
    SerialPacket orientationPacket("orientation");
    orientationPacket.parameters.push_back(to_string(orientationRadians.x));
    orientationPacket.parameters.push_back(to_string(orientationRadians.y));
    orientationPacket.parameters.push_back(to_string(orientationRadians.z));

    comms.sendPacketToSerial(&orientationPacket);
}

bool handleMotionControlPacket(std::vector<std::string> parameters)
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
    return true;
}

bool handleGripperControlPacket(std::vector<std::string> parameters)
{
    if(parameters.size() != 2)
    {
        return false;
    }

    float upDown, openClose;
    bool success = parseFloat(upDown, parameters[0]);
    success &= parseFloat(openClose, parameters[1]);

    if (!success) {
        return false;
    }

    control.setGripperOutputs(upDown, openClose);
    return true;
}

bool handleGimbalControlPacket(std::vector<std::string> parameters)
{
    if(parameters.size() != 1)
    {
        return false;
    }

    float upDown;
    if (!parseFloat(upDown, parameters[0])) {
        return false;
    }

    control.setGimbalOutputs(upDown);
    return true;
}

void loop()
{
    unsigned long loopStart = millis();
    
    SerialPacket lastPacket;
    while(comms.readPacketFromSerial(&lastPacket))
    {
        // TODO: Prevent blocking loop for too long
        if(lastPacket.type == "motion_control")
        {
            if(!handleMotionControlPacket(lastPacket.parameters))
            {
                DEBUG_SERIAL_PRINTLN("Failed to handle motion control packet");
            }
        }
        else if(lastPacket.type == "gripper_control")
        {
            if(!handleGripperControlPacket(lastPacket.parameters))
            {
                DEBUG_SERIAL_PRINTLN("Failed to handle gripper control packet");
            }
        }
        else if(lastPacket.type == "gimbal_control")
        {
            if(!handleGimbalControlPacket(lastPacket.parameters))
            {
                DEBUG_SERIAL_PRINTLN("Failed to handle gimbal control packet");
            }
        }
        else
        {
            // TODO
        }
    }

    if(control.isEnabled() && comms.getTimeSinceLastReceive() > DISABLE_TIMEOUT)
    {
        control.disable();
        // TODO: log?
    }

    sendTelemetry();
    sendOrientation();

    unsigned long loopDuration = millis() - loopStart;
    uint32_t targetLoopDuration = (uint32_t)(1000/float(LOOP_FREQUENCY));
    int32_t timeRemaining = targetLoopDuration - loopDuration;

    if (timeRemaining > 0) 
    {
        //DEBUG_SERIAL_PRINT("Loop finished with ");
        //DEBUG_SERIAL_PRINT(timeRemaining);
        //DEBUG_SERIAL_PRINTLN(" msecs remaining");
        delay(min((uint32_t)timeRemaining, targetLoopDuration));
    }
    else
    {
        DEBUG_SERIAL_PRINT("Loop finished having gone ");
        DEBUG_SERIAL_PRINT(-timeRemaining);
        DEBUG_SERIAL_PRINT(" msecs over target (");
        DEBUG_SERIAL_PRINT(targetLoopDuration);
        DEBUG_SERIAL_PRINTLN(" msecs)");
    }
}

// https://forum.pjrc.com/threads/29177-Teensy-3-1-signalr-c-(-text-_kill_r-0xe)-undefined-reference-to-_kill-error
extern "C" {
  int _getpid() { return -1;}
  int _kill(int pid, int sig) { return -1; }
  int _write() {return -1;}
}