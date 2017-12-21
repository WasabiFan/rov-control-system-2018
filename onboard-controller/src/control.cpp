#include "control.h"

#include <cmath>

#include <Arduino.h>

void Control::updateThrusterOutputs(Vector6f thrusterOutputs)
{
    
    if(!this->controlState.isEnabled)
    {
        this->stopAllOutputs();
        return;
    }

    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        digitalWriteFast(this->thrusterIO[i].dirPin, thrusterOutputs[i] > 0 ? HIGH : LOW);

        // TODO: think about scaling
        uint8_t dutyCycle = map(std::fabs(thrusterOutputs[i]), 0, 1, 0, 256);
        analogWrite(this->thrusterIO[i].pwmPin, dutyCycle);
    }
}

void Control::stopAllOutputs()
{
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        digitalWriteFast(this->thrusterIO[i].dirPin, LOW);
        analogWrite(this->thrusterIO[i].pwmPin, 0);
    }
}

void Control::init()
{
    // TODO: Tune frequency and resolution
    for(size_t i = 0; i < NUM_THRUSTERS; i++)
    {
        pinMode(this->thrusterIO[i].dirPin, OUTPUT);
        pinMode(this->thrusterIO[i].pwmPin, OUTPUT);
    }

    this->stopAllOutputs();
}

void Control::updateRequestedRigidForces(Vector6f newForces)
{
    // TODO: Can result of "fullPivLu" call be cached?
    Vector6f thrusterOutputs = this->intrinsics.fullPivLu().solve(newForces);
    this->updateThrusterOutputs(thrusterOutputs);
}

void Control::disable()
{
    this->controlState.isEnabled = false;
    this->stopAllOutputs();
}