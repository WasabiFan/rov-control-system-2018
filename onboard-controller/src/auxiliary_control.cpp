#include "auxiliary_control.h"

void AuxiliaryControls::init()
{
    isImuInitialized = this->imu.begin();
    if (!isImuInitialized)
    {
        // TODO: log
    }
    // TODO: IMU calibration?
    // setExtCrystalUse

}

imu::vector<3> AuxiliaryControls::getOrientation()
{
    if (!isImuInitialized)
    {
        return imu::vector<3>();
    }
    return imu.getQuat().toEuler();
}
