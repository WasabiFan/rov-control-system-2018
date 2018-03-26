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
        analogWrite(this->design.thrusters[i].pwmPin, pwmValue);
    }
}

void Control::stopAllOutputs()
{
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        analogWrite(this->design.thrusters[i].pwmPin, 0);
    }
}

void Control::init(DesignInfo& design)
{   
    this->design = design;

    for (int col = 0; col < NUM_THRUSTERS; col++) {
        this->intrinsics.block(0, col, 3, 1) = this->design.thrusters[col].orientation.normalized();

        auto comRelativePosition = this->design.thrusters[col].position - this->design.centerOfMass;
        this->intrinsics.block(3, col, 3, 1) = comRelativePosition.cross(this->design.thrusters[col].orientation.normalized());
    }

    analogWriteResolution(PWM_PRECISION_BITS);
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        analogWriteFrequency(this->design.thrusters[i].pwmPin, THRUSTER_BASE_FREQUENCY);
        pinMode(this->design.thrusters[i].pwmPin, OUTPUT);
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
        this->telemetryInfo.limitScaleFactor = 1;
    }

    this->telemetryInfo.lastOutputs = thrusterOutputs;
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

std::string Control::getIntrinsicsDebugInfo()
{
    std::ostringstream s;
    s << "Intrinsics: =======================" << std::endl;
    s << this->intrinsics << std::endl;
    s << "===================================" << std::endl;
    return s.str();
}
