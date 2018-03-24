#include "control.h"

#include <cmath>

#include <Arduino.h>

void Control::updateThrusterOutputs(Eigen::Vector6f thrusterOutputs)
{
    if(!this->controlState.isEnabled)
    {
        this->stopAllOutputs();
        return;
    }

    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        int pwmValue = map(thrusterOutputs[i], -1, 1, THRUSTER_MIN_DUTY_CYCLE, THRUSTER_MAX_DUTY_CYCLE);
        analogWrite(this->thrusterIO[i].pwmPin, pwmValue);
    }
}

void Control::stopAllOutputs()
{
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        analogWrite(this->thrusterIO[i].pwmPin, 0);
    }
}

void Control::init()
{
    analogWriteResolution(PWM_PRECISION_BITS);
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        analogWriteFrequency(this->thrusterIO[i].pwmPin, THRUSTER_BASE_FREQUENCY);
        pinMode(this->thrusterIO[i].pwmPin, OUTPUT);
    }

    this->stopAllOutputs();
}

void Control::updateRequestedRigidForcesPct(Eigen::Vector6f newForcesPct)
{
    // TODO: Can result of "fullPivLu" call be cached?
    Eigen::Vector6f thrusterOutputs = this->intrinsics.fullPivLu().solve(newForcesPct);

    float absMax = std::max(thrusterOutputs.maxCoeff(), std::fabs(thrusterOutputs.minCoeff()));
    if(absMax > 1) {
        thrusterOutputs /= absMax;
        this->telemetryInfo.isScalingAtLimit = true;
        this->telemetryInfo.limitScaleFactor = absMax;
    }
    else {
        this->telemetryInfo.isScalingAtLimit = false;
    }

    this->updateThrusterOutputs(thrusterOutputs);
}

void Control::disable()
{
    this->controlState.isEnabled = false;
    this->stopAllOutputs();
}

bool Control::isEnabled()
{
    return this->controlState.isEnabled;
}

TelemetryInfo Control::getTelemetryInfo()
{
    return this->telemetryInfo;
}