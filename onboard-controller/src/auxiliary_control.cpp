#include "auxiliary_control.h"

void AuxiliaryControls::initialize()
{
    isImuInitialized = this->imu.begin();
    if (!isImuInitialized)
    {
        // TODO: log
    }
    imu.getQuat()
    // TODO: IMU calibration?
    // setExtCrystalUse
}

