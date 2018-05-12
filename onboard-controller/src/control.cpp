#include "control.h"

#include <cmath>

#include <Arduino.h>

void Control::updateThrusterOutputs(Eigen::Vector6f thrusterOutputs)
{
    /*if(!this->controlState.isEnabled)
    {
        this->stopAllOutputs();
        return;
    }*/

    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        writeMotorController29(this->design.thrusters[i].pwmPin, thrusterOutputs[i]);
    }
}

void Control::stopAllOutputs()
{
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        analogWrite(this->design.thrusters[i].pwmPin, 0);
    }
}

void Control::writeMotorController29(uint8_t pin, float output)
{
    int pwmValue = map(output, -1, 1, THRUSTER_MIN_DUTY_CYCLE, THRUSTER_MAX_DUTY_CYCLE);
    analogWrite(pin, pwmValue);
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

    analogWriteFrequency(this->design.gripperOpenClosePin, THRUSTER_BASE_FREQUENCY);
    pinMode(this->design.gripperOpenClosePin, OUTPUT);
    
    analogWriteFrequency(this->design.gripperUpDownPin, THRUSTER_BASE_FREQUENCY);
    pinMode(this->design.gripperUpDownPin, OUTPUT);

    pinMode(this->design.gimbalPin, OUTPUT);

    this->stopAllOutputs();
}

void Control::updateRequestedRigidForcesPct(Eigen::Vector6f newForcesPct)
{
    Eigen::Vector3f requestedPlanarForce = { newForcesPct[0], newForcesPct[1], 0 };
    Eigen::Vector6f linearThrusterOutputs;
    linearThrusterOutputs <<
        requestedPlanarForce.dot(this->design.thrusters[0].orientation),
        requestedPlanarForce.dot(this->design.thrusters[1].orientation),
        requestedPlanarForce.dot(this->design.thrusters[2].orientation),
        requestedPlanarForce.dot(this->design.thrusters[3].orientation),
        newForcesPct[2],
        -newForcesPct[2];
    Eigen::Vector6f rotationalThrusterOutputs;
    for (int i = 0; i < 4; i++) {
        auto comRelativePosition = this->design.thrusters[i].position - this->design.centerOfMass;
        auto cross = comRelativePosition.cross(this->design.thrusters[i].orientation.normalized() * newForcesPct[5]);
        rotationalThrusterOutputs[i] = (newForcesPct[5] > 0 ? 1 : -1 ) * ( cross.norm() / comRelativePosition.norm());
    }
    rotationalThrusterOutputs[4] = 0;
    rotationalThrusterOutputs[5] = 0;


    // TODO: Can result of "fullPivLu" call be cached?
    Eigen::Vector6f thrusterOutputs = linearThrusterOutputs + rotationalThrusterOutputs; // = this->intrinsics.fullPivLu().solve(newForcesPct);

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

void Control::setGripperOutputs(float upDown, float openClose)
{
    writeMotorController29(this->design.gripperUpDownPin, upDown);
    writeMotorController29(this->design.gripperOpenClosePin, openClose);
}

void Control::setGimbalOutputs(float upDown)
{
    double scaledUpDown = map(upDown, 0, 1, this->design.minGimbalPosition, this->design.maxGimbalPosition);
    int val = (int)map(scaledUpDown, 0, 1, 0, PWM_RANGE_MAX);
    //analogWrite(this->design.gimbalPin, val);
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
