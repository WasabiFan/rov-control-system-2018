#include "auxiliary_control.h"

void AuxiliaryControl::init()
{
    bool isImuInitialized = this->imu.begin();
    if (isImuInitialized)
    {
        this->imuState = IMU_STATE_CONNECTED;
    }
    else
    {
        Comms::getInstance().logError("Failed to initialize IMU. Orientation will be unavailable.");
    }
    // TODO: IMU calibration?
    // setExtCrystalUse

}

void AuxiliaryControl::updateTracking()
{
    // TODO
}

imu::Vector<3> AuxiliaryControl::getOrientation()
{
    if (!(this->imuState >= IMU_STATE_CONNECTED))
    {
        return imu::Vector<3>();
    }
    return imu.getQuat().toEuler();
}
