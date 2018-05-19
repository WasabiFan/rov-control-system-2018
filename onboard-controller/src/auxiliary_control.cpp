#include "auxiliary_control.h"

void AuxiliaryControl::init()
{
    isImuInitialized = this->imu.begin();
    if (!isImuInitialized)
    {
        // TODO: log
    }
    // TODO: IMU calibration?
    // setExtCrystalUse

}

imu::Vector<3> AuxiliaryControl::getOrientation()
{
    if (!isImuInitialized)
    {
        return imu::Vector<3>();
    }
    return imu.getQuat().toEuler();
}
